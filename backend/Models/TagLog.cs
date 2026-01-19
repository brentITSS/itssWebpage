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

    // Navigation properties
    [ForeignKey("TagTypeId")]
    public virtual TagType TagType { get; set; } = null!;

    [ForeignKey("PropertyId")]
    public virtual Property? Property { get; set; }

    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup? PropertyGroup { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("ContactLogId")]
    public virtual ContactLog? ContactLog { get; set; }
}
