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
    public int TagTypeId { get; set; }

    // Polymorphic relationship - can tag different entity types
    [MaxLength(50)]
    [Column("entityType")]
    public string? EntityType { get; set; } // "Property", "PropertyGroup", "Tenant", "ContactLog"

    [Column("entityID")]
    public int? EntityId { get; set; } // ID of the tagged entity

    [Column("propertyID")]
    public int? PropertyId { get; set; }

    [Column("propertyGroupID")]
    public int? PropertyGroupId { get; set; }

    [Column("tenantID")]
    public int? TenantId { get; set; }

    [Column("contactLogID")]
    public int? ContactLogId { get; set; }

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
