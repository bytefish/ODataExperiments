using ODataFga.Dtos;
using ODataFga.Models;

namespace ODataFga.Services;

/// <summary>
/// Service for managing groups, including creating groups, adding and removing members, and deleting groups. This 
/// interface defines the contract for group-related operations within the system.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Creates a new group based on the provided request data. This method will handle the logic for creating a group,
    /// </summary>
    /// <param name="request">Request for creating a Group</param>
    /// <returns>The group created by the request</returns>
    Task<Group> CreateGroupAsync(CreateGroupRequest request);

    /// <summary>
    /// Adds a member to the specified group. The member can be either a user or another group, as indicated 
    /// by the isUser parameter. This method will handle the logic for associating the target member with 
    /// the group, ensuring that the appropriate relationships are established in the system.
    /// </summary>
    /// <param name="groupId">Unique identifier for the Group</param>
    /// <param name="targetId">Unique identifier for the Target</param>
    /// <param name="isUser">A flag indicating if we are removing a user or group</param>
    /// <returns>An awaitable task</returns>
    Task AddMemberAsync(string groupId, string targetId, bool isUser);

    /// <summary>
    /// Removes a member from the specified group. The member can be either a user or another group, as indicated 
    /// by the isUser parameter. This method will handle the logic for disassociating the target member from 
    /// the group, ensuring that the appropriate relationships are removed from the system.
    /// </summary>
    /// <param name="groupId">Unique identifier for the Group</param>
    /// <param name="targetId">Unique identifier for the Target</param>
    /// <param name="isUser">A flag indicating if we are removing a user or group</param>
    /// <returns>An awaitable task</returns>
    Task RemoveMemberAsync(string groupId, string targetId, bool isUser);

    /// <summary>
    /// Deletes a Group identified by the specified key. 
    /// 
    /// This method will handle the logic for removing the group from the system,
    /// </summary>
    /// <param name="key">Identifier for the group</param>
    /// <returns>An awaitable task</returns>
    Task DeleteGroupAsync(string key);
}