using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblMaintenance")]
public class Maintenance
{
    [Key]
    [Column("maintenanceID")]
    public int MaintenanceId { get; set; }

    [Column("propertyGrpID")]
    public int PropertyGroupId { get; set; }

    [Column("propertyID")]
    public int PropertyId { get; set; }

    [Column("maintenanceTypeID")]
    public int MaintenanceTypeId { get; set; }

    [Column("maintenanceStatusID")]
    public int? MaintenanceStatusId { get; set; }

    [MaxLength(500)]
    [Column("summary")]
    public string? Summary { get; set; }

    [Column("detailNotes", TypeName = "nvarchar(max)")]
    public string? DetailNotes { get; set; }

    [Column("workDate")]
    public DateTime? WorkDate { get; set; }

    [Column("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup PropertyGroup { get; set; } = null!;

    [ForeignKey("PropertyId")]
    public virtual Property Property { get; set; } = null!;

    [ForeignKey("MaintenanceTypeId")]
    public virtual MaintenanceType MaintenanceType { get; set; } = null!;

    [ForeignKey("MaintenanceStatusId")]
    public virtual MaintenanceStatus? MaintenanceStatus { get; set; }
}
