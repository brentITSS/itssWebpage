using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalLog")]
public class JournalLog
{
    [Key]
    public int JournalLogId { get; set; }

    [Required]
    public int PropertyId { get; set; }

    public int? TenancyId { get; set; }

    public int? TenantId { get; set; }

    [Required]
    public int JournalTypeId { get; set; }

    public int? JournalSubTypeId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime TransactionDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property Property { get; set; } = null!;

    [ForeignKey("TenancyId")]
    public virtual Tenancy? Tenancy { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("JournalTypeId")]
    public virtual JournalType JournalType { get; set; } = null!;

    [ForeignKey("JournalSubTypeId")]
    public virtual JournalSubType? JournalSubType { get; set; }

    public virtual ICollection<JournalLogAttachment> Attachments { get; set; } = new List<JournalLogAttachment>();
}
