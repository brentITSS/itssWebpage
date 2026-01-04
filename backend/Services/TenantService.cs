using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public TenantService(ITenantRepository tenantRepository, IAuditLogRepository auditLogRepository)
    {
        _tenantRepository = tenantRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<TenantResponseDto>> GetAllTenantsAsync()
    {
        var tenants = await _tenantRepository.GetAllAsync();
        return tenants.Select(MapToTenantResponseDto).ToList();
    }

    public async Task<TenantResponseDto?> GetTenantByIdAsync(int tenantId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null) return null;

        return MapToTenantResponseDto(tenant);
    }

    public async Task<TenantResponseDto> CreateTenantAsync(CreateTenantRequest request, int createdByUserId)
    {
        var tenant = new Tenant
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        tenant = await _tenantRepository.CreateAsync(tenant);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = createdByUserId,
            Action = "Create",
            EntityType = "Tenant",
            EntityId = tenant.TenantId,
            NewValues = $"Name: {tenant.FirstName} {tenant.LastName}, Email: {tenant.Email}",
            CreatedDate = DateTime.UtcNow
        });

        return MapToTenantResponseDto(tenant);
    }

    public async Task<TenantResponseDto?> UpdateTenantAsync(int tenantId, UpdateTenantRequest request, int modifiedByUserId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null) return null;

        var oldValues = $"Name: {tenant.FirstName} {tenant.LastName}, Email: {tenant.Email}, Phone: {tenant.Phone}";

        if (request.FirstName != null) tenant.FirstName = request.FirstName;
        if (request.LastName != null) tenant.LastName = request.LastName;
        if (request.Email != null) tenant.Email = request.Email;
        if (request.Phone != null) tenant.Phone = request.Phone;
        tenant.ModifiedDate = DateTime.UtcNow;
        tenant.ModifiedByUserId = modifiedByUserId;

        tenant = await _tenantRepository.UpdateAsync(tenant);

        // Audit log
        var newValues = $"Name: {tenant.FirstName} {tenant.LastName}, Email: {tenant.Email}, Phone: {tenant.Phone}";
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "Update",
            EntityType = "Tenant",
            EntityId = tenantId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedDate = DateTime.UtcNow
        });

        return MapToTenantResponseDto(tenant);
    }

    public async Task<bool> DeleteTenantAsync(int tenantId, int deletedByUserId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null) return false;

        var result = await _tenantRepository.DeleteAsync(tenantId);

        if (result)
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = deletedByUserId,
                Action = "Delete",
                EntityType = "Tenant",
                EntityId = tenantId,
                OldValues = $"Name: {tenant.FirstName} {tenant.LastName}",
                CreatedDate = DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<List<TenancyResponseDto>> GetAllTenanciesAsync()
    {
        var tenancies = await _tenantRepository.GetAllTenanciesAsync();
        return tenancies.Select(MapToTenancyResponseDto).ToList();
    }

    public async Task<TenancyResponseDto?> GetTenancyByIdAsync(int tenancyId)
    {
        var tenancy = await _tenantRepository.GetTenancyByIdAsync(tenancyId);
        if (tenancy == null) return null;

        return MapToTenancyResponseDto(tenancy);
    }

    public async Task<TenancyResponseDto> CreateTenancyAsync(CreateTenancyRequest request, int createdByUserId)
    {
        var tenancy = new Tenancy
        {
            PropertyId = request.PropertyId,
            TenantId = request.TenantId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MonthlyRent = request.MonthlyRent,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        tenancy = await _tenantRepository.CreateTenancyAsync(tenancy);
        tenancy = await _tenantRepository.GetTenancyByIdAsync(tenancy.TenancyId);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = createdByUserId,
            Action = "Create",
            EntityType = "Tenancy",
            EntityId = tenancy.TenancyId,
            NewValues = $"PropertyId: {tenancy.PropertyId}, TenantId: {tenancy.TenantId}, StartDate: {tenancy.StartDate}",
            CreatedDate = DateTime.UtcNow
        });

        return MapToTenancyResponseDto(tenancy!);
    }

    public async Task<TenancyResponseDto?> UpdateTenancyAsync(int tenancyId, UpdateTenancyRequest request, int modifiedByUserId)
    {
        var tenancy = await _tenantRepository.GetTenancyByIdAsync(tenancyId);
        if (tenancy == null) return null;

        var oldValues = $"PropertyId: {tenancy.PropertyId}, TenantId: {tenancy.TenantId}, StartDate: {tenancy.StartDate}, EndDate: {tenancy.EndDate}";

        if (request.PropertyId.HasValue) tenancy.PropertyId = request.PropertyId.Value;
        if (request.TenantId.HasValue) tenancy.TenantId = request.TenantId.Value;
        if (request.StartDate.HasValue) tenancy.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) tenancy.EndDate = request.EndDate;
        if (request.MonthlyRent.HasValue) tenancy.MonthlyRent = request.MonthlyRent;
        tenancy.ModifiedDate = DateTime.UtcNow;
        tenancy.ModifiedByUserId = modifiedByUserId;

        tenancy = await _tenantRepository.UpdateTenancyAsync(tenancy);
        tenancy = await _tenantRepository.GetTenancyByIdAsync(tenancyId);

        // Audit log
        var newValues = $"PropertyId: {tenancy!.PropertyId}, TenantId: {tenancy.TenantId}, StartDate: {tenancy.StartDate}, EndDate: {tenancy.EndDate}";
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "Update",
            EntityType = "Tenancy",
            EntityId = tenancyId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedDate = DateTime.UtcNow
        });

        return MapToTenancyResponseDto(tenancy);
    }

    public async Task<bool> DeleteTenancyAsync(int tenancyId, int deletedByUserId)
    {
        var tenancy = await _tenantRepository.GetTenancyByIdAsync(tenancyId);
        if (tenancy == null) return false;

        var result = await _tenantRepository.DeleteTenancyAsync(tenancyId);

        if (result)
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = deletedByUserId,
                Action = "Delete",
                EntityType = "Tenancy",
                EntityId = tenancyId,
                OldValues = $"PropertyId: {tenancy.PropertyId}, TenantId: {tenancy.TenantId}",
                CreatedDate = DateTime.UtcNow
            });
        }

        return result;
    }

    private TenantResponseDto MapToTenantResponseDto(Tenant tenant)
    {
        return new TenantResponseDto
        {
            TenantId = tenant.TenantId,
            FirstName = tenant.FirstName,
            LastName = tenant.LastName,
            Email = tenant.Email,
            Phone = tenant.Phone,
            CreatedDate = tenant.CreatedDate
        };
    }

    private TenancyResponseDto MapToTenancyResponseDto(Tenancy tenancy)
    {
        return new TenancyResponseDto
        {
            TenancyId = tenancy.TenancyId,
            PropertyId = tenancy.PropertyId,
            PropertyName = tenancy.Property.PropertyName,
            TenantId = tenancy.TenantId,
            TenantName = $"{tenancy.Tenant.FirstName} {tenancy.Tenant.LastName}".Trim(),
            StartDate = tenancy.StartDate,
            EndDate = tenancy.EndDate,
            MonthlyRent = tenancy.MonthlyRent ?? 0,
            CreatedDate = tenancy.CreatedDate
        };
    }
}
