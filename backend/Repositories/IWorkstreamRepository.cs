using backend.Models;

namespace backend.Repositories;

public interface IWorkstreamRepository
{
    Task<List<Workstream>> GetAllAsync();
    Task<Workstream?> GetByIdAsync(int workstreamId);
    Task<List<PermissionType>> GetAllPermissionTypesAsync();
    Task<PermissionType?> GetPermissionTypeByIdAsync(int permissionTypeId);
    Task<Workstream> CreateAsync(Workstream workstream);
    Task<Workstream> UpdateAsync(Workstream workstream);
    Task<bool> DeleteAsync(int workstreamId);
    Task<List<WorkstreamUser>> GetWorkstreamUsersAsync(int workstreamId);
    Task<List<WorkstreamUser>> GetUserWorkstreamsAsync(int userId);
    Task<WorkstreamUser> AddWorkstreamUserAsync(WorkstreamUser workstreamUser);
    Task<bool> RemoveWorkstreamUserAsync(int workstreamId, int userId);
}
