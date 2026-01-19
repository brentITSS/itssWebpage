using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalSubType")]
public class JournalSubType
{
    [Key]
    [Column("journalSubTypeID")]
    public int JournalSubTypeId { get; set; }

    [Required]
    [Column("journalTypeID")]
    public int JournalTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("subType")]
    public string JournalSubTypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("active")]
    public bool? IsActive { get; set; }

    // Navigation properties
    [ForeignKey("JournalTypeId")]
    public virtual JournalType JournalType { get; set; } = null!;

    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
}
