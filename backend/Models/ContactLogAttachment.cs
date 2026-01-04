using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblContactLogAttachment")]
public class ContactLogAttachment
{
    [Key]
    public int ContactLogAttachmentId { get; set; }

    [Required]
    public int ContactLogId { get; set; }

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
    [ForeignKey("ContactLogId")]
    public virtual ContactLog ContactLog { get; set; } = null!;
}
