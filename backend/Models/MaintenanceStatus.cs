using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblMaintenanceStatus")]
public class MaintenanceStatus
{
    [Key]
    [Column("maintenanceStatusID")]
    public int MaintenanceStatusId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("maintenanceStatusName")]
    public string MaintenanceStatusName { get; set; } = string.Empty;

    [Column("description", TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Column("sortOrder")]
    public int? SortOrder { get; set; }

    [Column("isActive")]
    public bool? IsActive { get; set; }

    [Column("createdDate")]
    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Maintenance> Maintenances { get; set; } = new List<Maintenance>();
}
