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
            .Include(r => r.Tenancy)
                .ThenInclude(t => t!.Property)
            .Include(r => r.Tenant)
                .ThenInclude(t => t!.Tenancy!)
                    .ThenInclude(tn => tn.Property)
            .OrderByDescending(r => r.CreatedDate ?? DateTime.MinValue)
            .ToListAsync();
    }

    public async Task<Reminder?> GetByIdAsync(int reminderId)
    {
        return await _context.Reminders
            .Include(r => r.Property)
            .Include(r => r.PropertyGroup)
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
}
