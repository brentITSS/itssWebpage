using backend.DTOs;

namespace backend.Services;

public interface IRoleService
{
    Task<List<RoleResponseDto>> GetAllRolesAsync();
    Task<RoleResponseDto?> GetRoleByIdAsync(int roleId);
    Task<List<RoleTypeDto>> GetAllRoleTypesAsync();
    Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequest request);
    Task<RoleResponseDto?> UpdateRoleAsync(int roleId, UpdateRoleRequest request);
    Task<bool> DeleteRoleAsync(int roleId);
}
