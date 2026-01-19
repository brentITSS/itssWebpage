using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace backend.Models;

[Table("tblProperty")]
public class Property
{
    [Key]
    [Column("propertyID")]
    public int PropertyId { get; set; }

    [MaxLength(200)]
    [Column("propertyName")]
    public string? PropertyName { get; set; }

    [Column("propertyGrpID")]
    public int? PropertyGroupId { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("active")]
    public bool? IsActive { get; set; }

    [MaxLength(500)]
    [Column("address1")]
    public string? Address1 { get; set; }

    [MaxLength(500)]
    [Column("address2")]
    public string? Address2 { get; set; }

    [MaxLength(100)]
    [Column("addressCityTown")]
    public string? AddressCityTown { get; set; }

    [MaxLength(100)]
    [Column("addressCountry")]
    public string? AddressCountry { get; set; }

    [MaxLength(50)]
    [Column("addressPostCode")]
    public string? PostCode { get; set; }

    [Column("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [MaxLength(255)]
    [Column("createdBy")]
    public string? CreatedBy { get; set; }

    // Computed property for full address
    [NotMapped]
    public string? Address
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Address1)) parts.Add(Address1);
            if (!string.IsNullOrWhiteSpace(Address2)) parts.Add(Address2);
            if (!string.IsNullOrWhiteSpace(AddressCityTown)) parts.Add(AddressCityTown);
            if (!string.IsNullOrWhiteSpace(AddressCountry)) parts.Add(AddressCountry);
            if (!string.IsNullOrWhiteSpace(PostCode)) parts.Add(PostCode);
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }
    }

    // Navigation properties
    [ForeignKey("PropertyGroupId")]
    public virtual PropertyGroup? PropertyGroup { get; set; }

    public virtual ICollection<Tenancy> Tenancies { get; set; } = new List<Tenancy>();
    public virtual ICollection<JournalLog> JournalLogs { get; set; } = new List<JournalLog>();
    public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
    // Note: TagLog references PropertyGroup, not Property directly
}
