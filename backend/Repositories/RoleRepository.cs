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
        return await _context.Roles
            .Include(r => r.RoleType)
            .ToListAsync();
    }

    public async Task<Role?> GetByIdAsync(int roleId)
    {
        return await _context.Roles
            .Include(r => r.RoleType)
            .FirstOrDefaultAsync(r => r.RoleId == roleId);
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
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<Role> UpdateAsync(Role role)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteAsync(int roleId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null) return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserRole>> GetUserRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
                .ThenInclude(r => r.RoleType)
            .Where(ur => ur.UserId == userId)
            .ToListAsync();
    }

    public async Task AddUserRoleAsync(UserRole userRole)
    {
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveUserRoleAsync(int userId, int roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        
        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }
    }
}
