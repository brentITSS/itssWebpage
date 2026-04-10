using backend.Models;

namespace backend.Repositories;

public interface IReminderRepository
{
    Task<List<Reminder>> GetAllAsync();
    Task<Reminder?> GetByIdAsync(int reminderId);
    Task<Reminder> CreateAsync(Reminder reminder);
    Task<Reminder> UpdateAsync(Reminder reminder);
    Task<bool> DeleteAsync(int reminderId);
}
