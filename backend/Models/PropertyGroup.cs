using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblPropertyGroup")]
public class PropertyGroup
{
    [Key]
    [Column("PropertyGroupID")]
    public int PropertyGroupId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("PropertyGroupName")]
    public string PropertyGroupName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("Description")]
    public string? Description { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [Column("CreatedByID")]
    public int? CreatedByUserId { get; set; }

    [Column("ModifiedByID")]
    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
