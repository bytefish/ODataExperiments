using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using ODataFga.Database;
using ODataFga.Dtos;
using ODataFga.Services;

namespace ODataFga.Controllers;

public class DocumentsController : ODataController
{
    private readonly AppDbContext _db;
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(AppDbContext db, IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _db = db;
        _documentService = documentService;
        _logger = logger;
    }

    [EnableQuery]
    public IActionResult Get() => Ok(_db.Documents);

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateDocumentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var doc = await _documentService.CreateDocumentAsync(request);

            return Created(doc);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Unexpected error creating document.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Move(string key, [FromBody] MoveDocumentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _documentService.MoveDocumentAsync(key, request);

            return Ok(new { Message = "Moved successfully", NewLocation = request.NewFolderId });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Unexpected error moving document.");
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(string key)
    {
        try
        {
            await _documentService.DeleteDocumentAsync(key);

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Unexpected error deleting document.");
        }
    }

    private IActionResult HandleException(Exception ex, string logMessage) => ex switch
    {
        UnauthorizedAccessException uae when uae.Message == "Forbidden" => Forbid(),
        UnauthorizedAccessException => Unauthorized(),
        KeyNotFoundException => NotFound(),
        ArgumentException ae => BadRequest(ae.Message),
        InvalidOperationException ioe => StatusCode(502, ioe.Message),
        _ => LogAndReturn500(ex, logMessage)
    };

    private IActionResult LogAndReturn500(Exception ex, string logMessage)
    {
        _logger.LogError(ex, logMessage);

        return StatusCode(500, "Internal storage error.");
    }
}
