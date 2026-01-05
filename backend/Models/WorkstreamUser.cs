using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblWorkstreamUsers")]
public class WorkstreamUser
{
    [Key]
    public int WorkstreamUserId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int WorkstreamId { get; set; }

    [Required]
    public int PermissionTypeId { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("WorkstreamId")]
    public virtual Workstream Workstream { get; set; } = null!;

    [ForeignKey("PermissionTypeId")]
    public virtual PermissionType PermissionType { get; set; } = null!;
}
