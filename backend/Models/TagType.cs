using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTagType")]
public class TagType
{
    [Key]
    [Column("tagTypeID")]
    public int TagTypeId { get; set; }

    [MaxLength(100)]
    [Column("tagTypeName")]
    public string? TagTypeName { get; set; }

    [MaxLength(50)]
    [Column("color")]
    public string? Color { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
