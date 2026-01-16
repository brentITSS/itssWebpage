using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblProperty")]
public class Property
{
    [Key]
    [Column("propertyID")]
    public int PropertyId { get; set; }

    [Required]
    [Column("propertyGroupID")]
    public int PropertyGroupId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("propertyName")]
    public string PropertyName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(50)]
    [Column("postCode")]
    public string? PostCode { get; set; }

    [Column("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("modifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [Column("createdByID")]
    public int? CreatedByUserId { get; set; }

    [Column("modifiedByID")]
    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup PropertyGroup { get; set; } = null!;

    public virtual ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
