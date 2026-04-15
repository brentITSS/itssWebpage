using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

/// <summary>
/// Maps to dbo.tblReminder (production columns: reminder, reminderDetail, reminderActive, tenantID, tenancyID, etc.).
/// </summary>
[Table("tblReminder")]
public class Reminder
{
    [Key]
    [Column("reminderID")]
    public int ReminderId { get; set; }

    [Column("tenantID")]
    public int? TenantId { get; set; }

    [Column("tenancyID")]
    public int? TenancyId { get; set; }

    [Column("propertyGrpID")]
    public int? PropertyGroupId { get; set; }

    [Column("propertyID")]
    public int? PropertyId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("reminder")]
    public string Title { get; set; } = string.Empty;

    [Column("reminderDetail", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [MaxLength(255)]
    [Column("createdBy")]
    public string? CreatedBy { get; set; }

    [Column("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [Column("reminderDate")]
    public DateTime? ReminderDate { get; set; }

    /// <summary>When true or null, the reminder is still active; false means completed/dismissed.</summary>
    [Column("reminderActive")]
    public bool? ReminderActive { get; set; }

    [Column("reminderPriorityID")]
    public int? ReminderPriorityId { get; set; }

    [Timestamp]
    [Column("SSMA_TimeStamp")]
    public byte[]? RowVersion { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("TenancyId")]
    public virtual Tenancy? Tenancy { get; set; }

    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup? PropertyGroup { get; set; }

    [ForeignKey("PropertyId")]
    public virtual Property? Property { get; set; }

    [ForeignKey("ReminderPriorityId")]
    public virtual ReminderPriority? ReminderPriority { get; set; }
}
