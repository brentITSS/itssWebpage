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

    // Navigation properties
    [ForeignKey("ContactLogId")]
    public virtual ContactLog ContactLog { get; set; } = null!;
}
