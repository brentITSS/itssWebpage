using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTenant")]
public class Tenant
{
    [Key]
    [Column("tenantId")]
    public int TenantId { get; set; }

    [MaxLength(100)]
    [Column("firstName")]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    [Column("lastName")]
    public string? LastName { get; set; }

    [MaxLength(255)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    // Navigation properties
    public virtual ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
