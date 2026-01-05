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
    [Column("roleTypeName")]
    public string RoleTypeName { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
