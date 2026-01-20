using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Controller for Contact Logs - Task 7: Contact Logs
/// Access restricted to Property Hub workstream users or Global Admins.
/// </summary>
[ApiController]
[Route("api/contact-logs")]
[Authorize]
public class ContactLogsController : ControllerBase
{
    private readonly IContactLogService _contactLogService;
    private readonly IAuthService _authService;

    public ContactLogsController(IContactLogService contactLogService, IAuthService authService)
    {
        _contactLogService = contactLogService;
        _authService = authService;
    }

    /// <summary>
    /// Get all contact logs. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ContactLogResponseDto>>> GetAllContactLogs()
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

        // Filter contact logs based on user access (Global Admins and Property Hub Admins see all)
        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var contactLogs = await _contactLogService.GetAllContactLogsForUserAsync(
            currentUserId.Value, 
            currentUser.IsGlobalAdmin, 
            isPropertyHubAdmin
        );
        return Ok(contactLogs);
    }

    /// <summary>
    /// Get all contact log types. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<List<ContactLogTypeDto>>> GetAllContactLogTypes()
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

    /// <summary>
    /// Get contact logs by property ID. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("property/{propertyId}")]
    public async Task<ActionResult<List<ContactLogResponseDto>>> GetContactLogsByProperty(int propertyId)
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

        var contactLogs = await _contactLogService.GetContactLogsByPropertyIdAsync(propertyId);
        return Ok(contactLogs);
    }

    /// <summary>
    /// Get contact logs by tenant ID. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    public async Task<ActionResult<List<ContactLogResponseDto>>> GetContactLogsByTenant(int tenantId)
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

        var contactLogs = await _contactLogService.GetContactLogsByTenantIdAsync(tenantId);
        return Ok(contactLogs);
    }

    /// <summary>
    /// Get contact log by ID. Accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ContactLogResponseDto>> GetContactLog(int id)
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

        var contactLog = await _contactLogService.GetContactLogByIdAsync(id);
        if (contactLog == null) return NotFound();

        return Ok(contactLog);
    }

    /// <summary>
    /// Create contact log. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ContactLogResponseDto>> CreateContactLog([FromBody] CreateContactLogRequest request)
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

        var contactLog = await _contactLogService.CreateContactLogAsync(request, currentUserId.Value);
        return CreatedAtAction(nameof(GetContactLog), new { id = contactLog.ContactLogId }, contactLog);
    }

    /// <summary>
    /// Update contact log. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ContactLogResponseDto>> UpdateContactLog(int id, [FromBody] UpdateContactLogRequest request)
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

        var contactLog = await _contactLogService.UpdateContactLogAsync(id, request, currentUserId.Value);
        if (contactLog == null) return NotFound();

        return Ok(contactLog);
    }

    /// <summary>
    /// Delete contact log. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteContactLog(int id)
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

        var result = await _contactLogService.DeleteContactLogAsync(id, currentUserId.Value);
        if (!result) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Upload attachment to contact log. Only accessible to Property Hub workstream users or Global Admins.
    /// Stores file metadata in tblContactLogAttachment.
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
            var attachment = await _contactLogService.AddAttachmentAsync(id, file, currentUserId.Value);
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

        var result = await _contactLogService.DeleteAttachmentAsync(attachmentId, currentUserId.Value);
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
