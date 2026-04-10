namespace backend.DTOs;

public class ReminderResponseDto
{
    public int ReminderId { get; set; }
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public int? TenancyId { get; set; }
    public string? TenancySummary { get; set; }
    public int? PropertyGroupId { get; set; }
    public string? PropertyGroupName { get; set; }
    public int? PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    /// <summary>Underlying tblReminder.reminderActive (null/true = open).</summary>
    public bool? ReminderActive { get; set; }
    /// <summary>True when reminderActive is explicitly false.</summary>
    public bool IsCompleted { get; set; }
}

public class CreateReminderRequest
{
    public int? TenantId { get; set; }
    public int? TenancyId { get; set; }
    public int? PropertyGroupId { get; set; }
    public int? PropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    /// <summary>When true, stored as reminderActive = false.</summary>
    public bool IsCompleted { get; set; }
}

public class UpdateReminderRequest
{
    public int? TenantId { get; set; }
    public int? TenancyId { get; set; }
    public int? PropertyGroupId { get; set; }
    public int? PropertyId { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public bool? IsCompleted { get; set; }
}
