using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Role>> GetAllAsync()
    {
        // tblRole table doesn't exist - return empty list
        // Service layer will handle converting RoleType to RoleResponseDto
        return new List<Role>();
    }

    public async Task<Role?> GetByIdAsync(int roleId)
    {
        // tblRole table doesn't exist - return null
        // Service layer will handle converting RoleType to RoleResponseDto
        return null;
    }

    public async Task<List<RoleType>> GetAllRoleTypesAsync()
    {
        return await _context.RoleTypes.ToListAsync();
    }

    public async Task<RoleType?> GetRoleTypeByIdAsync(int roleTypeId)
    {
        return await _context.RoleTypes.FindAsync(roleTypeId);
    }

    public async Task<Role> CreateAsync(Role role)
    {
        // tblRole table doesn't exist - operation not supported
        throw new NotSupportedException("Role creation is not supported. Use RoleTypes instead.");
    }

    public async Task<Role> UpdateAsync(Role role)
    {
        // tblRole table doesn't exist - operation not supported
        throw new NotSupportedException("Role updates are not supported. Use RoleTypes instead.");
    }

    public async Task<bool> DeleteAsync(int roleId)
    {
        // tblRole table doesn't exist - operation not supported
        throw new NotSupportedException("Role deletion is not supported. Use RoleTypes instead.");
    }

    public async Task<List<UserRole>> GetUserRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.RoleType)
            .Where(ur => ur.UserId == userId)
            .ToListAsync();
    }

    public async Task AddUserRoleAsync(UserRole userRole)
    {
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveUserRoleAsync(int userId, int roleTypeId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleTypeId == roleTypeId);
        
        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }
    }
}
