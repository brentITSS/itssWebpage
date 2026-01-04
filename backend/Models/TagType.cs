using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTagType")]
public class TagType
{
    [Key]
    public int TagTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string TagTypeName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
