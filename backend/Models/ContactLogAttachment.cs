using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblContactLogAttachment")]
public class ContactLogAttachment
{
    [Key]
    [Column("contactLogAttachmentID")]
    public int ContactLogAttachmentId { get; set; }

    [MaxLength(255)]
    [Column("contactID")]
    public string? ContactId { get; set; }

    [Column("contactLogID")]
    public int? ContactLogId { get; set; }

    [MaxLength(500)]
    [Column("attachmentDescription")]
    public string? Description { get; set; }

    // Computed properties for backward compatibility (database doesn't have these fields)
    [NotMapped]
    public string? FileName => Description; // Store filename in Description if needed

    [NotMapped]
    public string? FilePath { get; set; }

    [NotMapped]
    public string? FileType { get; set; }

    [NotMapped]
    public long? FileSize { get; set; }

    // Navigation properties
    [ForeignKey("ContactLogId")]
    public virtual ContactLog ContactLog { get; set; } = null!;
}
