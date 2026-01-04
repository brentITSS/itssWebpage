using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblTenant")]
public class Tenant
{
    [Key]
    public int TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    public virtual ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
