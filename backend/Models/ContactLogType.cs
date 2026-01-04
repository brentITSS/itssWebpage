using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblContactLogType")]
public class ContactLogType
{
    [Key]
    public int ContactLogTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ContactLogTypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
}
