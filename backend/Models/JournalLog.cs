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

    [Column("journalAmountRand")]
    public decimal? JournalAmountRand { get; set; }

    [Column("journalDescription")]
    public string? JournalDescription { get; set; }

    [Column("trackingDataOnly")]
    public bool TrackingDataOnly { get; set; }

    // Backward-compatible aliases used by existing service and UI mappings.
    [NotMapped]
    public decimal? Amount
    {
        get => JournalAmountRand;
        set => JournalAmountRand = value;
    }

    [NotMapped]
    public string? Description
    {
        get => JournalDescription;
        set => JournalDescription = value;
    }

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

    public virtual ICollection<JournalLogAttachment> Attachments { get; set; } = new List<JournalLogAttachment>();
}
