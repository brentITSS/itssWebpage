using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class ReminderService : IReminderService
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ITenantRepository _tenantRepository;

    public ReminderService(
        IReminderRepository reminderRepository,
        IPropertyRepository propertyRepository,
        ITenantRepository tenantRepository)
    {
        _reminderRepository = reminderRepository;
        _propertyRepository = propertyRepository;
        _tenantRepository = tenantRepository;
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
            Notes = request.Notes,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            ReminderActive = request.IsCompleted ? false : true,
        };

        await NormalizeLinksAsync(reminder);
        EnsureHasScopeLink(reminder);
        reminder = await _reminderRepository.CreateAsync(reminder);
        var loaded = await _reminderRepository.GetByIdAsync(reminder.ReminderId);
        return MapToDto(loaded!);
    }

    public async Task<ReminderResponseDto?> UpdateReminderAsync(int reminderId, UpdateReminderRequest request)
    {
        var reminder = await _reminderRepository.GetByIdAsync(reminderId);
        if (reminder == null) return null;

        reminder.TenantId = request.TenantId;
        reminder.TenancyId = request.TenancyId;
        reminder.PropertyGroupId = request.PropertyGroupId;
        reminder.PropertyId = request.PropertyId;
        if (request.Title != null) reminder.Title = request.Title;
        if (request.Notes != null) reminder.Notes = request.Notes;
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

        var active = r.ReminderActive ?? true;
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
            Notes = r.Notes,
            CreatedBy = r.CreatedBy,
            CreatedDate = r.CreatedDate,
            ReminderActive = r.ReminderActive,
            IsCompleted = r.ReminderActive == false,
        };
    }
}
