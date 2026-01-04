using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblWorkstream")]
public class Workstream
{
    [Key]
    public int WorkstreamId { get; set; }

    [Required]
    [MaxLength(100)]
    public string WorkstreamName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation properties
    public virtual ICollection<WorkstreamUser> WorkstreamUsers { get; set; } = new List<WorkstreamUser>();
}
