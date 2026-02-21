using System.ComponentModel.DataAnnotations;

namespace ODataFga.Dtos;

/// <summary>
/// Request for creating a new group with a specified name and an optional parent group. The Name property 
/// is required and must not be empty, while the ParentId can be null or reference an existing group. 
/// </summary>
public class CreateGroupRequest 
{
    /// <summary>
    /// Group Name to be created. This property is required and must not be null or empty. It 
    /// represents the name of the group to be created in the system. 
    /// </summary>
    [Required] public string Name { get; set; } = "";

    /// <summary>
    /// Parent Group ID where the new group should be created. If null, the group will 
    /// be created at the root level.
    /// </summary>
    public string? ParentId { get; set; } 
}
