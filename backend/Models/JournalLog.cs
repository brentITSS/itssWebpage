using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalLog")]
public class JournalLog
{
    [Key]
    [Column("journalLogID")]
    public int JournalLogId { get; set; }

    [Column("propertyID")]
    public int PropertyId { get; set; }

    [Column("tenancyID")]
    public int? TenancyId { get; set; }

    [Column("tenantID")]
    public int? TenantId { get; set; }

    [Column("journalTypeID")]
    public int JournalTypeId { get; set; }

    [Column("journalSubTypeID")]
    public int? JournalSubTypeId { get; set; }

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    [MaxLength(1000)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("transactionDate")]
    public DateTime? TransactionDate { get; set; }

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
