using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblReminder")]
public class Reminder
{
    [Key]
    [Column("reminderID")]
    public int ReminderId { get; set; }

    [Column("propertyGrpID")]
    public int? PropertyGroupId { get; set; }

    [Column("propertyID")]
    public int? PropertyId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("reminderTitle")]
    public string Title { get; set; } = string.Empty;

    [Column("reminderNotes", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column("dueDate")]
    public DateTime DueDate { get; set; }

    [Column("isCompleted")]
    public bool IsCompleted { get; set; }

    [Column("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup? PropertyGroup { get; set; }

    [ForeignKey("PropertyId")]
    public virtual Property? Property { get; set; }
}
