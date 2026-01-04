using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblContactLog")]
public class ContactLog
{
    [Key]
    public int ContactLogId { get; set; }

    [Required]
    public int PropertyId { get; set; }

    public int? TenantId { get; set; }

    [Required]
    public int ContactLogTypeId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Notes { get; set; } = string.Empty;

    public DateTime ContactDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property Property { get; set; } = null!;

    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("ContactLogTypeId")]
    public virtual ContactLogType ContactLogType { get; set; } = null!;

    public virtual ICollection<ContactLogAttachment> Attachments { get; set; } = new List<ContactLogAttachment>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
