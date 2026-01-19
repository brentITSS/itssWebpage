using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalType")]
public class JournalType
{
    [Key]
    [Column("journalTypeID")]
    public int JournalTypeId { get; set; }

    [MaxLength(100)]
    [Column("journalType")]
    public string? JournalTypeName { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("active")]
    public bool? IsActive { get; set; }

    // Navigation properties
    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
    public virtual ICollection<JournalSubType> JournalSubTypes { get; set; } = new List<JournalSubType>();
}
