using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Controller for Journal Logs - Task 6: Journal Logs
/// Access restricted to Property Hub workstream users or Global Admins.
/// </summary>
[ApiController]
[Route("api/journals")]
[Authorize]
public class JournalsController : ControllerBase
{
    private readonly IJournalLogService _journalLogService;
    private readonly IAuthService _authService;

    public JournalsController(IJournalLogService journalLogService, IAuthService authService)
    {
        _journalLogService = journalLogService;
        _authService = authService;
    }

    /// <summary>
    /// Get all journal logs. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<JournalLogResponseDto>>> GetAllJournalLogs()
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

        // Filter journal logs based on user access (Global Admins and Property Hub Admins see all)
        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var journalLogs = await _journalLogService.GetAllJournalLogsForUserAsync(
            currentUserId.Value, 
            currentUser.IsGlobalAdmin, 
            isPropertyHubAdmin
        );
        return Ok(journalLogs);
    }

    /// <summary>
    /// Get all journal types. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<List<JournalTypeDto>>> GetAllJournalTypes()
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

    /// <summary>
    /// Get journal logs by property ID. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("property/{propertyId}")]
    public async Task<ActionResult<List<JournalLogResponseDto>>> GetJournalLogsByProperty(int propertyId)
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

        var journalLogs = await _journalLogService.GetJournalLogsByPropertyIdAsync(propertyId);
        return Ok(journalLogs);
    }

    /// <summary>
    /// Get journal log by ID. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<JournalLogResponseDto>> GetJournalLog(int id)
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

        var journalLog = await _journalLogService.GetJournalLogByIdAsync(id);
        if (journalLog == null) return NotFound();

        return Ok(journalLog);
    }

    /// <summary>
    /// Create journal log. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<JournalLogResponseDto>> CreateJournalLog([FromBody] CreateJournalLogRequest request)
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

        var journalLog = await _journalLogService.CreateJournalLogAsync(request, currentUserId.Value);
        return CreatedAtAction(nameof(GetJournalLog), new { id = journalLog.JournalLogId }, journalLog);
    }

    /// <summary>
    /// Update journal log. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<JournalLogResponseDto>> UpdateJournalLog(int id, [FromBody] UpdateJournalLogRequest request)
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

        var journalLog = await _journalLogService.UpdateJournalLogAsync(id, request, currentUserId.Value);
        if (journalLog == null) return NotFound();

        return Ok(journalLog);
    }

    /// <summary>
    /// Delete journal log. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteJournalLog(int id)
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

        var result = await _journalLogService.DeleteJournalLogAsync(id, currentUserId.Value);
        if (!result) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Upload attachment to journal log. Only accessible to Property Hub workstream users or Global Admins.
    /// Stores file metadata in tblJournalLogAttachment.
    /// </summary>
    [HttpPost("{id}/attachments")]
    public async Task<ActionResult<AttachmentDto>> AddAttachment(int id, IFormFile file)
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

        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        try
        {
            var attachment = await _journalLogService.AddAttachmentAsync(id, file, currentUserId.Value);
            return Ok(attachment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete attachment. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpDelete("attachments/{attachmentId}")]
    public async Task<ActionResult> DeleteAttachment(int attachmentId)
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

        var result = await _journalLogService.DeleteAttachmentAsync(attachmentId, currentUserId.Value);
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
