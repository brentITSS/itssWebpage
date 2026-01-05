using backend.Models;

namespace backend.Repositories;

public interface IRoleRepository
{
    Task<List<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(int roleId);
    Task<List<RoleType>> GetAllRoleTypesAsync();
    Task<RoleType?> GetRoleTypeByIdAsync(int roleTypeId);
    Task<Role> CreateAsync(Role role);
    Task<Role> UpdateAsync(Role role);
    Task<bool> DeleteAsync(int roleId);
    Task<List<UserRole>> GetUserRolesAsync(int userId);
    Task AddUserRoleAsync(UserRole userRole);
    Task RemoveUserRoleAsync(int userId, int roleTypeId);
}
