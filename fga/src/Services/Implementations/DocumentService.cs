using Microsoft.EntityFrameworkCore;
using ODataFga.Database;
using ODataFga.Dtos;
using ODataFga.Models;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using System.Transactions;

namespace ODataFga.Services.Implementations;


public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;
    private readonly IOpenFgaClient _fga;
    private readonly ICurrentUserService _user;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(AppDbContext db, IOpenFgaClient fga, ICurrentUserService user, ILogger<DocumentService> logger)
    {
        _db = db;
        _fga = fga;
        _user = user;
        _logger = logger;
    }

    public async Task<Document> CreateDocumentAsync(CreateDocumentRequest request)
    {
        if (_user.UserId == null)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        List<string> ancestorPath = new();

        if (!string.IsNullOrEmpty(request.FolderId))
        {
            List<string> rawIds = await _db.Database
                .SqlQueryRaw<string>("SELECT * FROM sp_resolve_ancestors({0})", request.FolderId)
                .ToListAsync();

            if (rawIds.Count == 0)
            {
                throw new ArgumentException("Folder not found");
            }

            ancestorPath = rawIds
                .Select(id => $"folder:{id}")
                .ToList();
        }

        Document doc = new Document
        {
            Title = request.Title,
            FolderId = request.FolderId,
            AncestorIds = ancestorPath
        };

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            _db.Documents.Add(doc);
            
            await _db.SaveChangesAsync();

            scope.Complete();
        }

        try
        {
            var writes = new List<ClientTupleKey>
            {
                new() { User = _user.UserId, Relation = "owner", Object = $"document:{doc.Id}" }
            };

            if (!string.IsNullOrEmpty(request.FolderId))
            {
                writes.Add(new() { User = $"folder:{request.FolderId}", Relation = "parent", Object = $"document:{doc.Id}" });
            }

            await _fga.Write(new ClientWriteRequest { Writes = writes });
        }
        catch (Exception fgaEx)
        {
            _logger.LogError(fgaEx, "OpenFGA write failed. Compensating...");

            _db.Documents.Remove(doc);
            await _db.SaveChangesAsync();

            throw new InvalidOperationException("Authorization service unavailable.");
        }

        return doc;
    }

    public async Task MoveDocumentAsync(string key, MoveDocumentRequest request)
    {
        if (_user.UserId == null)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        Document? doc = await _db.Documents.FindAsync(key);

        if (doc == null)
        {
            throw new KeyNotFoundException("Document not found.");
        }

        if (doc.FolderId == request.NewFolderId)
        {
            return;
        }

        PermissionIndex? perm = await _db.Permissions
            .FirstOrDefaultAsync(p => p.ObjectType == "document" && p.ObjectId == key && p.UserId == _user.UserId);

        if (perm == null || (perm.PermissionMask & (int)(DocPermissions.Owner | DocPermissions.Editor)) == 0)
        {
            throw new UnauthorizedAccessException("Forbidden");
        }
            
        string? oldFolderId = doc.FolderId;

        List<string> oldAncestors = doc.AncestorIds;
        List<string> newAncestors = new();

        if (!string.IsNullOrEmpty(request.NewFolderId))
        {
            List<string> rawIds = await _db.Database
                .SqlQueryRaw<string>("SELECT * FROM sp_resolve_ancestors({0})", request.NewFolderId)
                .ToListAsync();

            if (rawIds.Count == 0)
            {
                throw new ArgumentException("Target not found");
            }

            newAncestors = rawIds
                .Select(id => $"folder:{id}")
                .ToList();
        }

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            doc.FolderId = request.NewFolderId;
            doc.AncestorIds = newAncestors;

            await _db.SaveChangesAsync();

            scope.Complete();
        }

        try
        {
            List<ClientTupleKeyWithoutCondition> deletes = [];
            List<ClientTupleKey> writes = [];

            if (!string.IsNullOrEmpty(oldFolderId))
            {
                deletes.Add(new() { User = $"folder:{oldFolderId}", Relation = "parent", Object = $"document:{key}" });
            }

            if (!string.IsNullOrEmpty(request.NewFolderId))
            {
                writes.Add(new() { User = $"folder:{request.NewFolderId}", Relation = "parent", Object = $"document:{key}" });
            }

            await _fga.Write(new ClientWriteRequest { Writes = writes, Deletes = deletes });
        }
        catch (Exception fgaEx)
        {
            _logger.LogError(fgaEx, "FGA Move failed. Compensating DB...");

            doc.FolderId = oldFolderId;
            doc.AncestorIds = oldAncestors;

            await _db.SaveChangesAsync();

            throw new InvalidOperationException("Authorization service failed. Move reverted.");
        }
    }

    public async Task DeleteDocumentAsync(string key)
    {
        if (_user.UserId == null)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        Document? doc = await _db.Documents.FindAsync(key);

        if (doc == null)
        {
            throw new KeyNotFoundException("Document not found.");
        }

        PermissionIndex? perm = await _db.Permissions
            .FirstOrDefaultAsync(p => p.ObjectType == "document" && p.ObjectId == key && p.UserId == _user.UserId);

        if (perm == null || (perm.PermissionMask & (int)DocPermissions.Owner) == 0)
        {
            throw new UnauthorizedAccessException("Forbidden");
        }

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            _db.Documents.Remove(doc);

            await _db.SaveChangesAsync();

            scope.Complete();
        }

        try
        {
            List<ClientTupleKeyWithoutCondition> deletes = 
            [
                new() { User = _user.UserId, Relation = "owner", Object = $"document:{key}" }
            ];

            if (!string.IsNullOrEmpty(doc.FolderId))
            {
                deletes.Add(new() { User = $"folder:{doc.FolderId}", Relation = "parent", Object = $"document:{key}" });
            }

            await _fga.Write(new ClientWriteRequest { Deletes = deletes });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up FGA tuples for deleted doc {Id}", key);
        }
    }
}