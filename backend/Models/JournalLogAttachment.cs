using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalLogAttachment")]
public class JournalLogAttachment
{
    [Key]
    [Column("journalAttachmentID")]
    public int JournalLogAttachmentId { get; set; }

    [Column("journalLogID")]
    public int? JournalLogId { get; set; }

    [Column("dateAttached")]
    public DateTime? DateAttached { get; set; }

    [MaxLength(255)]
    [Column("attachedBy")]
    public string? AttachedBy { get; set; }

    // File-related properties don't exist in database - using NotMapped for backward compatibility
    [NotMapped]
    public string? FileName { get; set; }

    [NotMapped]
    public string? FilePath { get; set; }

    [NotMapped]
    public string? FileType { get; set; }

    [NotMapped]
    public long? FileSize { get; set; }

    // Navigation properties
    [ForeignKey("JournalLogId")]
    public virtual JournalLog? JournalLog { get; set; }
}
