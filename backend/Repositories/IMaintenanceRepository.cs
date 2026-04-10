using backend.Models;

namespace backend.Repositories;

public interface IMaintenanceRepository
{
    Task<List<Maintenance>> GetAllAsync();
    Task<Maintenance?> GetByIdAsync(int maintenanceId);
    Task<Maintenance> CreateAsync(Maintenance maintenance);
    Task<Maintenance> UpdateAsync(Maintenance maintenance);
    Task<bool> DeleteAsync(int maintenanceId);

    Task<List<MaintenanceType>> GetAllMaintenanceTypesAsync();
    Task<MaintenanceType?> GetMaintenanceTypeByIdAsync(int id);
    Task<MaintenanceType> CreateMaintenanceTypeAsync(MaintenanceType type);
    Task<MaintenanceType> UpdateMaintenanceTypeAsync(MaintenanceType type);
    Task<bool> DeleteMaintenanceTypeAsync(int id);
    Task<int> CountMaintenancesByTypeAsync(int maintenanceTypeId);
}
