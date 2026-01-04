using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Controller for Properties - Task 4: Property Hub Home
/// Access restricted to Property Hub workstream users or Global Admins.
/// </summary>
[ApiController]
[Route("api/properties")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly IAuthService _authService;

    public PropertiesController(IPropertyService propertyService, IAuthService authService)
    {
        _propertyService = propertyService;
        _authService = authService;
    }

    /// <summary>
    /// Get all properties. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PropertyResponseDto>>> GetAllProperties()
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

        var properties = await _propertyService.GetAllPropertiesAsync();
        return Ok(properties);
    }

    /// <summary>
    /// Get property by ID. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PropertyResponseDto>> GetProperty(int id)
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

        var property = await _propertyService.GetPropertyByIdAsync(id);
        if (property == null) return NotFound();

        return Ok(property);
    }

    /// <summary>
    /// Create property. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PropertyResponseDto>> CreateProperty([FromBody] CreatePropertyRequest request)
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

        var property = await _propertyService.CreatePropertyAsync(request, currentUserId.Value);
        return CreatedAtAction(nameof(GetProperty), new { id = property.PropertyId }, property);
    }

    /// <summary>
    /// Update property. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PropertyResponseDto>> UpdateProperty(int id, [FromBody] UpdatePropertyRequest request)
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

        var property = await _propertyService.UpdatePropertyAsync(id, request, currentUserId.Value);
        if (property == null) return NotFound();

        return Ok(property);
    }

    /// <summary>
    /// Delete property. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProperty(int id)
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

        var result = await _propertyService.DeletePropertyAsync(id, currentUserId.Value);
        if (!result) return NotFound();

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
