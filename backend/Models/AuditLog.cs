using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblAuditLog")]
public class AuditLog
{
    [Key]
    [Column("auditLogID")]
    public int AuditLogId { get; set; }

    [Required]
    [Column("userID")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("action")]
    public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete"

    [Required]
    [MaxLength(100)]
    [Column("entityType")]
    public string EntityType { get; set; } = string.Empty; // "User", "Property", etc.

    [Column("entityID")]
    public int? EntityId { get; set; }

    [MaxLength(1000)]
    [Column("oldValues")]
    public string? OldValues { get; set; }

    [MaxLength(1000)]
    [Column("newValues")]
    public string? NewValues { get; set; }

    [Column("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    [Column("ipAddress")]
    public string? IpAddress { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
