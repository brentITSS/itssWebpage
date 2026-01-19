using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblContactLogAttachment")]
public class ContactLogAttachment
{
    [Key]
    [Column("contactLogAttachmentID")]
    public int ContactLogAttachmentId { get; set; }

    [Column("contactLogID")]
    public int ContactLogId { get; set; }

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
    [ForeignKey("ContactLogId")]
    public virtual ContactLog ContactLog { get; set; } = null!;
}
