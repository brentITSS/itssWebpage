using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly IReminderService _reminderService;
    private readonly IAuthService _authService;

    public CalendarController(IReminderService reminderService, IAuthService authService)
    {
        _reminderService = reminderService;
        _authService = authService;
    }

    [HttpGet("events")]
    public async Task<ActionResult<List<CalendarEventDto>>> GetEvents(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? propertyGroupId,
        [FromQuery] int? propertyId,
        [FromQuery] int? tenancyId,
        [FromQuery] int? tenantId,
        [FromQuery] bool includeCompleted = true)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        if (!HasPropertyHubAccess(currentUser))
            return Forbid("Access denied: Property Hub workstream access required");

        var isPropertyHubAdmin = _authService.HasPropertyHubAdminAccess(currentUser);
        var reminders = await _reminderService.GetAllRemindersForUserAsync(
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);

        var filtered = reminders
            .Where(r => r.ReminderDate.HasValue)
            .Where(r => includeCompleted || !r.IsCompleted)
            .Where(r => !propertyGroupId.HasValue || r.PropertyGroupId == propertyGroupId.Value)
            .Where(r => !propertyId.HasValue || r.PropertyId == propertyId.Value)
            .Where(r => !tenancyId.HasValue || r.TenancyId == tenancyId.Value)
            .Where(r => !tenantId.HasValue || r.TenantId == tenantId.Value)
            .Where(r => !from.HasValue || r.ReminderDate!.Value.Date >= from.Value.Date)
            .Where(r => !to.HasValue || r.ReminderDate!.Value.Date <= to.Value.Date)
            .OrderBy(r => r.ReminderDate)
            .ThenBy(r => r.CreatedDate)
            .ToList();

        var events = filtered.Select(r => new CalendarEventDto
        {
            EventType = "reminder",
            SourceId = r.ReminderId,
            Title = r.Title,
            Start = r.ReminderDate!.Value.Date,
            End = r.ReminderDate!.Value.Date.AddDays(1),
            IsAllDay = true,
            Description = BuildReminderDescription(r),
            IsCompleted = r.IsCompleted,
            Color = r.IsCompleted ? "#94a3b8" : r.ReminderPriorityColor ?? "#2563eb",
            PropertyGroupId = r.PropertyGroupId,
            PropertyGroupName = r.PropertyGroupName,
            PropertyId = r.PropertyId,
            PropertyName = r.PropertyName,
            TenancyId = r.TenancyId,
            TenancySummary = r.TenancySummary,
            TenantId = r.TenantId,
            TenantName = r.TenantName
        }).ToList();

        return Ok(events);
    }

    private static string BuildReminderDescription(ReminderResponseDto reminder)
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
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }
}
