
using ODataFga.Database;
using ODataFga.Models;
using ODataFga.Services;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;

namespace ODataFga.Hosted;

public class BitmaskWatcherService : BackgroundService
{
    private readonly IPermissionSyncService _permissionSyncService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BitmaskWatcherService> _logger;
    private const string STATE_KEY = "fga_bitmask";

    public BitmaskWatcherService(IPermissionSyncService permissionSyncService, IServiceScopeFactory scopeFactory, ILogger<BitmaskWatcherService> logger)
    {
        _permissionSyncService = permissionSyncService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    await ProcessChangesAsync(scope, stoppingToken);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Sync Error"); }
            await Task.Delay(1000, stoppingToken); // Faster poll for tests
        }
    }

    private async Task ProcessChangesAsync(IServiceScope scope, CancellationToken ct)
    {
        // Services
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IOpenFgaClient fga = scope.ServiceProvider.GetRequiredService<IOpenFgaClient>();

        // Sync state tracking
        SyncState? state = await db.SyncStates.FindAsync([ STATE_KEY ], ct);

        // Get last changes since last sync
        ClientReadChangesRequest req = new ClientReadChangesRequest { };

        if (state?.LastSyncTime != null)
        {
            req.StartTime = state.LastSyncTime.Value;
        }

        // Capture time before the request to prevent missing any operations that occur during the fetch
        var currentPollTime = DateTime.UtcNow;

        ReadChangesResponse response;

        try
        {
            response = await fga.ReadChanges(req, cancellationToken: ct);
        }
        catch
        {
            return;
        }

        if (response.Changes?.Count > 0)
        {
            HashSet<(string Type, string Id)> objectsToReconcile = [];

            HashSet<string> usersToReconcile = [];

            foreach (var change in response.Changes)
            {
                if (change.TupleKey?.Object == null) continue;

                string[] objectParts = change.TupleKey.Object.Split(':');

                if (objectParts.Length < 2)
                {
                    continue;
                }

                string objType = objectParts[0];
                string objId = objectParts[1];
                string user = change.TupleKey.User;

                if (objType == "document" || objType == "folder")
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
                await ReconcileObject(db, _permissionSyncService, fga, obj.Type, obj.Id, ct);
            }

            foreach (string userId in usersToReconcile)
            {
                await ReconcileUser(db, _permissionSyncService, fga, userId, ct);
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

    private async Task ReconcileObject(AppDbContext db, IPermissionSyncService permissionSyncService, IOpenFgaClient fga, string type, string id, CancellationToken ct)
    {
        List<PermissionIndex> permissions = [];
        string[] relations = ["viewer", "editor", "owner", "approver"];

        foreach (var rel in relations)
        {
            ListUsersResponse response = await fga.ListUsers(new ClientListUsersRequest
            {
                Object = new FgaObject { Type = type, Id = id },
                Relation = rel,
                UserFilters = new List<UserTypeFilter> { new UserTypeFilter { Type = "user" } }
            }, cancellationToken: ct);

            foreach (User user in response.Users)
            {
                if (user.Object?.Id != null)
                {
                    permissions.Add(new PermissionIndex
                    {
                        ObjectType = type,
                        ObjectId = id,
                        UserId = $"user:{user.Object.Id}",
                        PermissionMask = (int)PermissionMapper.FromString(rel)
                    });
                }
            }
        }
        var unique = permissions.GroupBy(p => p.UserId).Select(g => new PermissionIndex
        {
            ObjectType = type,
            ObjectId = id,
            UserId = g.Key,
            PermissionMask = g.Aggregate(0, (acc, n) => acc | n.PermissionMask)
        }).ToList();

        await permissionSyncService.SyncForObjectAsync(db, type, id, unique, ct);
    }

    private async Task ReconcileUser(AppDbContext db, IPermissionSyncService permissionSyncService, IOpenFgaClient fga, string userId, CancellationToken ct)
    {
        var permissions = new List<PermissionIndex>();

        string[] types = ["folder", "document"];
        string[] relations = ["viewer", "editor", "owner", "approver"];

        foreach (var type in types)
        {
            foreach (var rel in relations)
            {
                var response = await fga.ListObjects(new ClientListObjectsRequest { User = userId, Relation = rel, Type = type }, cancellationToken: ct);

                if (response.Objects == null)
                {
                    continue;
                }

                foreach (var obj in response.Objects)
                {
                    permissions.Add(new PermissionIndex
                    {
                        ObjectType = type,
                        ObjectId = obj.Split(':')[1],
                        UserId = userId,
                        PermissionMask = (int)PermissionMapper.FromString(rel)
                    });
                }
            }
        }

        List<PermissionIndex> unique = permissions.GroupBy(p => new { p.ObjectType, p.ObjectId }).Select(g => new PermissionIndex
        {
            ObjectType = g.Key.ObjectType,
            ObjectId = g.Key.ObjectId,
            UserId = userId,
            PermissionMask = g.Aggregate(0, (acc, n) => acc | n.PermissionMask)
        }).ToList();

        await permissionSyncService.SyncForUserAsync(db, userId, unique, ct);
    }
}