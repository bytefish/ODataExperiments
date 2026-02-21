using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using ODataFga.Database;
using ODataFga.Dtos;
using ODataFga.Services;

namespace ODataFga.Controllers;

public class GroupsController : ODataController
{
    private readonly AppDbContext _db;
    private readonly IGroupService _groupService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(AppDbContext db, IGroupService groupService, ILogger<GroupsController> logger)
    {
        _db = db; 
        _groupService = groupService; 
        _logger = logger;
    }

    [EnableQuery]
    public IActionResult Get()
    {
        return Ok(_db.Groups);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateGroupRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try 
        { 
            return Created(await _groupService.CreateGroupAsync(request)); 
        }
        catch (Exception ex) 
        { 
            return StatusCode(500, ex.Message); 
        }
    }

    [HttpPost("odata/Groups('{key}')/AddMember")]
    public async Task<IActionResult> AddMember(string key, [FromBody] GroupMemberRequest request)
    {
        try 
        { 
            await _groupService.AddMemberAsync(key, request.TargetId, request.IsUser); return Ok(); 
        }
        catch (Exception ex) 
        {
            return StatusCode(500, ex.Message); 
        }
    }

    [HttpPost("odata/Groups('{key}')/RemoveMember")]
    public async Task<IActionResult> RemoveMember(string key, [FromBody] GroupMemberRequest request)
    {
        try 
        { 
            await _groupService.RemoveMemberAsync(key, request.TargetId, request.IsUser); return Ok(); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }
}