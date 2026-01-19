using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTenancy")]
public class Tenancy
{
    [Key]
    [Column("tenancyID")]
    public int TenancyId { get; set; }

    [Column("propertyID")]
    public int PropertyId { get; set; }

    [Column("tenantID")]
    public int TenantId { get; set; }

    [Column("startDate")]
    public DateTime? StartDate { get; set; }

    [Column("endDate")]
    public DateTime? EndDate { get; set; }

    [Column("monthlyRent", TypeName = "decimal(18,2)")]
    public decimal? MonthlyRent { get; set; }

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property Property { get; set; } = null!;

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
}
