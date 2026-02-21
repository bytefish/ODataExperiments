using System.ComponentModel.DataAnnotations;

namespace ODataFga.Dtos;

/// <summary>
/// Represents a request to move a document to a new parent location within a hierarchical structure.
/// </summary>
/// <remarks>The <see langword="NewParentId"/> property must be provided to specify the identifier of the new
/// parent location. Ensure that the specified parent ID exists and is valid within the context of the document
/// hierarchy.</remarks>
public class MoveDocumentRequest 
{ 
    /// <summary>
    /// Gets or sets the identifier of the new parent entity to which this entity will be associated.
    /// </summary>
    /// <remarks>This property is required and must not be null or empty. It is used to establish a
    /// relationship with a new parent entity in the context of hierarchical data structures.</remarks>
    [Required] public string NewParentId { get; set; } = ""; 
}