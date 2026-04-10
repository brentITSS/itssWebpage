using backend.Models;

namespace backend.Repositories;

public interface IReminderRepository
{
    Task<List<Reminder>> GetAllAsync();
    Task<Reminder?> GetByIdAsync(int reminderId);
    Task<Reminder> CreateAsync(Reminder reminder);
    Task<Reminder> UpdateAsync(Reminder reminder);
    Task<bool> DeleteAsync(int reminderId);

    Task<List<ReminderPriority>> GetAllReminderPrioritiesAsync();
    Task<ReminderPriority?> GetReminderPriorityByIdAsync(int id);
    Task<ReminderPriority> CreateReminderPriorityAsync(ReminderPriority priority);
    Task<ReminderPriority> UpdateReminderPriorityAsync(ReminderPriority priority);
    Task<bool> DeleteReminderPriorityAsync(int id);
    Task<int> CountRemindersByPriorityAsync(int reminderPriorityId);
}
