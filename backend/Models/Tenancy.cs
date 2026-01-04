using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTenancy")]
public class Tenancy
{
    [Key]
    public int TenancyId { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required]
    public int TenantId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MonthlyRent { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("PropertyId")]
    public virtual Property Property { get; set; } = null!;

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
}
