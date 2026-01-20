using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalLog")]
public class JournalLog
{
    [Key]
    [Column("journalLogID")]
    public int JournalLogId { get; set; }

    [Column("propertyGroupID")]
    public int? PropertyGroupId { get; set; }

    [Column("propertyID")]
    public int? PropertyId { get; set; }

    [Column("tenancyID")]
    public int? TenancyId { get; set; }

    [Column("tenantID")]
    public int? TenantId { get; set; }

    [Column("transactionDate")]
    public DateTime? TransactionDate { get; set; }

    [Column("journalTypeID")]
    public int? JournalTypeId { get; set; }

    [Column("journalSubTypeID")]
    public int? JournalSubTypeId { get; set; }

    // Computed properties for fields that don't exist in database (for backward compatibility)
    [NotMapped]
    public decimal? Amount { get; set; }

    [NotMapped]
    public string? Description { get; set; }

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property? Property { get; set; }

    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup? PropertyGroup { get; set; }

    [ForeignKey("TenancyId")]
    public virtual Tenancy? Tenancy { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("JournalTypeId")]
    public virtual JournalType? JournalType { get; set; }

    [ForeignKey("JournalSubTypeId")]
    public virtual JournalSubType? JournalSubType { get; set; }

    // Temporarily removed navigation property until table schema is verified
    // public virtual ICollection<JournalLogAttachment> Attachments { get; set; } = new List<JournalLogAttachment>();
}
