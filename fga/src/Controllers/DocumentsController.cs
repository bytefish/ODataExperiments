using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using ODataFga.Database;
using ODataFga.Dtos;
using ODataFga.Models;
using ODataFga.Services;

namespace ODataFga.Controllers;

public class DocumentsController : ODataController
{
    private readonly AppDbContext _db;
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserService _user;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(AppDbContext db, IDocumentService documentService, ICurrentUserService user, ILogger<DocumentsController> logger)
    {
        _db = db; 
        _documentService = documentService; 
        _user = user; 
        _logger = logger;
    }

    [EnableQuery]
    public IActionResult Get()
    {
        return Ok(_db.Documents);
    }

    [HttpGet("odata/Documents/SharedWithMe")]
    [EnableQuery]
    public IActionResult SharedWithMe()
    {
        if (_user.UserId == null)
        {
            return Unauthorized();
        }

        int ownerMask = (int)DocPermissions.Owner;

        // Thanks to Postgres RLS, the database always restricts access to allowed documents.
        // We only need to subtract the ones the user actually owns to fulfill a "Shared With Me"
        // functionality.
        var sharedDocs = _db.Documents.Where(d =>
            !(
                d.Permissions.Any(p => p.ObjectType == "document" && p.ObjectId == d.Id && p.UserId == _user.UserId && (p.PermissionMask & ownerMask) == ownerMask)
                ||
                d.Permissions.Any(p => d.AncestorIds.Contains(p.ObjectId) && p.UserId == _user.UserId && (p.PermissionMask & ownerMask) == ownerMask)
            )
        );

        return Ok(sharedDocs);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateDocumentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try 
        { 
            return Created(await _documentService.CreateDocumentAsync(request)); 
        }
        catch (Exception ex) 
        { 
            return HandleException(ex, "Unexpected error creating document."); 
        }
    }

    [HttpPost]
    public async Task<IActionResult> Move(string key, [FromBody] MoveDocumentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _documentService.MoveDocumentAsync(key, request);

            return Ok(new { Message = "Moved successfully", NewLocation = request.NewParentId });
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

    [HttpPost("odata/Documents('{key}')/Share")]
    public async Task<IActionResult> Share(string key, [FromBody] ShareDocumentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _documentService.ShareDocumentAsync(key, request.TargetUserId, request.Relation);

            return Ok(new { Message = "Shared successfully" });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Unexpected error sharing document.");
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

    private IActionResult LogAndReturn500(Exception ex, string logMessage) { _logger.LogError(ex, logMessage); return StatusCode(500, "Internal storage error."); }
}