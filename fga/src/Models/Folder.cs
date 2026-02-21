using ODataFga.Fga;

namespace ODataFga.Models;

/// <summary>
/// Represents a folder entity in the system, which can be used to organize documents and 
/// other folders in a hierarchical structure.
/// </summary>
[FgaType("folder")]
public class Folder : ISecuredResource
{
    /// <summary>
    /// Folder ID , generated as a new GUID string by default. This serves as the unique identifier 
    /// for each folder instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the folder, initialized to "New Folder" by default.
    /// </summary>
    public string Name { get; set; } = "New Folder";

    /// <summary>
    /// The ParentId property holds the ID of the parent folder. If the folder is a top-level 
    /// folder, this property will be null.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// List of AncestorIds contains the IDs of all ancestor folders in the hierarchy, starting 
    /// from the immediate parent up to the root folder.
    /// </summary>
    public List<string> AncestorIds { get; set; } = new();

    /// <summary>
    /// List of Permissions associated with the folder. Each PermissionIndex represents a 
    /// specific permission.
    /// </summary>
    public List<PermissionIndex> Permissions { get; set; } = new List<PermissionIndex>();
}