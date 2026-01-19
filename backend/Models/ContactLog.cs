using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblContactLog")]
public class ContactLog
{
    [Key]
    [Column("contactLogID")]
    public int ContactLogId { get; set; }

    [Column("propertyID")]
    public int PropertyId { get; set; }

    [Column("tenantID")]
    public int? TenantId { get; set; }

    [Column("contactLogTypeID")]
    public int ContactLogTypeId { get; set; }

    [MaxLength(500)]
    [Column("subject")]
    public string? Subject { get; set; }

    [Column("notes", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column("contactDate")]
    public DateTime? ContactDate { get; set; }

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
