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

    // Navigation properties
    [ForeignKey("TenancyId")]
    public virtual Tenancy? Tenancy { get; set; }
    
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
