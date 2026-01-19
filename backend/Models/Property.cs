using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("tblProperty")]
public class Property
{
    [Key]
    [Column("PropertyID")]
    public int PropertyId { get; set; }

    [Required]
    [Column("PropertyGroupID")]
    public int PropertyGroupId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("PropertyName")]
    public string PropertyName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("Address")]
    public string? Address { get; set; }

    [MaxLength(50)]
    [Column("PostCode")]
    public string? PostCode { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Column("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [Column("CreatedByID")]
    public int? CreatedByUserId { get; set; }

    [Column("ModifiedByID")]
    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup PropertyGroup { get; set; } = null!;

    public virtual ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    public virtual ICollection<TagLog> TagLogs { get; set; } = new List<TagLog>();
}
