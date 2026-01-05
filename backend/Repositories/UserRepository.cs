using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

/// <summary>
/// Repository for accessing tblUser table. All queries map exactly to the existing database structure.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Query tblUser by email for authentication. Maps exactly to existing table structure.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        // First, get the user from tblUser (exact table match)
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return null;

        // Load related data if tables exist (handles case where related tables may not be populated)
        var userWithRelations = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.RoleType)
            .Include(u => u.WorkstreamUsers)
                .ThenInclude(wu => wu.Workstream)
            .Include(u => u.WorkstreamUsers)
                .ThenInclude(wu => wu.PermissionType)
            .FirstOrDefaultAsync(u => u.UserId == user.UserId);

        return userWithRelations ?? user;
    }

    /// <summary>
    /// Get user by ID with related data for JWT token generation.
    /// </summary>
    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.RoleType)
            .Include(u => u.WorkstreamUsers)
                .ThenInclude(wu => wu.Workstream)
            .Include(u => u.WorkstreamUsers)
                .ThenInclude(wu => wu.PermissionType)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <summary>
    /// Get all users with related data.
    /// </summary>
    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.RoleType)
            .Include(u => u.WorkstreamUsers)
                .ThenInclude(wu => wu.Workstream)
            .Include(u => u.WorkstreamUsers)
                .ThenInclude(wu => wu.PermissionType)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Create a new user in tblUser table.
    /// </summary>
    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Update an existing user in tblUser table.
    /// </summary>
    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Check if email already exists in tblUser table.
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email && (excludeUserId == null || u.UserId != excludeUserId));
    }
}
