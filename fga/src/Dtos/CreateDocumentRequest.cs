using System.ComponentModel.DataAnnotations;

namespace ODataFga.Dtos;

/// <summary>
/// Creates a new document with the specified title and optional parent folder. The Title property 
/// is required and must not be empty, while the ParentId can be null or reference an existing 
/// folder. 
/// 
/// This request is used to initiate the creation of a document within the system, allowing 
/// for hierarchical organization if a ParentId is provided.
/// </summary>
public class CreateDocumentRequest
{
    /// <summary>
    /// The title of the document to be created. This field is required and must not be empty. It 
    /// serves as the primary identifier for the document and is used for display purposes in 
    /// the user interface.
    /// </summary>
    [Required] 
    public string Title { get; set; } = "";

    /// <summary>
    /// Optional identifier of the parent folder where the document should be created. If null, 
    /// the document will be created at the root level. If provided, this should reference 
    /// an existing folder in the system.
    /// </summary>
    public string? ParentId { get; set; }
}
