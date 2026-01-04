using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblJournalLogAttachment")]
public class JournalLogAttachment
{
    [Key]
    public int JournalLogAttachmentId { get; set; }

    [Required]
    public int JournalLogId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FileType { get; set; }

    public long FileSize { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("JournalLogId")]
    public virtual JournalLog JournalLog { get; set; } = null!;
}
