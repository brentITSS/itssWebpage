using backend.Models;

namespace backend.Repositories;

public interface ITenantRepository
{
    Task<List<Tenant>> GetAllAsync();
    Task<Tenant?> GetByIdAsync(int tenantId);
    Task<Tenant> CreateAsync(Tenant tenant);
    Task<Tenant> UpdateAsync(Tenant tenant);
    Task<bool> DeleteAsync(int tenantId);
    
    Task<List<Tenancy>> GetAllTenanciesAsync();
    Task<Tenancy?> GetTenancyByIdAsync(int tenancyId);
    Task<List<Tenancy>> GetTenanciesByPropertyAsync(int propertyId);
    Task<List<Tenancy>> GetTenanciesByTenantAsync(int tenantId);
    Task<Tenancy> CreateTenancyAsync(Tenancy tenancy);
    Task<Tenancy> UpdateTenancyAsync(Tenancy tenancy);
    Task<bool> DeleteTenancyAsync(int tenancyId);
}
