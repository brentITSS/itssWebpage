using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblCalendarAppointment")]
public class CalendarAppointment
{
    [Key]
    [Column("calendarAppointmentID")]
    public int CalendarAppointmentId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    [Column("sourceID")]
    public int SourceId { get; set; }

    [Column("appointmentDate")]
    public DateTime AppointmentDate { get; set; }

    [Column("isAllDay")]
    public bool IsAllDay { get; set; } = true;

    [MaxLength(255)]
    [Column("titleOverride")]
    public string? TitleOverride { get; set; }

    [Column("notes", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column("active")]
    public bool IsActive { get; set; } = true;

    [Column("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("modifiedDate")]
    public DateTime? ModifiedDate { get; set; }
}
