using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;

    public TenantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Tenant>> GetAllAsync()
    {
        return await _context.Tenants.ToListAsync();
    }

    public async Task<Tenant?> GetByIdAsync(int tenantId)
    {
        return await _context.Tenants.FindAsync(tenantId);
    }

    public async Task<Tenant> CreateAsync(Tenant tenant)
    {
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    public async Task<bool> DeleteAsync(int tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return false;

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Tenancy>> GetAllTenanciesAsync()
    {
        return await _context.Tenancies
            .Include(t => t.Property)
                .ThenInclude(p => p.PropertyGroup)
            .Include(t => t.Tenant)
            .ToListAsync();
    }

    public async Task<Tenancy?> GetTenancyByIdAsync(int tenancyId)
    {
        return await _context.Tenancies
            .Include(t => t.Property)
                .ThenInclude(p => p.PropertyGroup)
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.TenancyId == tenancyId);
    }

    public async Task<List<Tenancy>> GetTenanciesByPropertyAsync(int propertyId)
    {
        return await _context.Tenancies
            .Include(t => t.Property)
            .Include(t => t.Tenant)
            .Where(t => t.PropertyId == propertyId)
            .ToListAsync();
    }

    public async Task<List<Tenancy>> GetTenanciesByTenantAsync(int tenantId)
    {
        return await _context.Tenancies
            .Include(t => t.Property)
                .ThenInclude(p => p.PropertyGroup)
            .Include(t => t.Tenant)
            .Where(t => t.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<Tenancy> CreateTenancyAsync(Tenancy tenancy)
    {
        _context.Tenancies.Add(tenancy);
        await _context.SaveChangesAsync();
        return tenancy;
    }

    public async Task<Tenancy> UpdateTenancyAsync(Tenancy tenancy)
    {
        _context.Tenancies.Update(tenancy);
        await _context.SaveChangesAsync();
        return tenancy;
    }

    public async Task<bool> DeleteTenancyAsync(int tenancyId)
    {
        var tenancy = await _context.Tenancies.FindAsync(tenancyId);
        if (tenancy == null) return false;

        _context.Tenancies.Remove(tenancy);
        await _context.SaveChangesAsync();
        return true;
    }
}
