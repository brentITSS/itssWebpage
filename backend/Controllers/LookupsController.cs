using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Controller for Lookup Tables - Task 5: Property Hub Admin
/// Manages Journal Types, Contact Log Types, and Tag Types.
/// Access restricted to Property Hub Admin or Global Admins.
/// </summary>
[ApiController]
[Route("api/lookups")]
[Authorize]
public class LookupsController : ControllerBase
{
    private readonly IJournalLogService _journalLogService;
    private readonly IContactLogService _contactLogService;
    private readonly ITagService _tagService;
    private readonly IAuthService _authService;

    public LookupsController(
        IJournalLogService journalLogService,
        IContactLogService contactLogService,
        ITagService tagService,
        IAuthService authService)
    {
        _journalLogService = journalLogService;
        _contactLogService = contactLogService;
        _tagService = tagService;
        _authService = authService;
    }

    // Journal Types

    /// <summary>
    /// Get all journal types. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("journal-types")]
    public async Task<ActionResult<List<JournalTypeDto>>> GetJournalTypes()
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

        var journalTypes = await _journalLogService.GetAllJournalTypesAsync();
        return Ok(journalTypes);
    }

    // Contact Log Types

    /// <summary>
    /// Get all contact log types. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("contact-log-types")]
    public async Task<ActionResult<List<ContactLogTypeDto>>> GetContactLogTypes()
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

        var contactLogTypes = await _contactLogService.GetAllContactLogTypesAsync();
        return Ok(contactLogTypes);
    }

    // Tag Types

    /// <summary>
    /// Get all tag types. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("tag-types")]
    public async Task<ActionResult<List<TagTypeResponseDto>>> GetTagTypes()
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
    /// Create tag type. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPost("tag-types")]
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
        return Ok(tagType);
    }

    /// <summary>
    /// Update tag type. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPut("tag-types/{id}")]
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
    [HttpDelete("tag-types/{id}")]
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
