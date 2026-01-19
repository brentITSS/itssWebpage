using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblContactLogType")]
public class ContactLogType
{
    [Key]
    [Column("contactLogTypeID")]
    public int ContactLogTypeId { get; set; }

    [MaxLength(100)]
    [Column("contactLogTypeName")]
    public string? ContactLogTypeName { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
}
