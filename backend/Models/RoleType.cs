using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblRoleType")]
public class RoleType
{
    [Key]
    [Column("roleTypeID")]
    public int RoleTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("roleType")]
    public string RoleTypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
