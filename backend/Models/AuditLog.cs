using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblAuditLog")]
public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete"

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // "User", "Property", etc.

    public int? EntityId { get; set; }

    [MaxLength(1000)]
    public string? OldValues { get; set; }

    [MaxLength(1000)]
    public string? NewValues { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
