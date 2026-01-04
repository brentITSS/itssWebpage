using backend.DTOs;

namespace backend.Services;

public interface ITenantService
{
    Task<List<TenantResponseDto>> GetAllTenantsAsync();
    Task<TenantResponseDto?> GetTenantByIdAsync(int tenantId);
    Task<TenantResponseDto> CreateTenantAsync(CreateTenantRequest request, int createdByUserId);
    Task<TenantResponseDto?> UpdateTenantAsync(int tenantId, UpdateTenantRequest request, int modifiedByUserId);
    Task<bool> DeleteTenantAsync(int tenantId, int deletedByUserId);
    
    Task<List<TenancyResponseDto>> GetAllTenanciesAsync();
    Task<TenancyResponseDto?> GetTenancyByIdAsync(int tenancyId);
    Task<TenancyResponseDto> CreateTenancyAsync(CreateTenancyRequest request, int createdByUserId);
    Task<TenancyResponseDto?> UpdateTenancyAsync(int tenancyId, UpdateTenancyRequest request, int modifiedByUserId);
    Task<bool> DeleteTenancyAsync(int tenancyId, int deletedByUserId);
}
