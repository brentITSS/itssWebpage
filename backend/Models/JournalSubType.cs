using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalSubType")]
public class JournalSubType
{
    [Key]
    public int JournalSubTypeId { get; set; }

    [Required]
    public int JournalTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string JournalSubTypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("JournalTypeId")]
    public virtual JournalType JournalType { get; set; } = null!;

    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
}
