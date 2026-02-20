using System.ComponentModel.DataAnnotations;

namespace ODataFga.Dtos;

public class CreateDocumentRequest 
{ 
    [Required] public string Title { get; set; } = ""; 
    public string? FolderId { get; set; } 
}
