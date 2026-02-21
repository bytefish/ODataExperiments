
using Microsoft.EntityFrameworkCore;
using ODataFga.Database;
using ODataFga.Fga;
using ODataFga.Models;
using ODataFga.Services;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace ODataFga.Hosted;


public class BitmaskWatcherService : BackgroundService
{
    private readonly IPermissionSyncService _permissionSyncService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IOpenFgaClient _fga;
    private readonly ILogger<BitmaskWatcherService> _logger;

    private const string STATE_KEY = "fga_bitmask";

    public BitmaskWatcherService(IPermissionSyncService permissionSyncService, IDbContextFactory<AppDbContext> dbFactory, IOpenFgaClient fga, ILogger<BitmaskWatcherService> logger)
    {
        _permissionSyncService = permissionSyncService; 
        _dbFactory = dbFactory; 
        _fga = fga; 
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try 
            { 
                await ProcessChangesAsync(stoppingToken); 
            } 
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Sync Error"); 
            }
        
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessChangesAsync(CancellationToken ct)
    {
        using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);

        SyncState? state = await db.SyncStates.FindAsync([STATE_KEY], ct);
        
        ClientReadChangesRequest req = new ClientReadChangesRequest { };

        if (state?.LastSyncTime != null)
        {
            req.StartTime = state.LastSyncTime.Value;
        }

        DateTime currentPollTime = DateTime.UtcNow;

        // Get the list of tracked types and relations from the registry
        string[] trackedTypes = FgaTypeRegistry.GetTrackedTypes();
        string[] trackedRelations = FgaTypeRegistry.GetTrackedRelations();

        ReadChangesResponse response;
        
        try 
        { 
            response = await _fga.ReadChanges(req, cancellationToken: ct); 
        } 
        catch 
        { 
            _logger.LogError("Failed to read changes from FGA. Will retry on next poll.");

            return; 
        }

        if (response.Changes?.Count > 0)
        {
            HashSet<(string Type, string Id)> objectsToReconcile = [];
            HashSet<string> usersToReconcile = [];

            foreach (TupleChange change in response.Changes)
            {
                if (change.TupleKey?.Object == null)
                {
                    _logger.LogInformation("Skipping change with null object: {@Change}", change);
                    continue;
                }

                string[] objectParts = change.TupleKey.Object.Split(':');

                if (objectParts.Length < 2)
                {
                    _logger.LogInformation("Skipping change with invalid object format: {@Change}", change);

                    continue;
                }

                string objType = objectParts[0];
                string objId = objectParts[1];
                string user = change.TupleKey.User;

                if (trackedTypes.Contains(objType))
                {
                    objectsToReconcile.Add((objType, objId));
                }

                if (user != null && user.StartsWith("user:"))
                {
                    usersToReconcile.Add(user);
                }
            }

            foreach ((string Type, string Id) obj in objectsToReconcile)
            {
                await ReconcileObject(db, _permissionSyncService, _fga, obj.Type, obj.Id, trackedRelations, ct);
            }

            foreach (string userId in usersToReconcile)
            {
                await ReconcileUser(db, _permissionSyncService, _fga, userId, trackedTypes, trackedRelations, ct);
            }
        }

        if (state == null) 
        { 
            state = new SyncState { Key = STATE_KEY }; 
            
            db.SyncStates.Add(state); 
        }

        state.LastSyncTime = currentPollTime;
        
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Reconciles permissions for an object by listing all users that have access to the object for each tracked relation,
    /// </summary>
    private async Task ReconcileObject(AppDbContext db, IPermissionSyncService permissionSyncService, IOpenFgaClient fga, string type, string id, string[] relations, CancellationToken ct)
    {
        List<PermissionIndex> permissions = [];

        foreach (string rel in relations)
        {
            ListUsersResponse response = await fga.ListUsers(new ClientListUsersRequest { Object = new FgaObject { Type = type, Id = id }, Relation = rel, UserFilters = new List<UserTypeFilter> { new UserTypeFilter { Type = "user" } } }, cancellationToken: ct);
             
            foreach (User user in response.Users)
            {
                if (user.Object?.Id != null)
                {
                    permissions.Add(new PermissionIndex { ObjectType = type, ObjectId = id, UserId = $"user:{user.Object.Id}", PermissionMask = (int)PermissionMapper.FromString(rel) });
                }
            }
        }

        List<PermissionIndex> unique = permissions.GroupBy(p => p.UserId).Select(g => new PermissionIndex { ObjectType = type, ObjectId = id, UserId = g.Key, PermissionMask = g.Aggregate(0, (acc, n) => acc | n.PermissionMask) }).ToList();
        
        await permissionSyncService.SyncForObjectAsync(db, type, id, unique, ct);
    }

    /// <summary>
    /// Reconciles permissions for a user by listing all objects the user has access to for each tracked type 
    /// and relation, then aggregating those permissions into a bitmask for each object and syncing to the 
    /// database. 
    /// 
    /// This is necessary to handle changes to user permissions that may not be captured by object-centric 
    /// reconciliation, such as changes to group memberships or direct user permissions on objects.
    /// </summary>
    private async Task ReconcileUser(AppDbContext db, IPermissionSyncService permissionSyncService, IOpenFgaClient fga, string userId, string[] types, string[] relations, CancellationToken ct)
    {
        List<PermissionIndex> permissions = new List<PermissionIndex>();

        foreach (string type in types)
        {
            foreach (string rel in relations)
            {
                ListObjectsResponse response = await fga.ListObjects(new ClientListObjectsRequest { User = userId, Relation = rel, Type = type }, cancellationToken: ct);

                if (response.Objects == null)
                {
                    _logger.LogInformation("No objects found for user {UserId} with relation {Relation} on type {Type}", userId, rel, type);

                    continue;
                }

                foreach (string obj in response.Objects)
                {
                    permissions.Add(new PermissionIndex { ObjectType = type, ObjectId = obj.Split(':')[1], UserId = userId, PermissionMask = (int)PermissionMapper.FromString(rel) });
                }
            }
        }

        // Aggregate permissions by object
        List<PermissionIndex> unique = permissions
            // Filter out any permissions that don't have a valid ObjectId
            .GroupBy(p => new { p.ObjectType, p.ObjectId })
            // For each object, combine the permissions into a single PermissionIndex with a bitmask
            .Select(g => new PermissionIndex 
            { 
                ObjectType = g.Key.ObjectType, 
                ObjectId = g.Key.ObjectId, 
                UserId = userId, 
                PermissionMask = g
                    // Combine permissions using bitwise OR to create a bitmask representing all permissions the user has for that object
                    .Aggregate(0, (acc, n) => acc | n.PermissionMask) })
            .ToList();

        await permissionSyncService.SyncForUserAsync(db, userId, unique, ct);
    }
}