using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class ReminderRepository : IReminderRepository
{
    private readonly ApplicationDbContext _context;

    public ReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Reminder>> GetAllAsync()
    {
        return await _context.Reminders
            .Include(r => r.Property)
            .Include(r => r.PropertyGroup)
            .Include(r => r.ReminderPriority)
            .Include(r => r.Tenancy)
                .ThenInclude(t => t!.Property)
            .Include(r => r.Tenant)
                .ThenInclude(t => t!.Tenancy!)
                    .ThenInclude(tn => tn.Property)
            .OrderBy(r => r.ReminderDate == null ? 1 : 0)
            .ThenBy(r => r.ReminderDate ?? DateTime.MaxValue)
            .ThenByDescending(r => r.CreatedDate ?? DateTime.MinValue)
            .ToListAsync();
    }

    public async Task<Reminder?> GetByIdAsync(int reminderId)
    {
        return await _context.Reminders
            .Include(r => r.Property)
            .Include(r => r.PropertyGroup)
            .Include(r => r.ReminderPriority)
            .Include(r => r.Tenancy)
                .ThenInclude(t => t!.Property)
            .Include(r => r.Tenant)
                .ThenInclude(t => t!.Tenancy!)
                    .ThenInclude(tn => tn.Property)
            .FirstOrDefaultAsync(r => r.ReminderId == reminderId);
    }

    public async Task<Reminder> CreateAsync(Reminder reminder)
    {
        if (reminder.CreatedDate == null)
            reminder.CreatedDate = DateTime.UtcNow;
        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<Reminder> UpdateAsync(Reminder reminder)
    {
        _context.Reminders.Update(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<bool> DeleteAsync(int reminderId)
    {
        var entity = await _context.Reminders.FindAsync(reminderId);
        if (entity == null) return false;
        _context.Reminders.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ReminderPriority>> GetAllReminderPrioritiesAsync()
    {
        return await _context.ReminderPriorities
            .OrderBy(p => p.SortOrder ?? int.MaxValue)
            .ThenBy(p => p.ReminderPriorityName)
            .ToListAsync();
    }

    public async Task<ReminderPriority?> GetReminderPriorityByIdAsync(int id)
    {
        return await _context.ReminderPriorities.FindAsync(id);
    }

    public async Task<ReminderPriority> CreateReminderPriorityAsync(ReminderPriority priority)
    {
        if (priority.CreatedDate == null)
            priority.CreatedDate = DateTime.UtcNow;
        _context.ReminderPriorities.Add(priority);
        await _context.SaveChangesAsync();
        return priority;
    }

    public async Task<ReminderPriority> UpdateReminderPriorityAsync(ReminderPriority priority)
    {
        _context.ReminderPriorities.Update(priority);
        await _context.SaveChangesAsync();
        return priority;
    }

    public async Task<bool> DeleteReminderPriorityAsync(int id)
    {
        var entity = await _context.ReminderPriorities.FindAsync(id);
        if (entity == null) return false;
        _context.ReminderPriorities.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountRemindersByPriorityAsync(int reminderPriorityId)
    {
        return await _context.Reminders.CountAsync(r => r.ReminderPriorityId == reminderPriorityId);
    }
}
