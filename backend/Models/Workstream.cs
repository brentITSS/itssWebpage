using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblWorkstream")]
public class Workstream
{
    [Key]
    [Column("workstreamID")]
    public int WorkstreamId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("workstreamWebID")]
    public string WorkstreamWebId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("workstream")]
    public string WorkstreamName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("workstreamGroup")]
    public string? WorkstreamGroup { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<WorkstreamUser> WorkstreamUsers { get; set; } = new List<WorkstreamUser>();
}
