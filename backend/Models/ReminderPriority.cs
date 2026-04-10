using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblReminderPriority")]
public class ReminderPriority
{
    [Key]
    [Column("reminderPriorityID")]
    public int ReminderPriorityId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("reminderPriorityName")]
    public string ReminderPriorityName { get; set; } = string.Empty;

    [Column("description", TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [MaxLength(32)]
    [Column("displayColor")]
    public string? DisplayColor { get; set; }

    [Column("sortOrder")]
    public int? SortOrder { get; set; }

    [Column("isActive")]
    public bool? IsActive { get; set; }

    [Column("createdDate")]
    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
