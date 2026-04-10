using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly IAuthService _authService;

    public MaintenanceController(IMaintenanceService maintenanceService, IAuthService authService)
    {
        _maintenanceService = maintenanceService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<List<MaintenanceResponseDto>>> GetAll()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var list = await _maintenanceService.GetAllMaintenancesForUserAsync(
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaintenanceResponseDto>> GetById(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var dto = await _maintenanceService.GetMaintenanceByIdForUserAsync(
            id,
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceResponseDto>> Create([FromBody] CreateMaintenanceRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        try
        {
            var created = await _maintenanceService.CreateMaintenanceAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.MaintenanceId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MaintenanceResponseDto>> Update(int id, [FromBody] UpdateMaintenanceRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        try
        {
            var updated = await _maintenanceService.UpdateMaintenanceAsync(id, request);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        var ok = await _maintenanceService.DeleteMaintenanceAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    private bool HasPropertyHubAccess(UserDto user)
    {
        if (user.IsGlobalAdmin) return true;

        return user.WorkstreamAccess.Any(wa =>
            wa.WorkstreamName.Equals("Property Hub", StringComparison.OrdinalIgnoreCase) ||
            wa.WorkstreamName.Contains("Property", StringComparison.OrdinalIgnoreCase));
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return null;
        return userId;
    }
}
