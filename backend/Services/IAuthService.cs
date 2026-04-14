using backend.DTOs;

namespace backend.Services;

/// <summary>
/// Service for authentication operations. Handles login, JWT token generation, and user retrieval.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user with email and password against tblUser table.
    /// Returns JWT token and user information on success.
    /// </summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    
    /// <summary>
    /// Get current authenticated user information.
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync(int userId);
    
    /// <summary>
    /// Check if user is a Global Admin (RoleType = 'Global Admin').
    /// </summary>
    bool IsGlobalAdmin(UserDto user);
    
    /// <summary>
    /// Check if user has access to a specific workstream.
    /// </summary>
    bool HasWorkstreamAccess(UserDto user, int workstreamId);
    
    /// <summary>
    /// Check if user has a specific permission type for a workstream.
    /// </summary>
    bool HasPermission(UserDto user, int workstreamId, string permissionType);
    
    /// <summary>
    /// Check if user has Property Hub Admin permission or is Global Admin.
    /// Uses tblWorkstreamUsers + tblPermissionType to check for "Admin" permission on Property Hub workstream.
    /// </summary>
    bool HasPropertyHubAdminAccess(UserDto user);

    /// <summary>
    /// Create a time-limited password reset token for the user (if the account exists and is active).
    /// </summary>
    Task<ForgotPasswordResponse> RequestPasswordResetAsync(string email);

    /// <summary>
    /// Set a new password using a valid, unconsumed reset token.
    /// </summary>
    Task<bool> CompletePasswordResetAsync(string token, string newPassword);

    /// <summary>
    /// Change password for authenticated user and clear password-change requirement flag.
    /// </summary>
    Task<bool> ChangePasswordAsync(int userId, string newPassword);
}
