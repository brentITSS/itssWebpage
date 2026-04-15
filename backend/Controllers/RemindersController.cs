using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/reminders")]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;
    private readonly IAuthService _authService;

    public RemindersController(IReminderService reminderService, IAuthService authService)
    {
        _reminderService = reminderService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReminderResponseDto>>> GetAll()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var list = await _reminderService.GetAllRemindersForUserAsync(
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReminderResponseDto>> GetById(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var dto = await _reminderService.GetReminderByIdForUserAsync(
            id,
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpGet("{id:int}/ics")]
    public async Task<ActionResult> DownloadReminderAsIcs(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var reminder = await _reminderService.GetReminderByIdForUserAsync(
            id,
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);
        if (reminder == null) return NotFound();

        if (!reminder.ReminderDate.HasValue)
            return BadRequest(new { message = "Reminder date is required before downloading an appointment." });

        var startDate = reminder.ReminderDate.Value.Date;
        var endDateExclusive = startDate.AddDays(1);
        var description = BuildIcsDescription(reminder);
        var uid = $"reminder-{reminder.ReminderId}@itss-property-hub";
        var now = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");

        var ics = string.Join("\r\n", new[]
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//ITSS//Property Hub//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{uid}",
            $"DTSTAMP:{now}",
            $"DTSTART;VALUE=DATE:{startDate:yyyyMMdd}",
            $"DTEND;VALUE=DATE:{endDateExclusive:yyyyMMdd}",
            $"SUMMARY:{EscapeIcsText(reminder.Title)}",
            $"DESCRIPTION:{EscapeIcsText(description)}",
            "STATUS:CONFIRMED",
            "END:VEVENT",
            "END:VCALENDAR"
        }) + "\r\n";

        var bytes = Encoding.UTF8.GetBytes(ics);
        var filename = $"reminder-{reminder.ReminderId}.ics";
        return File(bytes, "text/calendar; charset=utf-8", filename);
    }

    [HttpPost]
    public async Task<ActionResult<ReminderResponseDto>> Create([FromBody] CreateReminderRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        if (!_authService.CanMutatePropertyHubOperationalData(currentUser))
            return Forbid("Access denied: Edit or higher permission is required to create reminders.");

        try
        {
            var created = await _reminderService.CreateReminderAsync(request, currentUser.Email);
            return CreatedAtAction(nameof(GetById), new { id = created.ReminderId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReminderResponseDto>> Update(int id, [FromBody] UpdateReminderRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        if (!_authService.CanMutatePropertyHubOperationalData(currentUser))
            return Forbid("Access denied: Edit or higher permission is required to update reminders.");

        try
        {
            var updated = await _reminderService.UpdateReminderAsync(id, request);
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

        if (!_authService.CanMutatePropertyHubOperationalData(currentUser))
            return Forbid("Access denied: Edit or higher permission is required to delete reminders.");

        var ok = await _reminderService.DeleteReminderAsync(id);
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

    private static string BuildIcsDescription(ReminderResponseDto reminder)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(reminder.Notes))
            lines.Add(reminder.Notes.Trim());

        if (!string.IsNullOrWhiteSpace(reminder.PropertyGroupName))
            lines.Add($"Property group: {reminder.PropertyGroupName}");
        if (!string.IsNullOrWhiteSpace(reminder.PropertyName))
            lines.Add($"Property: {reminder.PropertyName}");
        if (!string.IsNullOrWhiteSpace(reminder.TenancySummary))
            lines.Add($"Tenancy: {reminder.TenancySummary}");
        if (!string.IsNullOrWhiteSpace(reminder.TenantName))
            lines.Add($"Tenant: {reminder.TenantName}");
        if (!string.IsNullOrWhiteSpace(reminder.ReminderPriorityName))
            lines.Add($"Priority: {reminder.ReminderPriorityName}");

        return lines.Count == 0 ? "Reminder from Property Hub." : string.Join(Environment.NewLine, lines);
    }

    private static string EscapeIcsText(string value)
    {
        return value
            .Replace(@"\", @"\\")
            .Replace(";", @"\;")
            .Replace(",", @"\,")
            .Replace("\r\n", @"\n")
            .Replace("\n", @"\n")
            .Replace("\r", string.Empty);
    }
}
