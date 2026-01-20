using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Controller for Property Groups - Task 4: Property Hub Home
/// Access restricted to Property Hub workstream users or Global Admins.
/// </summary>
[ApiController]
[Route("api/property-groups")]
[Authorize]
public class PropertyGroupsController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly IAuthService _authService;

    public PropertyGroupsController(IPropertyService propertyService, IAuthService authService)
    {
        _propertyService = propertyService;
        _authService = authService;
    }

    /// <summary>
    /// Get all property groups. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PropertyGroupResponseDto>>> GetAllPropertyGroups()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub workstream access
        if (!HasPropertyHubAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub workstream access required");
        }

        // Filter property groups based on user access (Global Admins and Property Hub Admins see all)
        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var propertyGroups = await _propertyService.GetAllPropertyGroupsForUserAsync(
            currentUserId.Value, 
            currentUser.IsGlobalAdmin, 
            isPropertyHubAdmin
        );
        return Ok(propertyGroups);
    }

    /// <summary>
    /// Get property group by ID. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PropertyGroupResponseDto>> GetPropertyGroup(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub workstream access
        if (!HasPropertyHubAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub workstream access required");
        }

        var propertyGroup = await _propertyService.GetPropertyGroupByIdAsync(id);
        if (propertyGroup == null) return NotFound();

        return Ok(propertyGroup);
    }

    /// <summary>
    /// Create property group. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PropertyGroupResponseDto>> CreatePropertyGroup([FromBody] CreatePropertyGroupRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub Admin access
        if (!_authService.HasPropertyHubAdminAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub Admin permission required");
        }

        var propertyGroup = await _propertyService.CreatePropertyGroupAsync(request, currentUserId.Value);
        return CreatedAtAction(nameof(GetPropertyGroup), new { id = propertyGroup.PropertyGroupId }, propertyGroup);
    }

    /// <summary>
    /// Update property group. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PropertyGroupResponseDto>> UpdatePropertyGroup(int id, [FromBody] UpdatePropertyGroupRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub Admin access
        if (!_authService.HasPropertyHubAdminAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub Admin permission required");
        }

        var propertyGroup = await _propertyService.UpdatePropertyGroupAsync(id, request, currentUserId.Value);
        if (propertyGroup == null) return NotFound();

        return Ok(propertyGroup);
    }

    /// <summary>
    /// Delete property group. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePropertyGroup(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub Admin access
        if (!_authService.HasPropertyHubAdminAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub Admin permission required");
        }

        var result = await _propertyService.DeletePropertyGroupAsync(id, currentUserId.Value);
        if (!result) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Get users assigned to a property group. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpGet("{id}/users")]
    public async Task<ActionResult<List<UserResponseDto>>> GetPropertyGroupUsers(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub Admin access
        if (!_authService.HasPropertyHubAdminAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub Admin permission required");
        }

        var users = await _propertyService.GetPropertyGroupUsersAsync(id);
        return Ok(users);
    }

    /// <summary>
    /// Assign user to property group. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPost("{id}/users")]
    public async Task<ActionResult> AssignUserToPropertyGroup(int id, [FromBody] AssignPropertyGroupUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub Admin access
        if (!_authService.HasPropertyHubAdminAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub Admin permission required");
        }

        try
        {
            await _propertyService.AssignUserToPropertyGroupAsync(id, request.UserId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Remove user from property group. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpDelete("{id}/users/{userId}")]
    public async Task<ActionResult> RemoveUserFromPropertyGroup(int id, int userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub Admin access
        if (!_authService.HasPropertyHubAdminAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub Admin permission required");
        }

        await _propertyService.RemoveUserFromPropertyGroupAsync(id, userId);
        return NoContent();
    }

    /// <summary>
    /// Check if user has Property Hub workstream access.
    /// Global Admins have access to all workstreams.
    /// </summary>
    private bool HasPropertyHubAccess(DTOs.UserDto user)
    {
        if (user.IsGlobalAdmin) return true;

        // Check if user has access to Property Hub workstream
        return user.WorkstreamAccess.Any(wa =>
            wa.WorkstreamName.Equals("Property Hub", StringComparison.OrdinalIgnoreCase) ||
            wa.WorkstreamName.Contains("Property", StringComparison.OrdinalIgnoreCase));
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
