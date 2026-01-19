using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblContactLog")]
public class ContactLog
{
    [Key]
    [Column("contactLogID")]
    public int ContactLogId { get; set; }

    [Column("propertyGrpID")]
    public int? PropertyGroupId { get; set; }

    [Column("propertyID")]
    public int? PropertyId { get; set; }

    [Column("tenantID")]
    public int? TenantId { get; set; }

    [Required]
    [Column("contactDate")]
    public DateTime ContactDate { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("contactBy")]
    public string ContactBy { get; set; } = string.Empty;

    [Required]
    [Column("contactNotes", TypeName = "nvarchar(max)")]
    public string Notes { get; set; } = string.Empty;

    [Required]
    [Column("contactLogTypeID")]
    public int ContactLogTypeId { get; set; }

    // Computed property for Subject (for backward compatibility)
    [NotMapped]
    public string? Subject => Notes;

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property? Property { get; set; }

    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup? PropertyGroup { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("ContactLogTypeId")]
    public virtual ContactLogType ContactLogType { get; set; } = null!;

    public virtual ICollection<ContactLogAttachment> Attachments { get; set; } = new List<ContactLogAttachment>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
