using backend.Models;

namespace backend.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// Get user by email for authentication. Queries tblUser table exactly as defined in ERD.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);
    
    /// <summary>
    /// Get user by ID with related data for JWT token generation.
    /// </summary>
    Task<User?> GetByIdAsync(int userId);
    
    /// <summary>
    /// Get user by ID for updates (tracked entity, no navigation properties).
    /// </summary>
    Task<User?> GetByIdForUpdateAsync(int userId);
    
    /// <summary>
    /// Get all users with related data.
    /// </summary>
    Task<List<User>> GetAllAsync();
    
    /// <summary>
    /// Create a new user in tblUser table.
    /// </summary>
    Task<User> CreateAsync(User user);
    
    /// <summary>
    /// Update an existing user in tblUser table.
    /// </summary>
    Task<User> UpdateAsync(User user);
    
    /// <summary>
    /// Check if email already exists in tblUser table.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
}
