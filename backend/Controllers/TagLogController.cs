using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Controller for Tag Logs - Task 8: Tagging System
/// Manages assignment and removal of tags to/from entities (Journal Logs, Contact Logs, Tenants, Properties, Property Groups).
/// Access restricted to Property Hub workstream users or Global Admins.
/// </summary>
[ApiController]
[Route("api/tag-log")]
[Authorize]
public class TagLogController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IAuthService _authService;

    public TagLogController(ITagService tagService, IAuthService authService)
    {
        _tagService = tagService;
        _authService = authService;
    }

    /// <summary>
    /// Get tag logs for a specific entity. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TagDto>>> GetTagLogsByEntity([FromQuery] string entityType, [FromQuery] int entityId)
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

        var tagLogs = await _tagService.GetTagLogsByEntityAsync(entityType, entityId);
        return Ok(tagLogs);
    }

    /// <summary>
    /// Create a tag log (assign a tag to an entity). Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTagLog([FromBody] CreateTagLogRequest request)
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

        // Validate entity type
        var validEntityTypes = new[] { "Property", "PropertyGroup", "Tenant", "ContactLog", "JournalLog" };
        if (!validEntityTypes.Contains(request.EntityType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Invalid entity type. Valid types are: {string.Join(", ", validEntityTypes)}");
        }

        // Check if tag already exists for this entity
        var existingTags = await _tagService.GetTagLogsByEntityAsync(request.EntityType, request.EntityId);
        if (existingTags.Any(t => t.TagTypeId == request.TagTypeId))
        {
            return BadRequest("This tag is already assigned to this entity");
        }

        var tagLog = await _tagService.CreateTagLogAsync(request, currentUserId.Value);
        return Ok(tagLog);
    }

    /// <summary>
    /// Delete a tag log (remove a tag from an entity). Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTagLog(int id)
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

        var result = await _tagService.DeleteTagLogAsync(id, currentUserId.Value);
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
