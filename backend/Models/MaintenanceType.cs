using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblMaintenanceType")]
public class MaintenanceType
{
    [Key]
    [Column("maintenanceTypeID")]
    public int MaintenanceTypeId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("maintenanceTypeName")]
    public string MaintenanceTypeName { get; set; } = string.Empty;

    [Column("description", TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Column("isActive")]
    public bool? IsActive { get; set; }

    [Column("createdDate")]
    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Maintenance> Maintenances { get; set; } = new List<Maintenance>();
}
