using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblPasswordResetToken")]
public class PasswordResetToken
{
    [Key]
    [Column("passwordResetTokenID")]
    public int PasswordResetTokenId { get; set; }

    [Column("userID")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(64)]
    [Column("tokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [Column("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [Column("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    public virtual User? User { get; set; }
}
