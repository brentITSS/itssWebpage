using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class CalendarAppointmentRepository : ICalendarAppointmentRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarAppointmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, CalendarAppointment>> GetBySourceIdsAsync(string sourceType, IEnumerable<int> sourceIds)
    {
        var normalizedSourceType = NormalizeSourceType(sourceType);
        var ids = sourceIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, CalendarAppointment>();
        }

        return await _context.CalendarAppointments
            .Where(x => x.SourceType == normalizedSourceType && ids.Contains(x.SourceId))
            .ToDictionaryAsync(x => x.SourceId, x => x);
    }

    public async Task<CalendarAppointment?> GetBySourceAsync(string sourceType, int sourceId)
    {
        var normalizedSourceType = NormalizeSourceType(sourceType);
        return await _context.CalendarAppointments
            .FirstOrDefaultAsync(x => x.SourceType == normalizedSourceType && x.SourceId == sourceId);
    }

    public async Task<CalendarAppointment> UpsertAsync(
        string sourceType,
        int sourceId,
        DateTime appointmentDate,
        bool isAllDay = true,
        string? titleOverride = null,
        string? notes = null)
    {
        var normalizedSourceType = NormalizeSourceType(sourceType);
        var existing = await GetBySourceAsync(normalizedSourceType, sourceId);
        if (existing == null)
        {
            existing = new CalendarAppointment
            {
                SourceType = normalizedSourceType,
                SourceId = sourceId,
                AppointmentDate = appointmentDate,
                IsAllDay = isAllDay,
                TitleOverride = titleOverride,
                Notes = notes,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.CalendarAppointments.Add(existing);
        }
        else
        {
            existing.AppointmentDate = appointmentDate;
            existing.IsAllDay = isAllDay;
            existing.TitleOverride = titleOverride;
            existing.Notes = notes;
            existing.IsActive = true;
            existing.ModifiedDate = DateTime.UtcNow;
            _context.CalendarAppointments.Update(existing);
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteBySourceAsync(string sourceType, int sourceId)
    {
        var normalizedSourceType = NormalizeSourceType(sourceType);
        var existing = await GetBySourceAsync(normalizedSourceType, sourceId);
        if (existing == null)
        {
            return;
        }

        _context.CalendarAppointments.Remove(existing);
        await _context.SaveChangesAsync();
    }

    private static string NormalizeSourceType(string sourceType)
    {
        return sourceType.Trim().ToLowerInvariant();
    }
}
