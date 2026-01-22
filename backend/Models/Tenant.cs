using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTenant")]
public class Tenant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("tenantID")]
    public int TenantId { get; set; }

    [Column("tenancyID")]
    public int? TenancyId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("secondName")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Column("tenantDOB")]
    public DateTime TenantDOB { get; set; }

    [MaxLength(255)]
    [Column("tenantEmail")]
    public string? Email { get; set; }

    [MaxLength(255)]
    [Column("identification")]
    public string? Identification { get; set; }

    [MaxLength(50)]
    [Column("mobile")]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [Column("currentEmployer")]
    public string? CurrentEmployer { get; set; }

    [Column("currentDeclaredGross", TypeName = "money")]
    public decimal? CurrentDeclaredGross { get; set; }

    [Column("expenditurePerMonth", TypeName = "money")]
    public decimal? ExpenditurePerMonth { get; set; }

    [Column("liveIn")]
    public bool? LiveIn { get; set; }

    [Column("rentalCommitment", TypeName = "money")]
    public decimal? RentalCommitment { get; set; }

    [Column("tenantActive")]
    public bool? TenantActive { get; set; }

    // Navigation properties
    [ForeignKey("TenancyId")]
    public virtual Tenancy? Tenancy { get; set; }
    
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
