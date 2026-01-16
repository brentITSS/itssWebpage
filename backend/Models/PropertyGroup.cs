using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblPropertyGroup")]
public class PropertyGroup
{
    [Key]
    [Column("propertyGroupID")]
    public int PropertyGroupId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("propertyGroupName")]
    public string PropertyGroupName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("modifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [Column("createdByID")]
    public int? CreatedByUserId { get; set; }

    [Column("modifiedByID")]
    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
