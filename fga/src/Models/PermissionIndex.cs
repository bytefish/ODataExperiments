namespace ODataFga.Models;

/// <summary>
/// PermissionIndex is a class that represents the permissions assigned to a user for a specific object in the system.
/// </summary>
public class PermissionIndex
{
    /// <summary>
    /// ObjectType is a string that represents the type of the object for which permissions 
    /// are being indexed. This could be something like "document", "folder", or any other 
    /// entity type in the system. 
    /// 
    /// It serves as a categorization mechanism to differentiate permissions based on the type 
    /// of object they are associated with.
    /// </summary>
    public string ObjectType { get; set; } = "";

    /// <summary>
    /// Object ID is a string that uniquely identifies the specific instance of the object.
    /// </summary>
    public string ObjectId { get; set; } = "";

    /// <summary>
    /// User ID is a string that represents the unique identifier of the user for whom the 
    /// permissions are being indexed.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// The PermissionMask is an integer that represents the permissions assigned to the 
    /// user for the specified object.
    /// </summary>
    public int PermissionMask { get; set; }
}