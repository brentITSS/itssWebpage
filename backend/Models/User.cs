using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblUser")]
public class User
{
    [Key]
    [Column("userID")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("emailAddress")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password")]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("firstName")]
    public string? FirstName { get; set; }

    [MaxLength(255)]
    [Column("lastName")]
    public string? LastName { get; set; }

    [Column("active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<WorkstreamUser> WorkstreamUsers { get; set; } = new List<WorkstreamUser>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
