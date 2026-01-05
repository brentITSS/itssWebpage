using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblUserRole")]
public class UserRole
{
    [Key]
    [Column("roleID")]
    public int UserRoleId { get; set; }

    [Required]
    [Column("userID")]
    public int UserId { get; set; }

    [Required]
    [Column("roleTypeID")]
    public int RoleTypeId { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleTypeId")]
    public virtual RoleType RoleType { get; set; } = null!;
}
