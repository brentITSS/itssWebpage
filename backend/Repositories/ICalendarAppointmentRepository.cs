using backend.Models;

namespace backend.Repositories;

public interface ICalendarAppointmentRepository
{
    Task<Dictionary<int, CalendarAppointment>> GetBySourceIdsAsync(string sourceType, IEnumerable<int> sourceIds);
    Task<CalendarAppointment?> GetBySourceAsync(string sourceType, int sourceId);
    Task<CalendarAppointment> UpsertAsync(
        string sourceType,
        int sourceId,
        DateTime appointmentDate,
        bool isAllDay = true,
        string? titleOverride = null,
        string? notes = null);
    Task DeleteBySourceAsync(string sourceType, int sourceId);
}
