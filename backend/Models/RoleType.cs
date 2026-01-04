using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblRoleType")]
public class RoleType
{
    [Key]
    public int RoleTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoleTypeName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
