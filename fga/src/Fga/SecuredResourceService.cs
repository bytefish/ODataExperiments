using ODataFga.Database;
using ODataFga.Services;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using System.Reflection;
using System.Transactions;

namespace ODataFga.Fga
{

    public abstract class SecuredResourceService<TEntity, TParent>
        where TEntity : class, ISecuredResource, new()
        where TParent : class, ISecuredResource
    {
        protected readonly AppDbContext Db;
        protected readonly IOpenFgaClient Fga;
        protected readonly ICurrentUserService User;
        protected readonly ILogger Logger;

        private readonly string _fgaType;
        private readonly string? _parentFgaType;

        protected SecuredResourceService(AppDbContext db, IOpenFgaClient fga, ICurrentUserService user, ILogger logger)
        {
            Db = db;
            Fga = fga;
            User = user;
            Logger = logger;

            _fgaType = typeof(TEntity)
                .GetCustomAttribute<FgaTypeAttribute>()?.Name ?? throw new InvalidOperationException($"Missing [FgaType]");

            _parentFgaType = typeof(TParent)
                .GetCustomAttribute<FgaTypeAttribute>()?.Name;
        }

        protected async Task EnsureAccessAsync(string key, string granularRelation)
        {
            if (User.UserId == null)
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var response = await Fga.Check(new ClientCheckRequest
            {
                User = User.UserId,
                Relation = granularRelation,
                Object = $"{_fgaType}:{key}"
            });

            if (response.Allowed != true)
            {
                throw new UnauthorizedAccessException("Forbidden");
            }
        }

        protected async Task<List<string>> GetAncestorsAsync(string? parentId)
        {
            if (string.IsNullOrEmpty(parentId))
            {
                return new List<string>();
            }

            var parent = await Db.Set<TParent>().FindAsync(parentId);

            if (parent == null)
            {
                throw new ArgumentException("Parent not found");
            }

            List<string> newAncestors = parent.AncestorIds.ToList();

            newAncestors.Add(parent.Id);

            return newAncestors;
        }

        protected async Task<TEntity> CreateAsync(TEntity resource)
        {
            if (User.UserId == null)
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            resource.AncestorIds = await GetAncestorsAsync(resource.ParentId);

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Db.Set<TEntity>().Add(resource);
                await Db.SaveChangesAsync();
                scope.Complete();
            }

            try
            {
                List<ClientTupleKey> writes = new List<ClientTupleKey> { new() { User = User.UserId, Relation = "owner", Object = $"{_fgaType}:{resource.Id}" } };

                if (!string.IsNullOrEmpty(resource.ParentId) && !string.IsNullOrEmpty(_parentFgaType))
                {
                    writes.Add(new() { User = $"{_parentFgaType}:{resource.ParentId}", Relation = "parent", Object = $"{_fgaType}:{resource.Id}" });
                }

                await Fga.Write(new ClientWriteRequest { Writes = writes });
            }
            catch
            {
                Logger.LogError("OpenFGA write failed. Compensating...");

                Db.Set<TEntity>().Remove(resource);

                await Db.SaveChangesAsync();

                throw new InvalidOperationException("Authorization service unavailable.");
            }

            return resource;
        }

        protected async Task MoveAsync(string key, string newParentId)
        {
            await EnsureAccessAsync(key, "can_move");

            var resource = await Db.Set<TEntity>().FindAsync(key);

            if (resource == null)
            {
                throw new KeyNotFoundException("Resource not found.");
            }

            if (resource.ParentId == newParentId)
            {
                return;
            }

            string? oldParentId = resource.ParentId;

            List<string> oldAncestors = resource.AncestorIds;
            List<string> newAncestors = await GetAncestorsAsync(newParentId);

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                resource.ParentId = newParentId;
                resource.AncestorIds = newAncestors;

                await Db.SaveChangesAsync();

                scope.Complete();
            }

            try
            {
                List<ClientTupleKeyWithoutCondition> deletes = [];
                List<ClientTupleKey> writes = [];

                if (!string.IsNullOrEmpty(oldParentId) && !string.IsNullOrEmpty(_parentFgaType))
                {
                    deletes.Add(new()
                    {
                        User = $"{_parentFgaType}:{oldParentId}",
                        Relation = "parent",
                        Object = $"{_fgaType}:{key}"
                    });
                }

                if (!string.IsNullOrEmpty(newParentId) && !string.IsNullOrEmpty(_parentFgaType))
                {
                    writes.Add(new()
                    {
                        User = $"{_parentFgaType}:{newParentId}",
                        Relation = "parent",
                        Object = $"{_fgaType}:{key}"
                    });
                }

                await Fga.Write(new ClientWriteRequest { Writes = writes, Deletes = deletes });
            }
            catch
            {
                Logger.LogError("FGA Move failed. Compensating DB...");

                resource.ParentId = oldParentId;
                resource.AncestorIds = oldAncestors;

                await Db.SaveChangesAsync();

                throw new InvalidOperationException("Authorization service failed. Move reverted.");
            }
        }

        protected async Task DeleteAsync(string key)
        {
            await EnsureAccessAsync(key, "can_delete");

            TEntity? resource = await Db.Set<TEntity>().FindAsync(key);

            if (resource == null)
            {
                throw new KeyNotFoundException("Resource not found.");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Db.Set<TEntity>().Remove(resource);

                await Db.SaveChangesAsync();

                scope.Complete();
            }

            try
            {
                List<ClientTupleKeyWithoutCondition> deletes =
                [
                    new() { User = User.UserId!, Relation = "owner", Object = $"{_fgaType}:{key}" }
                ];

                if (!string.IsNullOrEmpty(resource.ParentId) && !string.IsNullOrEmpty(_parentFgaType))
                {
                    deletes.Add(new()
                    {
                        User = $"{_parentFgaType}:{resource.ParentId}",
                        Relation = "parent",
                        Object = $"{_fgaType}:{key}"
                    });
                }

                await Fga.Write(new ClientWriteRequest { Deletes = deletes });
            }
            catch
            {
                Logger.LogWarning("Failed to clean up FGA tuples for deleted resource {Id}", key);
            }
        }

        protected async Task ShareAsync(string key, string targetUserId, string relation)
        {
            await EnsureAccessAsync(key, "can_share");

            List<ClientTupleKey> writes =
            [
                new() { User = $"user:{targetUserId}", Relation = relation, Object = $"{_fgaType}:{key}" }
            ];

            await Fga.Write(new ClientWriteRequest { Writes = writes });
        }
    }
}