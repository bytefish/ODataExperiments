using System.ComponentModel.DataAnnotations;

namespace ODataFga.Dtos;

public class MoveDocumentRequest 
{ 
    [Required] public string NewFolderId { get; set; } = ""; 
}