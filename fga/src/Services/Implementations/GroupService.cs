using ODataFga.Database;
using ODataFga.Dtos;
using ODataFga.Fga;
using ODataFga.Models;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

namespace ODataFga.Services.Implementations;

public class GroupService : SecuredResourceService<Group, Group>, IGroupService
{
    public GroupService(AppDbContext db, IOpenFgaClient fga, ICurrentUserService user, ILogger<GroupService> logger)
        : base(db, fga, user, logger) { }

    public async Task<Group> CreateGroupAsync(CreateGroupRequest request)
    {
        return await CreateAsync(new Group { Name = request.Name, ParentId = request.ParentId });
    }

    public async Task AddMemberAsync(string groupId, string targetId, bool isUser)
    {
        await EnsureAccessAsync(groupId, "can_manage");

        string userStr = isUser ? $"user:{targetId}" : $"group:{targetId}#member";

        await Fga.Write(new ClientWriteRequest
        {
            Writes = new List<ClientTupleKey> { new() { User = userStr, Relation = "member", Object = $"group:{groupId}" } }
        });
    }

    public async Task RemoveMemberAsync(string groupId, string targetId, bool isUser)
    {
        await EnsureAccessAsync(groupId, "can_manage");

        string userStr = isUser ? $"user:{targetId}" : $"group:{targetId}#member";

        await Fga.Write(new ClientWriteRequest
        {
            Deletes = new List<ClientTupleKeyWithoutCondition>
            {
                new() 
                { 
                    User = userStr, 
                    Relation = "member", 
                    Object = $"group:{groupId}"
                }
            }
        });
    }

    public async Task DeleteGroupAsync(string key)
    {
        await DeleteAsync(key);
    }
}
