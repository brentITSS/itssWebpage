using backend.DTOs;

namespace backend.Services;

public interface IWorkstreamService
{
    Task<List<WorkstreamResponseDto>> GetAllWorkstreamsAsync();
    Task<WorkstreamResponseDto?> GetWorkstreamByIdAsync(int workstreamId);
    Task<List<PermissionTypeDto>> GetAllPermissionTypesAsync();
    Task<WorkstreamResponseDto> CreateWorkstreamAsync(CreateWorkstreamRequest request);
    Task<WorkstreamResponseDto?> UpdateWorkstreamAsync(int workstreamId, UpdateWorkstreamRequest request);
    Task<bool> DeleteWorkstreamAsync(int workstreamId);
    Task<List<UserResponseDto>> GetWorkstreamUsersAsync(int workstreamId);
    Task AssignUserToWorkstreamAsync(int workstreamId, AssignWorkstreamUserRequest request);
    Task RemoveUserFromWorkstreamAsync(int workstreamId, int userId);
}
