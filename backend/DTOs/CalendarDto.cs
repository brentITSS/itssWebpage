namespace backend.DTOs;

public class CalendarEventDto
{
    public string EventType { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public bool IsAllDay { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public string? Color { get; set; }

    public int? PropertyGroupId { get; set; }
    public string? PropertyGroupName { get; set; }
    public int? PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public int? TenancyId { get; set; }
    public string? TenancySummary { get; set; }
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
}
