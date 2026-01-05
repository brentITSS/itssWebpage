using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblPermissionType")]
public class PermissionType
{
    [Key]
    [Column("permissionTypeID")]
    public int PermissionTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("permission")]
    public string PermissionTypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    // Navigation properties
    public virtual ICollection<WorkstreamUser> WorkstreamUsers { get; set; } = new List<WorkstreamUser>();
}
