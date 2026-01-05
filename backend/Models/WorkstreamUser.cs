using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblWorkstreamUsers")]
public class WorkstreamUser
{
    [Key]
    [Column("workstreamUserID")]
    public int WorkstreamUserId { get; set; }

    [Required]
    [Column("userID")]
    public int UserId { get; set; }

    [Required]
    [Column("workstreamID")]
    public int WorkstreamId { get; set; }

    [Column("permissionTypeID")]
    public int PermissionTypeId { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("WorkstreamId")]
    public virtual Workstream Workstream { get; set; } = null!;

    [ForeignKey("PermissionTypeId")]
    public virtual PermissionType PermissionType { get; set; } = null!;
}
