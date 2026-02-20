namespace ODataFga.Models;

/// <summary>
/// Document class represents a document entity in the system.
/// </summary>
public class Document
{
    /// <summary>
    /// Gets or sets the unique identifier for the object.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the title associated with the object.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the folder associated with this instance.
    /// </summary>
    public string? FolderId { get; set; }

    /// <summary>
    /// Gets or sets the list of ancestor identifiers for the current entity.
    /// </summary>
    public List<string> AncestorIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of permissions associated with the object.
    /// </summary>
    public List<PermissionIndex> Permissions { get; set; } = new();
}