using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly ApplicationDbContext _context;

    public MaintenanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Maintenance>> GetAllAsync()
    {
        return await _context.Maintenances
            .Include(m => m.Property)
            .Include(m => m.PropertyGroup)
            .Include(m => m.MaintenanceType)
            .OrderByDescending(m => m.WorkDate ?? m.CreatedDate)
            .ToListAsync();
    }

    public async Task<Maintenance?> GetByIdAsync(int maintenanceId)
    {
        return await _context.Maintenances
            .Include(m => m.Property)
            .Include(m => m.PropertyGroup)
            .Include(m => m.MaintenanceType)
            .FirstOrDefaultAsync(m => m.MaintenanceId == maintenanceId);
    }

    public async Task<Maintenance> CreateAsync(Maintenance maintenance)
    {
        if (maintenance.CreatedDate == null)
            maintenance.CreatedDate = DateTime.UtcNow;
        _context.Maintenances.Add(maintenance);
        await _context.SaveChangesAsync();
        return maintenance;
    }

    public async Task<Maintenance> UpdateAsync(Maintenance maintenance)
    {
        _context.Maintenances.Update(maintenance);
        await _context.SaveChangesAsync();
        return maintenance;
    }

    public async Task<bool> DeleteAsync(int maintenanceId)
    {
        var entity = await _context.Maintenances.FindAsync(maintenanceId);
        if (entity == null) return false;
        _context.Maintenances.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MaintenanceType>> GetAllMaintenanceTypesAsync()
    {
        return await _context.MaintenanceTypes
            .OrderBy(t => t.MaintenanceTypeName)
            .ToListAsync();
    }

    public async Task<MaintenanceType?> GetMaintenanceTypeByIdAsync(int id)
    {
        return await _context.MaintenanceTypes.FindAsync(id);
    }

    public async Task<MaintenanceType> CreateMaintenanceTypeAsync(MaintenanceType type)
    {
        if (type.CreatedDate == null)
            type.CreatedDate = DateTime.UtcNow;
        _context.MaintenanceTypes.Add(type);
        await _context.SaveChangesAsync();
        return type;
    }

    public async Task<MaintenanceType> UpdateMaintenanceTypeAsync(MaintenanceType type)
    {
        _context.MaintenanceTypes.Update(type);
        await _context.SaveChangesAsync();
        return type;
    }

    public async Task<bool> DeleteMaintenanceTypeAsync(int id)
    {
        var entity = await _context.MaintenanceTypes.FindAsync(id);
        if (entity == null) return false;
        _context.MaintenanceTypes.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountMaintenancesByTypeAsync(int maintenanceTypeId)
    {
        return await _context.Maintenances.CountAsync(m => m.MaintenanceTypeId == maintenanceTypeId);
    }
}
