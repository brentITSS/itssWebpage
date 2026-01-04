using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalType")]
public class JournalType
{
    [Key]
    public int JournalTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string JournalTypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
}
