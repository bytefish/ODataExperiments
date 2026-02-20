using Microsoft.EntityFrameworkCore;
using ODataFga.Models;
using ODataFga.Services;

namespace ODataFga.Database;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser) : base(options) { _currentUser = currentUser; }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Folder> Folders => Set<Folder>();

    public DbSet<PermissionIndex> Permissions => Set<PermissionIndex>();

    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncState>().HasKey(s => s.Key);

        modelBuilder.Entity<PermissionIndex>().HasKey(p => new { p.ObjectType, p.ObjectId, p.UserId });

        modelBuilder.Entity<Document>()
            .HasMany(d => d.Permissions)
            .WithOne()
            .HasForeignKey(p => p.ObjectId);

        modelBuilder.Entity<Document>().HasQueryFilter(d =>
            Permissions.Any(p =>
                p.ObjectType == "document" && p.ObjectId == d.Id && p.UserId == _currentUser.UserId &&
                (p.PermissionMask & _currentUser.RequiredMask) == _currentUser.RequiredMask)
            ||
            Permissions.Any(p =>
                p.ObjectType == "folder" && p.UserId == _currentUser.UserId &&
                (p.PermissionMask & _currentUser.RequiredMask) == _currentUser.RequiredMask &&
                d.AncestorIds.Contains(p.ObjectId))
        );

        modelBuilder.Entity<Folder>().HasQueryFilter(f =>
             Permissions.Any(p =>
                p.ObjectType == "folder" && p.ObjectId == f.Id && p.UserId == _currentUser.UserId &&
                (p.PermissionMask & _currentUser.RequiredMask) == _currentUser.RequiredMask)
        );
    }
}