using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblPropertyGroup")]
public class PropertyGroup
{
    [Key]
    [Column("propertyGrpID")]
    public int PropertyGroupId { get; set; }

    [MaxLength(200)]
    [Column("groupName")]
    public string? PropertyGroupName { get; set; }

    [Column("active")]
    public bool? IsActive { get; set; }

    // Navigation properties
    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
