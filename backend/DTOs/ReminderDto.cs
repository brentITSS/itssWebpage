namespace backend.DTOs;

public class ReminderResponseDto
{
    public int ReminderId { get; set; }
    public int? PropertyGroupId { get; set; }
    public string? PropertyGroupName { get; set; }
    public int? PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class CreateReminderRequest
{
    public int? PropertyGroupId { get; set; }
    public int? PropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
}

public class UpdateReminderRequest
{
    public int? PropertyGroupId { get; set; }
    public int? PropertyId { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public DateTime? DueDate { get; set; }
    public bool? IsCompleted { get; set; }
}
