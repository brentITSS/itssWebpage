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
    private readonly IMaintenanceService _maintenanceService;
    private readonly IContactLogService _contactLogService;
    private readonly IJournalLogService _journalLogService;
    private readonly IAuthService _authService;

    public CalendarController(
        IReminderService reminderService,
        IMaintenanceService maintenanceService,
        IContactLogService contactLogService,
        IJournalLogService journalLogService,
        IAuthService authService)
    {
        _reminderService = reminderService;
        _maintenanceService = maintenanceService;
        _contactLogService = contactLogService;
        _journalLogService = journalLogService;
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

        var reminderEvents = filtered.Select(r => new CalendarEventDto
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

        var maintenanceRows = await _maintenanceService.GetAllMaintenancesForUserAsync(
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);
        var maintenanceEvents = maintenanceRows
            .Where(x => x.HasCalendarAppointment && x.CalendarDate.HasValue)
            .Where(x => !propertyGroupId.HasValue || x.PropertyGroupId == propertyGroupId.Value)
            .Where(x => !propertyId.HasValue || x.PropertyId == propertyId.Value)
            .Where(x => !from.HasValue || x.CalendarDate!.Value.Date >= from.Value.Date)
            .Where(x => !to.HasValue || x.CalendarDate!.Value.Date <= to.Value.Date)
            .Select(x => new CalendarEventDto
            {
                EventType = "maintenance",
                SourceId = x.MaintenanceId,
                Title = string.IsNullOrWhiteSpace(x.Summary) ? $"Maintenance #{x.MaintenanceId}" : x.Summary!,
                Start = x.CalendarDate!.Value.Date,
                End = x.CalendarDate!.Value.Date.AddDays(1),
                IsAllDay = true,
                Description = BuildMaintenanceDescription(x),
                IsCompleted = IsMaintenanceCompleted(x),
                Color = IsMaintenanceCompleted(x) ? "#94a3b8" : "#0ea5e9",
                PropertyGroupId = x.PropertyGroupId,
                PropertyGroupName = x.PropertyGroupName,
                PropertyId = x.PropertyId,
                PropertyName = x.PropertyName
            })
            .Where(x => includeCompleted || !x.IsCompleted)
            .ToList();

        var contactRows = await _contactLogService.GetAllContactLogsForUserAsync(
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);
        var contactEvents = contactRows
            .Where(x => x.HasCalendarAppointment && x.CalendarDate.HasValue)
            .Where(x => !propertyGroupId.HasValue || x.PropertyGroupId == propertyGroupId.Value)
            .Where(x => !propertyId.HasValue || x.PropertyId == propertyId.Value)
            .Where(x => !tenantId.HasValue || x.TenantId == tenantId.Value)
            .Where(x => !from.HasValue || x.CalendarDate!.Value.Date >= from.Value.Date)
            .Where(x => !to.HasValue || x.CalendarDate!.Value.Date <= to.Value.Date)
            .Select(x => new CalendarEventDto
            {
                EventType = "contactLog",
                SourceId = x.ContactLogId,
                Title = string.IsNullOrWhiteSpace(x.Subject) ? "Contact log" : x.Subject,
                Start = x.CalendarDate!.Value.Date,
                End = x.CalendarDate!.Value.Date.AddDays(1),
                IsAllDay = true,
                Description = BuildContactLogDescription(x),
                IsCompleted = true,
                Color = "#7c3aed",
                PropertyGroupId = x.PropertyGroupId,
                PropertyGroupName = x.PropertyGroupName,
                PropertyId = x.PropertyId,
                PropertyName = x.PropertyName,
                TenantId = x.TenantId,
                TenantName = x.TenantName
            })
            .ToList();

        var journalRows = await _journalLogService.GetAllJournalLogsForUserAsync(
            currentUserId.Value,
            currentUser.IsGlobalAdmin,
            isPropertyHubAdmin);
        var journalEvents = journalRows
            .Where(x => x.HasCalendarAppointment && x.CalendarDate.HasValue)
            .Where(x => !propertyGroupId.HasValue || x.PropertyGroupId == propertyGroupId.Value)
            .Where(x => !propertyId.HasValue || x.PropertyId == propertyId.Value)
            .Where(x => !tenancyId.HasValue || x.TenancyId == tenancyId.Value)
            .Where(x => !tenantId.HasValue || x.TenantId == tenantId.Value)
            .Where(x => !from.HasValue || x.CalendarDate!.Value.Date >= from.Value.Date)
            .Where(x => !to.HasValue || x.CalendarDate!.Value.Date <= to.Value.Date)
            .Select(x => new CalendarEventDto
            {
                EventType = "journalLog",
                SourceId = x.JournalLogId,
                Title = string.IsNullOrWhiteSpace(x.Description)
                    ? $"{x.JournalTypeName} journal"
                    : x.Description!.Length > 90
                        ? $"{x.Description.Substring(0, 90)}…"
                        : x.Description,
                Start = x.CalendarDate!.Value.Date,
                End = x.CalendarDate!.Value.Date.AddDays(1),
                IsAllDay = true,
                Description = BuildJournalLogDescription(x),
                IsCompleted = true,
                Color = "#0f766e",
                PropertyGroupId = x.PropertyGroupId,
                PropertyGroupName = x.PropertyGroupName,
                PropertyId = x.PropertyId,
                PropertyName = x.PropertyName,
                TenancyId = x.TenancyId,
                TenantId = x.TenantId,
                TenantName = x.TenantName
            })
            .ToList();

        var events = reminderEvents
            .Concat(maintenanceEvents)
            .Concat(contactEvents)
            .Concat(journalEvents)
            .OrderBy(x => x.Start)
            .ThenBy(x => x.Title)
            .ToList();

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

    private static string BuildMaintenanceDescription(MaintenanceResponseDto maintenance)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(maintenance.DetailNotes))
            lines.Add(maintenance.DetailNotes.Trim());
        if (!string.IsNullOrWhiteSpace(maintenance.MaintenanceTypeName))
            lines.Add($"Type: {maintenance.MaintenanceTypeName}");
        if (!string.IsNullOrWhiteSpace(maintenance.MaintenanceStatusName))
            lines.Add($"Status: {maintenance.MaintenanceStatusName}");
        if (!string.IsNullOrWhiteSpace(maintenance.PropertyGroupName))
            lines.Add($"Property group: {maintenance.PropertyGroupName}");
        if (!string.IsNullOrWhiteSpace(maintenance.PropertyName))
            lines.Add($"Property: {maintenance.PropertyName}");

        return lines.Count == 0 ? "Maintenance item from Property Hub." : string.Join(Environment.NewLine, lines);
    }

    private static string BuildContactLogDescription(ContactLogResponseDto contactLog)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(contactLog.Notes))
            lines.Add(contactLog.Notes.Trim());
        if (!string.IsNullOrWhiteSpace(contactLog.ContactLogTypeName))
            lines.Add($"Type: {contactLog.ContactLogTypeName}");
        if (!string.IsNullOrWhiteSpace(contactLog.PropertyGroupName))
            lines.Add($"Property group: {contactLog.PropertyGroupName}");
        if (!string.IsNullOrWhiteSpace(contactLog.PropertyName))
            lines.Add($"Property: {contactLog.PropertyName}");
        if (!string.IsNullOrWhiteSpace(contactLog.TenantName))
            lines.Add($"Tenant: {contactLog.TenantName}");

        return lines.Count == 0 ? "Contact log from Property Hub." : string.Join(Environment.NewLine, lines);
    }

    private static string BuildJournalLogDescription(JournalLogResponseDto journalLog)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(journalLog.Description))
            lines.Add(journalLog.Description.Trim());
        if (!string.IsNullOrWhiteSpace(journalLog.JournalTypeName))
            lines.Add($"Journal type: {journalLog.JournalTypeName}");
        if (!string.IsNullOrWhiteSpace(journalLog.JournalSubTypeName))
            lines.Add($"Journal subtype: {journalLog.JournalSubTypeName}");
        lines.Add($"Amount: {journalLog.Amount:0.00}");
        if (!string.IsNullOrWhiteSpace(journalLog.PropertyGroupName))
            lines.Add($"Property group: {journalLog.PropertyGroupName}");
        if (!string.IsNullOrWhiteSpace(journalLog.PropertyName))
            lines.Add($"Property: {journalLog.PropertyName}");
        if (!string.IsNullOrWhiteSpace(journalLog.TenantName))
            lines.Add($"Tenant: {journalLog.TenantName}");

        return lines.Count == 0 ? "Journal log from Property Hub." : string.Join(Environment.NewLine, lines);
    }

    private static bool IsMaintenanceCompleted(MaintenanceResponseDto maintenance)
    {
        var status = maintenance.MaintenanceStatusName?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(status)) return false;

        return status.Contains("complete")
            || status.Contains("closed")
            || status.Contains("resolved")
            || status.Contains("cancel")
            || status.Contains("done");
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
