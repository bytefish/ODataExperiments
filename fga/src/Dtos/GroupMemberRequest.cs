using System.ComponentModel.DataAnnotations;

namespace ODataFga.Dtos;

/// <summary>
/// Represents a request to add or remove a member from a group, specifying the target member's identifier and type.
/// </summary>
/// <remarks>The TargetId property is required and should contain the unique identifier of the member to be added
/// or removed. The IsUser property indicates whether the target is a user; if false, it may represent a different type
/// of group member.</remarks>
public class GroupMemberRequest 
{
    /// <summary>
    /// TargetId is the unique identifier of the member to be added or removed from the group. This field 
    /// is required and must not be empty. It serves as the primary reference for identifying the member 
    /// within the system, allowing for accurate management of group memberships.
    /// </summary>
    [Required] 
    public string TargetId { get; set; } = "";

    /// <summary>
    /// A flag indicating whether the target member is a user. If true, the TargetId refers to a user; if 
    /// false, it may refer to another type of group member, such as a service account or an external 
    /// entity. 
    /// 
    /// This property helps determine how the system should handle the TargetId when processing the request.
    /// </summary>
    public bool IsUser { get; set; } = true; 
}
