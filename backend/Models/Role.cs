using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblRole")]
public class Role
{
    [Key]
    [Column("roleID")]
    public int RoleId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("roleName")]
    public string RoleName { get; set; } = string.Empty;

    [Column("roleTypeID")]
    public int RoleTypeId { get; set; }

    [Column("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("modifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [ForeignKey(nameof(RoleTypeId))]
    public virtual RoleType RoleType { get; set; } = null!;
}
