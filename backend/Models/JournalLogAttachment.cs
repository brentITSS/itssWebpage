using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalLogAttachment")]
public class JournalLogAttachment
{
    [Key]
    [Column("journalLogAttachmentId")]
    public int JournalLogAttachmentId { get; set; }

    [Column("journalLogId")]
    public int? JournalLogId { get; set; }

    // Note: Database schema not provided - using computed properties for file fields
    // If these columns exist in DB, they should be mapped here
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
