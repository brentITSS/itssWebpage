using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalLogAttachment")]
public class JournalLogAttachment
{
    [Key]
    [Column("journalLogAttachmentID")]
    public int JournalLogAttachmentId { get; set; }

    [Column("journalLogID")]
    public int JournalLogId { get; set; }

    [MaxLength(255)]
    [Column("fileName")]
    public string? FileName { get; set; }

    [MaxLength(500)]
    [Column("filePath")]
    public string? FilePath { get; set; }

    [MaxLength(50)]
    [Column("fileType")]
    public string? FileType { get; set; }

    [Column("fileSize")]
    public long? FileSize { get; set; }

    // Navigation properties
    [ForeignKey("JournalLogId")]
    public virtual JournalLog JournalLog { get; set; } = null!;
}
