using ODataFga.Database;
using ODataFga.Models;

namespace ODataFga.Services;

public interface IPermissionSyncService
{
    Task SyncForObjectAsync(AppDbContext db, string objectType, string objectId, IEnumerable<PermissionIndex> permissions, CancellationToken ct = default);

    Task SyncForUserAsync(AppDbContext db, string userId, IEnumerable<PermissionIndex> permissions, CancellationToken ct = default);
}