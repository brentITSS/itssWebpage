namespace backend.DTOs;

public class CreateTenantRequest
{
    public int? TenancyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class UpdateTenantRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class TenantResponseDto
{
    public int TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateTenancyRequest
{
    public int PropertyId { get; set; }
    public int? TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MonthlyRent { get; set; }
}

public class UpdateTenancyRequest
{
    public int? PropertyId { get; set; }
    public int? TenantId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MonthlyRent { get; set; }
}

public class TenancyResponseDto
{
    public int TenancyId { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MonthlyRent { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<TenantResponseDto> Tenants { get; set; } = new();
}
