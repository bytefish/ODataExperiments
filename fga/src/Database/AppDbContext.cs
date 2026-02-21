using Microsoft.EntityFrameworkCore;
using ODataFga.Fga;
using ODataFga.Models;
using System.Reflection;

namespace ODataFga.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    
    public DbSet<Folder> Folders => Set<Folder>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<PermissionIndex> Permissions => Set<PermissionIndex>();
    
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PermissionIndex>()
            .HasKey(p => new { p.ObjectType, p.ObjectId, p.UserId });


        // RLS queries always filter by UserId and ObjectType, then check the bitmask.
        modelBuilder.Entity<PermissionIndex>()
            .HasIndex(p => new { p.UserId, p.ObjectType, p.PermissionMask })
            .HasDatabaseName("ix_permissions_rls_lookup");

        modelBuilder.Entity<SyncState>()
            .HasKey(s => s.Key);

        IEnumerable<Type> securedTypes = modelBuilder.Model.GetEntityTypes()
            .Select(t => t.ClrType)
            .Where(t => typeof(ISecuredResource)
            .IsAssignableFrom(t) && !t.IsAbstract);

        foreach (Type type in securedTypes)
        {
            MethodInfo? method = GetType()
                .GetMethod(nameof(ApplySecuredResourceNavigation), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(type);

            method?.Invoke(this, [ modelBuilder ]);
        }
    }

    private void ApplySecuredResourceNavigation<T>(ModelBuilder modelBuilder) where T : class, ISecuredResource
    {
        modelBuilder.Entity<T>()
            .HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(p => p.ObjectId);

        // GIN Index for AncestorIds to improve performance for ANY(...) lookups in the Postgres Policy.
        modelBuilder.Entity<T>()
            .HasIndex(r => r.AncestorIds)
            .HasMethod("gin");
    }
}