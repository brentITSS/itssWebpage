using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTagLog")]
public class TagLog
{
    [Key]
    public int TagLogId { get; set; }

    [Required]
    public int TagTypeId { get; set; }

    // Polymorphic relationship - can tag different entity types
    [MaxLength(50)]
    public string? EntityType { get; set; } // "Property", "PropertyGroup", "Tenant", "ContactLog"

    public int? EntityId { get; set; } // ID of the tagged entity

    public int? PropertyId { get; set; }

    public int? PropertyGroupId { get; set; }

    public int? TenantId { get; set; }

    public int? ContactLogId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedByUserId { get; set; }

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
