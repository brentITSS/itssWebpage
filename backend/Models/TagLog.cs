using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTagLog")]
public class TagLog
{
    [Key]
    [Column("tagLogID")]
    public int TagLogId { get; set; }

    [Column("tagTypeID")]
    public int? TagTypeId { get; set; }

    [Column("tagActive")]
    public bool? IsActive { get; set; }

    [Column("contactLogID")]
    public int? ContactLogId { get; set; }

    [Column("journalLogID")]
    public int? JournalLogId { get; set; }

    [Column("tenantID")]
    public int? TenantId { get; set; }

    [Column("tenancyID")]
    public int? TenancyId { get; set; }

    [Column("propertyGrpID")]
    public int? PropertyGroupId { get; set; }

    // Computed properties for backward compatibility with polymorphic approach
    [NotMapped]
    public string? EntityType
    {
        get
        {
            if (ContactLogId.HasValue) return "ContactLog";
            if (JournalLogId.HasValue) return "JournalLog";
            if (TenantId.HasValue) return "Tenant";
            if (TenancyId.HasValue) return "Tenancy";
            if (PropertyGroupId.HasValue) return "PropertyGroup";
            return null;
        }
    }

    [NotMapped]
    public int? EntityId
    {
        get
        {
            if (ContactLogId.HasValue) return ContactLogId.Value;
            if (JournalLogId.HasValue) return JournalLogId.Value;
            if (TenantId.HasValue) return TenantId.Value;
            if (TenancyId.HasValue) return TenancyId.Value;
            if (PropertyGroupId.HasValue) return PropertyGroupId.Value;
            return null;
        }
    }

    // Navigation properties
    [ForeignKey("TagTypeId")]
    public virtual TagType? TagType { get; set; }

    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup? PropertyGroup { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("TenancyId")]
    public virtual Tenancy? Tenancy { get; set; }

    [ForeignKey("ContactLogId")]
    public virtual ContactLog? ContactLog { get; set; }
}
