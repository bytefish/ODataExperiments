namespace ODataFga.Models;

/// <summary>
/// Represents a folder entity in the system, which can be used to organize documents and 
/// other folders in a hierarchical structure.
/// </summary>
public class Folder
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
    /// The ParentId property holds the ID of the parent folder. If the folder is a top-level folder, this property will be null.
    /// </summary>
    public string? ParentId { get; set; }
}