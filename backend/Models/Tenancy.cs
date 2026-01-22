using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTenancy")]
public class Tenancy
{
    [Key]
    [Column("tenancyID")]
    public int TenancyId { get; set; }

    [Required]
    [Column("propertyID")]
    public int PropertyId { get; set; }

    [Required]
    [Column("tenancyStartDate")]
    public DateTime StartDate { get; set; }

    [Column("tenancyEndDate")]
    public DateTime? EndDate { get; set; }

    [Column("tenancyActive")]
    public bool? IsActive { get; set; }

    [Column("description", TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Column("monthlyRentalCharge", TypeName = "money")]
    public decimal? MonthlyRent { get; set; }

    [Column("specialConditions", TypeName = "nvarchar(max)")]
    public string? SpecialConditions { get; set; }

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property Property { get; set; } = null!;

    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
}
