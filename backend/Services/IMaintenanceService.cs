using backend.DTOs;

namespace backend.Services;

public interface IMaintenanceService
{
    Task<List<MaintenanceTypeDto>> GetAllMaintenanceTypesAsync();
    Task<MaintenanceTypeDto?> GetMaintenanceTypeByIdAsync(int id);
    Task<MaintenanceTypeDto> CreateMaintenanceTypeAsync(CreateMaintenanceTypeRequest request);
    Task<MaintenanceTypeDto?> UpdateMaintenanceTypeAsync(int id, UpdateMaintenanceTypeRequest request);
    Task<bool> IsMaintenanceTypeInUseAsync(int id);
    Task<bool> DeleteMaintenanceTypeAsync(int id);

    Task<List<MaintenanceResponseDto>> GetAllMaintenancesForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin);
    Task<MaintenanceResponseDto?> GetMaintenanceByIdForUserAsync(int id, int userId, bool isGlobalAdmin, bool isPropertyHubAdmin);
    Task<MaintenanceResponseDto> CreateMaintenanceAsync(CreateMaintenanceRequest request);
    Task<MaintenanceResponseDto?> UpdateMaintenanceAsync(int id, UpdateMaintenanceRequest request);
    Task<bool> DeleteMaintenanceAsync(int id);
}
