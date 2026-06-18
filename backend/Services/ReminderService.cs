using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class ReminderService : IReminderService
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IPropertyHubEmailService _emailService;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        IReminderRepository reminderRepository,
        IPropertyRepository propertyRepository,
        ITenantRepository tenantRepository,
        IPropertyHubEmailService emailService,
        ILogger<ReminderService> logger)
    {
        _reminderRepository = reminderRepository;
        _propertyRepository = propertyRepository;
        _tenantRepository = tenantRepository;
        _emailService = emailService;
        _logger = logger;
    }

    private static int? ResolvePropertyGroupId(Reminder r)
    {
        return r.PropertyGroupId
            ?? r.Property?.PropertyGroupId
            ?? r.Tenancy?.Property?.PropertyGroupId
            ?? r.Tenant?.Tenancy?.Property?.PropertyGroupId;
    }

    private static void EnsureHasScopeLink(Reminder reminder)
    {
        if (!reminder.PropertyGroupId.HasValue && !reminder.PropertyId.HasValue
            && !reminder.TenancyId.HasValue && !reminder.TenantId.HasValue)
        {
            throw new InvalidOperationException(
                "Link the reminder to a property group, property, tenancy, or tenant.");
        }
    }

    private static bool CanAccessReminder(Reminder r, List<int> userGroupIds, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        if (isGlobalAdmin || isPropertyHubAdmin) return true;
        if (userGroupIds.Count == 0) return true;

        var grp = ResolvePropertyGroupId(r);
        if (grp.HasValue && userGroupIds.Contains(grp.Value))
            return true;

        if (!r.PropertyGroupId.HasValue && !r.PropertyId.HasValue && !r.TenancyId.HasValue && !r.TenantId.HasValue)
            return true;

        return false;
    }

    public async Task<List<ReminderResponseDto>> GetAllRemindersForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        var all = await _reminderRepository.GetAllAsync();
        var userGroupIds = await _propertyRepository.GetUserPropertyGroupIdsAsync(userId);

        return all
            .Where(r => CanAccessReminder(r, userGroupIds, isGlobalAdmin, isPropertyHubAdmin))
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<ReminderResponseDto>> GetOverdueRemindersForUserAsync(
        int userId,
        bool isGlobalAdmin,
        bool isPropertyHubAdmin,
        int? propertyGroupId,
        int? propertyId,
        int? tenancyId,
        int? tenantId)
    {
        var all = await _reminderRepository.GetAllAsync();
        var userGroupIds = await _propertyRepository.GetUserPropertyGroupIdsAsync(userId);
        var todayUtc = DateTime.UtcNow.Date;

        return all
            .Where(r => CanAccessReminder(r, userGroupIds, isGlobalAdmin, isPropertyHubAdmin))
            .Where(r => r.ReminderDate.HasValue && r.ReminderDate.Value.Date < todayUtc)
            .Where(r => r.ReminderActive != false)
            .Where(r => !propertyGroupId.HasValue || r.PropertyGroupId == propertyGroupId.Value)
            .Where(r => !propertyId.HasValue || r.PropertyId == propertyId.Value)
            .Where(r => !tenancyId.HasValue || r.TenancyId == tenancyId.Value)
            .Where(r => !tenantId.HasValue || r.TenantId == tenantId.Value)
            .OrderBy(r => r.ReminderDate ?? DateTime.MaxValue)
            .ThenByDescending(r => r.CreatedDate ?? DateTime.MinValue)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ReminderResponseDto?> GetReminderByIdForUserAsync(int reminderId, int userId, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        var r = await _reminderRepository.GetByIdAsync(reminderId);
        if (r == null) return null;

        var userGroupIds = await _propertyRepository.GetUserPropertyGroupIdsAsync(userId);
        if (!CanAccessReminder(r, userGroupIds, isGlobalAdmin, isPropertyHubAdmin))
            return null;

        return MapToDto(r);
    }

    public async Task<ReminderResponseDto> CreateReminderAsync(CreateReminderRequest request, string? createdBy)
    {
        var reminder = new Reminder
        {
            TenantId = request.TenantId,
            TenancyId = request.TenancyId,
            PropertyGroupId = request.PropertyGroupId,
            PropertyId = request.PropertyId,
            Title = request.Title,
            ReminderPriorityId = request.ReminderPriorityId,
            Notes = request.Notes,
            ReminderDate = request.ReminderDate,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            ReminderActive = request.IsCompleted ? false : true,
        };

        await NormalizeLinksAsync(reminder);
        EnsureHasScopeLink(reminder);
        reminder = await _reminderRepository.CreateAsync(reminder);
        var loaded = await _reminderRepository.GetByIdAsync(reminder.ReminderId);
        var dto = MapToDto(loaded!);

        if (request.SendEmailReminder)
        {
            if (string.IsNullOrWhiteSpace(request.EmailRecipient))
            {
                throw new InvalidOperationException("Email recipient is required when sending an email reminder.");
            }

            if (!_emailService.IsConfigured)
            {
                dto.EmailNotificationSent = false;
                dto.EmailNotificationError = "Property Hub email is not configured on the server.";
                return dto;
            }

            try
            {
                var (subject, body) = BuildReminderEmailContent(dto, createdBy);
                await _emailService.SendEmailAsync(request.EmailRecipient.Trim(), subject, body);
                dto.EmailNotificationSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reminder {ReminderId} was created but email notification failed.", dto.ReminderId);
                dto.EmailNotificationSent = false;
                dto.EmailNotificationError = ex.Message;
            }
        }

        return dto;
    }

    public async Task<ReminderResponseDto?> UpdateReminderAsync(int reminderId, UpdateReminderRequest request)
    {
        var reminder = await _reminderRepository.GetByIdAsync(reminderId);
        if (reminder == null) return null;

        reminder.TenantId = request.TenantId;
        reminder.TenancyId = request.TenancyId;
        reminder.PropertyGroupId = request.PropertyGroupId;
        reminder.PropertyId = request.PropertyId;
        reminder.ReminderPriorityId = request.ReminderPriorityId;
        if (request.Title != null) reminder.Title = request.Title;
        if (request.Notes != null) reminder.Notes = request.Notes;
        reminder.ReminderDate = request.ReminderDate;
        if (request.IsCompleted.HasValue)
            reminder.ReminderActive = request.IsCompleted.Value ? false : true;

        await NormalizeLinksAsync(reminder);
        EnsureHasScopeLink(reminder);
        await _reminderRepository.UpdateAsync(reminder);
        var loaded = await _reminderRepository.GetByIdAsync(reminderId);
        return MapToDto(loaded!);
    }

    public async Task<bool> DeleteReminderAsync(int reminderId)
    {
        return await _reminderRepository.DeleteAsync(reminderId);
    }

    public async Task<List<ReminderPriorityDto>> GetAllReminderPrioritiesAsync()
    {
        var list = await _reminderRepository.GetAllReminderPrioritiesAsync();
        return list.Select(MapPriorityToDto).ToList();
    }

    public async Task<ReminderPriorityDto?> GetReminderPriorityByIdAsync(int id)
    {
        var e = await _reminderRepository.GetReminderPriorityByIdAsync(id);
        return e == null ? null : MapPriorityToDto(e);
    }

    public async Task<ReminderPriorityDto> CreateReminderPriorityAsync(CreateReminderPriorityRequest request)
    {
        var entity = new ReminderPriority
        {
            ReminderPriorityName = request.ReminderPriorityName,
            Description = request.Description,
            DisplayColor = request.DisplayColor,
            SortOrder = request.SortOrder ?? 0,
            IsActive = request.IsActive ?? true,
            CreatedDate = DateTime.UtcNow,
        };
        entity = await _reminderRepository.CreateReminderPriorityAsync(entity);
        return MapPriorityToDto(entity);
    }

    public async Task<ReminderPriorityDto?> UpdateReminderPriorityAsync(int id, UpdateReminderPriorityRequest request)
    {
        var entity = await _reminderRepository.GetReminderPriorityByIdAsync(id);
        if (entity == null) return null;

        if (request.ReminderPriorityName != null) entity.ReminderPriorityName = request.ReminderPriorityName;
        if (request.Description != null) entity.Description = request.Description;
        if (request.DisplayColor != null) entity.DisplayColor = request.DisplayColor;
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive;

        entity = await _reminderRepository.UpdateReminderPriorityAsync(entity);
        return MapPriorityToDto(entity);
    }

    public async Task<bool> IsReminderPriorityInUseAsync(int id)
    {
        return await _reminderRepository.CountRemindersByPriorityAsync(id) > 0;
    }

    public async Task<bool> DeleteReminderPriorityAsync(int id)
    {
        return await _reminderRepository.DeleteReminderPriorityAsync(id);
    }

    private async Task NormalizeLinksAsync(Reminder reminder)
    {
        if (reminder.TenancyId.HasValue)
        {
            var tn = await _tenantRepository.GetTenancyByIdAsync(reminder.TenancyId.Value);
            if (tn != null)
            {
                reminder.PropertyId = tn.PropertyId;
                reminder.PropertyGroupId = tn.Property?.PropertyGroupId ?? reminder.PropertyGroupId;
            }
        }
        else if (reminder.TenantId.HasValue)
        {
            var tenant = await _tenantRepository.GetByIdAsync(reminder.TenantId.Value);
            if (tenant?.TenancyId != null)
            {
                reminder.TenancyId = tenant.TenancyId;
                var tn = await _tenantRepository.GetTenancyByIdAsync(tenant.TenancyId.Value);
                if (tn != null)
                {
                    reminder.PropertyId = tn.PropertyId;
                    reminder.PropertyGroupId = tn.Property?.PropertyGroupId ?? reminder.PropertyGroupId;
                }
            }
        }
        else if (reminder.PropertyId.HasValue)
        {
            var prop = await _propertyRepository.GetPropertyByIdAsync(reminder.PropertyId.Value);
            if (prop?.PropertyGroupId != null)
                reminder.PropertyGroupId = prop.PropertyGroupId;
        }
    }

    private static ReminderResponseDto MapToDto(Reminder r)
    {
        var tenantName = r.Tenant != null
            ? $"{r.Tenant.FirstName} {r.Tenant.LastName}".Trim()
            : null;
        var tenancySummary = r.Tenancy != null
            ? (string.IsNullOrWhiteSpace(r.Tenancy.Description)
                ? $"Tenancy #{r.Tenancy.TenancyId}"
                : r.Tenancy.Description!.Length > 80
                    ? r.Tenancy.Description[..80] + "…"
                    : r.Tenancy.Description)
            : null;

        return new ReminderResponseDto
        {
            ReminderId = r.ReminderId,
            TenantId = r.TenantId,
            TenantName = string.IsNullOrEmpty(tenantName) ? null : tenantName,
            TenancyId = r.TenancyId,
            TenancySummary = tenancySummary,
            PropertyGroupId = r.PropertyGroupId,
            PropertyGroupName = r.PropertyGroup?.PropertyGroupName,
            PropertyId = r.PropertyId,
            PropertyName = r.Property?.PropertyName,
            Title = r.Title,
            ReminderPriorityId = r.ReminderPriorityId,
            ReminderPriorityName = r.ReminderPriority?.ReminderPriorityName,
            ReminderPriorityColor = r.ReminderPriority?.DisplayColor,
            Notes = r.Notes,
            CreatedBy = r.CreatedBy,
            CreatedDate = r.CreatedDate,
            ReminderDate = r.ReminderDate,
            ReminderActive = r.ReminderActive,
            IsCompleted = r.ReminderActive == false,
        };
    }

    private static (string Subject, string BodyHtml) BuildReminderEmailContent(ReminderResponseDto reminder, string? createdBy)
    {
        var subject = $"Property Hub reminder: {reminder.Title}";
        var lines = new List<string>
        {
            $"<p><strong>{System.Net.WebUtility.HtmlEncode(reminder.Title)}</strong></p>",
        };

        if (reminder.ReminderDate.HasValue)
        {
            lines.Add($"<p><strong>Reminder date:</strong> {reminder.ReminderDate.Value:yyyy-MM-dd}</p>");
        }

        if (!string.IsNullOrWhiteSpace(reminder.Notes))
        {
            lines.Add($"<p><strong>Detail:</strong><br/>{System.Net.WebUtility.HtmlEncode(reminder.Notes).Replace("\n", "<br/>", StringComparison.Ordinal)}</p>");
        }

        if (!string.IsNullOrWhiteSpace(reminder.PropertyGroupName))
            lines.Add($"<p><strong>Property group:</strong> {System.Net.WebUtility.HtmlEncode(reminder.PropertyGroupName)}</p>");
        if (!string.IsNullOrWhiteSpace(reminder.PropertyName))
            lines.Add($"<p><strong>Property:</strong> {System.Net.WebUtility.HtmlEncode(reminder.PropertyName)}</p>");
        if (!string.IsNullOrWhiteSpace(reminder.TenancySummary))
            lines.Add($"<p><strong>Tenancy:</strong> {System.Net.WebUtility.HtmlEncode(reminder.TenancySummary)}</p>");
        if (!string.IsNullOrWhiteSpace(reminder.TenantName))
            lines.Add($"<p><strong>Tenant:</strong> {System.Net.WebUtility.HtmlEncode(reminder.TenantName)}</p>");
        if (!string.IsNullOrWhiteSpace(reminder.ReminderPriorityName))
            lines.Add($"<p><strong>Priority:</strong> {System.Net.WebUtility.HtmlEncode(reminder.ReminderPriorityName)}</p>");
        if (!string.IsNullOrWhiteSpace(createdBy))
            lines.Add($"<p><strong>Created by:</strong> {System.Net.WebUtility.HtmlEncode(createdBy)}</p>");

        var body = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2937;">
            <h2 style="margin-top:0;">Property Hub reminder</h2>
            {string.Join("\n", lines)}
            <p style="color:#6b7280;font-size:12px;">Sent from Property Hub.</p>
            </body></html>
            """;

        return (subject, body);
    }

    private static ReminderPriorityDto MapPriorityToDto(ReminderPriority p)
    {
        return new ReminderPriorityDto
        {
            ReminderPriorityId = p.ReminderPriorityId,
            ReminderPriorityName = p.ReminderPriorityName,
            Description = p.Description,
            DisplayColor = p.DisplayColor,
            SortOrder = p.SortOrder,
            IsActive = p.IsActive,
            CreatedDate = p.CreatedDate,
        };
    }
}
