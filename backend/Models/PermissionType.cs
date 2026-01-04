using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblPermissionType")]
public class PermissionType
{
    [Key]
    public int PermissionTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PermissionTypeName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<WorkstreamUser> WorkstreamUsers { get; set; } = new List<WorkstreamUser>();
}
