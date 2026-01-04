using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/workstreams")]
[Authorize]
public class WorkstreamsController : ControllerBase
{
    private readonly IWorkstreamService _workstreamService;
    private readonly IAuthService _authService;

    public WorkstreamsController(IWorkstreamService workstreamService, IAuthService authService)
    {
        _workstreamService = workstreamService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkstreamResponseDto>>> GetAllWorkstreams()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Global Admin sees all, others see only their assigned workstreams
        var allWorkstreams = await _workstreamService.GetAllWorkstreamsAsync();
        if (_authService.IsGlobalAdmin(currentUser))
        {
            return Ok(allWorkstreams);
        }

        var userWorkstreams = allWorkstreams
            .Where(w => currentUser.WorkstreamAccess.Any(wa => wa.WorkstreamId == w.WorkstreamId))
            .ToList();

        return Ok(userWorkstreams);
    }

    [HttpGet("permission-types")]
    public async Task<ActionResult<List<PermissionTypeDto>>> GetAllPermissionTypes()
    {
        var permissionTypes = await _workstreamService.GetAllPermissionTypesAsync();
        return Ok(permissionTypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkstreamResponseDto>> GetWorkstream(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!_authService.IsGlobalAdmin(currentUser) && !_authService.HasWorkstreamAccess(currentUser, id))
        {
            return Forbid();
        }

        var workstream = await _workstreamService.GetWorkstreamByIdAsync(id);
        if (workstream == null) return NotFound();

        return Ok(workstream);
    }

    [HttpPost]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult<WorkstreamResponseDto>> CreateWorkstream([FromBody] CreateWorkstreamRequest request)
    {
        var workstream = await _workstreamService.CreateWorkstreamAsync(request);
        return CreatedAtAction(nameof(GetWorkstream), new { id = workstream.WorkstreamId }, workstream);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult<WorkstreamResponseDto>> UpdateWorkstream(int id, [FromBody] UpdateWorkstreamRequest request)
    {
        var workstream = await _workstreamService.UpdateWorkstreamAsync(id, request);
        if (workstream == null) return NotFound();

        return Ok(workstream);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult> DeleteWorkstream(int id)
    {
        var result = await _workstreamService.DeleteWorkstreamAsync(id);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpGet("{id}/users")]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult<List<UserResponseDto>>> GetWorkstreamUsers(int id)
    {
        var users = await _workstreamService.GetWorkstreamUsersAsync(id);
        return Ok(users);
    }

    [HttpPost("{id}/users")]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult> AssignUserToWorkstream(int id, [FromBody] AssignWorkstreamUserRequest request)
    {
        try
        {
            await _workstreamService.AssignUserToWorkstreamAsync(id, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}/users/{userId}")]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult> RemoveUserFromWorkstream(int id, int userId)
    {
        await _workstreamService.RemoveUserFromWorkstreamAsync(id, userId);
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return null;
        }
        return userId;
    }
}
