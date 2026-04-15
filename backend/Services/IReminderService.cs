using backend.DTOs;

namespace backend.Services;

public interface IReminderService
{
    Task<List<ReminderResponseDto>> GetAllRemindersForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin);
    Task<List<ReminderResponseDto>> GetOverdueRemindersForUserAsync(
        int userId,
        bool isGlobalAdmin,
        bool isPropertyHubAdmin,
        int? propertyGroupId,
        int? propertyId,
        int? tenancyId,
        int? tenantId);
    Task<ReminderResponseDto?> GetReminderByIdForUserAsync(int reminderId, int userId, bool isGlobalAdmin, bool isPropertyHubAdmin);
    Task<ReminderResponseDto> CreateReminderAsync(CreateReminderRequest request, string? createdBy);
    Task<ReminderResponseDto?> UpdateReminderAsync(int reminderId, UpdateReminderRequest request);
    Task<bool> DeleteReminderAsync(int reminderId);

    Task<List<ReminderPriorityDto>> GetAllReminderPrioritiesAsync();
    Task<ReminderPriorityDto?> GetReminderPriorityByIdAsync(int id);
    Task<ReminderPriorityDto> CreateReminderPriorityAsync(CreateReminderPriorityRequest request);
    Task<ReminderPriorityDto?> UpdateReminderPriorityAsync(int id, UpdateReminderPriorityRequest request);
    Task<bool> IsReminderPriorityInUseAsync(int id);
    Task<bool> DeleteReminderPriorityAsync(int id);
}
