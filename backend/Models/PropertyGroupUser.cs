using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

/// <summary>
/// Links users to specific property groups they have access to.
/// Similar to WorkstreamUser, but for property group-level access control.
/// </summary>
[Table("tblPropertyGroupUsers")]
public class PropertyGroupUser
{
    [Key]
    [Column("propertyGroupUserID")]
    public int PropertyGroupUserId { get; set; }

    [Required]
    [Column("userID")]
    public int UserId { get; set; }

    [Required]
    [Column("propertyGrpID")]
    public int PropertyGroupId { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup PropertyGroup { get; set; } = null!;
}
