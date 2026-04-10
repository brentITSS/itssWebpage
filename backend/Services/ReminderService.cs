using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class ReminderService : IReminderService
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IPropertyRepository _propertyRepository;

    public ReminderService(IReminderRepository reminderRepository, IPropertyRepository propertyRepository)
    {
        _reminderRepository = reminderRepository;
        _propertyRepository = propertyRepository;
    }

    private static bool CanAccessReminder(Reminder r, List<int> userGroupIds, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        if (isGlobalAdmin || isPropertyHubAdmin) return true;
        if (!r.PropertyGroupId.HasValue && !r.PropertyId.HasValue) return true;
        if (userGroupIds.Count == 0) return true;

        if (r.PropertyGroupId.HasValue && userGroupIds.Contains(r.PropertyGroupId.Value))
            return true;

        if (r.PropertyId.HasValue && r.Property?.PropertyGroupId is int pg && userGroupIds.Contains(pg))
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

    public async Task<ReminderResponseDto> CreateReminderAsync(CreateReminderRequest request)
    {
        var reminder = new Reminder
        {
            PropertyGroupId = request.PropertyGroupId,
            PropertyId = request.PropertyId,
            Title = request.Title,
            Notes = request.Notes,
            DueDate = request.DueDate,
            IsCompleted = request.IsCompleted,
            CreatedDate = DateTime.UtcNow,
        };

        await NormalizePropertyLinksAsync(reminder);
        reminder = await _reminderRepository.CreateAsync(reminder);
        var loaded = await _reminderRepository.GetByIdAsync(reminder.ReminderId);
        return MapToDto(loaded!);
    }

    public async Task<ReminderResponseDto?> UpdateReminderAsync(int reminderId, UpdateReminderRequest request)
    {
        var reminder = await _reminderRepository.GetByIdAsync(reminderId);
        if (reminder == null) return null;

        if (request.PropertyGroupId.HasValue) reminder.PropertyGroupId = request.PropertyGroupId;
        if (request.PropertyId.HasValue) reminder.PropertyId = request.PropertyId;
        if (request.Title != null) reminder.Title = request.Title;
        if (request.Notes != null) reminder.Notes = request.Notes;
        if (request.DueDate.HasValue) reminder.DueDate = request.DueDate.Value;
        if (request.IsCompleted.HasValue) reminder.IsCompleted = request.IsCompleted.Value;

        await NormalizePropertyLinksAsync(reminder);
        await _reminderRepository.UpdateAsync(reminder);
        var loaded = await _reminderRepository.GetByIdAsync(reminderId);
        return MapToDto(loaded!);
    }

    public async Task<bool> DeleteReminderAsync(int reminderId)
    {
        return await _reminderRepository.DeleteAsync(reminderId);
    }

    private async Task NormalizePropertyLinksAsync(Reminder reminder)
    {
        if (reminder.PropertyId.HasValue)
        {
            var prop = await _propertyRepository.GetPropertyByIdAsync(reminder.PropertyId.Value);
            if (prop?.PropertyGroupId != null)
                reminder.PropertyGroupId = prop.PropertyGroupId;
        }
    }

    private static ReminderResponseDto MapToDto(Reminder r)
    {
        return new ReminderResponseDto
        {
            ReminderId = r.ReminderId,
            PropertyGroupId = r.PropertyGroupId,
            PropertyGroupName = r.PropertyGroup?.PropertyGroupName,
            PropertyId = r.PropertyId,
            PropertyName = r.Property?.PropertyName,
            Title = r.Title,
            Notes = r.Notes,
            DueDate = r.DueDate,
            IsCompleted = r.IsCompleted,
            CreatedDate = r.CreatedDate,
        };
    }
}
