using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblPropertyGroup")]
public class PropertyGroup
{
    [Key]
    public int PropertyGroupId { get; set; }

    [Required]
    [MaxLength(200)]
    public string PropertyGroupName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
