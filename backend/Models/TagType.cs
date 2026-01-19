using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTagType")]
public class TagType
{
    [Key]
    [Column("tagID")]
    public int TagTypeId { get; set; }

    [MaxLength(100)]
    [Column("tagName")]
    public string? TagTypeName { get; set; }

    [MaxLength(500)]
    [Column("tagDescription")]
    public string? Description { get; set; }

    [Column("tagActive")]
    public bool? IsActive { get; set; }

    // Navigation properties
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
