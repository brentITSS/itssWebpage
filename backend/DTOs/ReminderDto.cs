namespace backend.DTOs;

public class ReminderPriorityDto
{
    public int ReminderPriorityId { get; set; }
    public string ReminderPriorityName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DisplayColor { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class CreateReminderPriorityRequest
{
    public string ReminderPriorityName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DisplayColor { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateReminderPriorityRequest
{
    public string? ReminderPriorityName { get; set; }
    public string? Description { get; set; }
    public string? DisplayColor { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

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
    public int? ReminderPriorityId { get; set; }
    public string? ReminderPriorityName { get; set; }
    public string? ReminderPriorityColor { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ReminderDate { get; set; }
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
    public int? ReminderPriorityId { get; set; }
    public string? Notes { get; set; }
    public DateTime? ReminderDate { get; set; }
    /// <summary>When true, stored as reminderActive = false.</summary>
    public bool IsCompleted { get; set; }
}

public class UpdateReminderRequest
{
    public int? TenantId { get; set; }
    public int? TenancyId { get; set; }
    public int? PropertyGroupId { get; set; }
    public int? PropertyId { get; set; }
    public int? ReminderPriorityId { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public DateTime? ReminderDate { get; set; }
    public bool? IsCompleted { get; set; }
}
