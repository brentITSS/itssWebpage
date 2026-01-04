using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Controller for Tag Types - Task 8: Tagging System
/// Access restricted to Property Hub workstream users or Global Admins.
/// </summary>
[ApiController]
[Route("api/tags")]
[Authorize]
public class TagTypesController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IAuthService _authService;

    public TagTypesController(ITagService tagService, IAuthService authService)
    {
        _tagService = tagService;
        _authService = authService;
    }

    /// <summary>
    /// Get all tag types. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TagTypeResponseDto>>> GetAllTagTypes()
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

        var tagTypes = await _tagService.GetAllTagTypesAsync();
        return Ok(tagTypes);
    }

    /// <summary>
    /// Get tag type by ID. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TagTypeResponseDto>> GetTagType(int id)
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

        var tagType = await _tagService.GetTagTypeByIdAsync(id);
        if (tagType == null) return NotFound();

        return Ok(tagType);
    }

    /// <summary>
    /// Create tag type. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TagTypeResponseDto>> CreateTagType([FromBody] CreateTagTypeRequest request)
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

        var tagType = await _tagService.CreateTagTypeAsync(request);
        return CreatedAtAction(nameof(GetTagType), new { id = tagType.TagTypeId }, tagType);
    }

    /// <summary>
    /// Update tag type. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TagTypeResponseDto>> UpdateTagType(int id, [FromBody] UpdateTagTypeRequest request)
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

        var tagType = await _tagService.UpdateTagTypeAsync(id, request);
        if (tagType == null) return NotFound();

        return Ok(tagType);
    }

    /// <summary>
    /// Delete tag type. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTagType(int id)
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

        var result = await _tagService.DeleteTagTypeAsync(id);
        if (!result) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Check if user has Property Hub workstream access.
    /// </summary>
    private bool HasPropertyHubAccess(DTOs.UserDto user)
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
        {
            return null;
        }
        return userId;
    }
}
