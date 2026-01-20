using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Services;

/// <summary>
/// Authentication service that handles login, password verification, and JWT token generation.
/// All authentication queries map exactly to the existing tblUser table structure.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// Authenticate user using email and password. Verifies against tblUser table.
    /// Uses BCrypt for secure password hashing verification.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Query tblUser table exactly as defined in ERD
        var user = await _userRepository.GetByEmailAsync(request.Email);
        
        // Check if user exists and is active
        if (user == null || !user.IsActive)
            return null;

        // Verify password using BCrypt (secure password hashing)
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var userDto = MapToUserDto(user);

        return new LoginResponse
        {
            Token = token,
            User = userDto
        };
    }

    /// <summary>
    /// Get current authenticated user information.
    /// </summary>
    public async Task<UserDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return MapToUserDto(user);
    }

    /// <summary>
    /// Check if user is a Global Admin (RoleType = 'Global Admin').
    /// </summary>
    public bool IsGlobalAdmin(UserDto user)
    {
        return user.IsGlobalAdmin;
    }

    /// <summary>
    /// Check if user has access to a specific workstream.
    /// Global Admins have access to all workstreams.
    /// </summary>
    public bool HasWorkstreamAccess(UserDto user, int workstreamId)
    {
        if (user.IsGlobalAdmin) return true;

        return user.WorkstreamAccess.Any(wa => wa.WorkstreamId == workstreamId);
    }

    /// <summary>
    /// Check if user has a specific permission type for a workstream.
    /// Global Admins have all permissions.
    /// </summary>
    public bool HasPermission(UserDto user, int workstreamId, string permissionType)
    {
        if (user.IsGlobalAdmin) return true;

        var access = user.WorkstreamAccess.FirstOrDefault(wa => wa.WorkstreamId == workstreamId);
        return access != null && access.PermissionTypeName.Equals(permissionType, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if user has Property Hub Admin permission or is Global Admin.
    /// Uses tblWorkstreamUsers + tblPermissionType to check for "Admin" permission on Property Hub workstream.
    /// </summary>
    public bool HasPropertyHubAdminAccess(UserDto user)
    {
        if (user.IsGlobalAdmin) return true;

        // Check if user has "Admin" permission type on Property Hub workstream
        var propertyHubAccess = user.WorkstreamAccess.FirstOrDefault(wa =>
            (wa.WorkstreamName.Equals("Property Hub", StringComparison.OrdinalIgnoreCase) ||
             wa.WorkstreamName.Contains("Property", StringComparison.OrdinalIgnoreCase)) &&
            wa.PermissionTypeName.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        return propertyHubAccess != null;
    }

    /// <summary>
    /// Generate JWT token for authenticated user.
    /// Includes user ID, email, roles, and workstream access in claims.
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim())
        };

        // Add role claims if UserRoles exist (handles case where related tables may not be populated)
        if (user.UserRoles != null && user.UserRoles.Any())
        {
            foreach (var userRole in user.UserRoles)
            {
                if (userRole.RoleType != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.RoleType.RoleTypeName));
                    
                    if (userRole.RoleType.RoleTypeName.Equals("Global Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        claims.Add(new Claim("IsGlobalAdmin", "true"));
                    }
                }
            }
        }

        // Add workstream access claims if WorkstreamUsers exist
        if (user.WorkstreamUsers != null && user.WorkstreamUsers.Any())
        {
            foreach (var workstreamUser in user.WorkstreamUsers)
            {
                if (workstreamUser.Workstream != null)
                {
                    claims.Add(new Claim("Workstream", workstreamUser.WorkstreamId.ToString()));
                    
                    if (workstreamUser.PermissionType != null)
                    {
                        claims.Add(new Claim($"Workstream_{workstreamUser.WorkstreamId}_Permission", workstreamUser.PermissionType.PermissionTypeName));
                    }
                }
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Map User entity from tblUser to UserDto.
    /// Handles cases where related tables (UserRoles, WorkstreamUsers) may not be populated.
    /// </summary>
    private UserDto MapToUserDto(User user)
    {
        var roles = new List<string>();
        var isGlobalAdmin = false;

        // Safely extract roles if UserRoles exist
        if (user.UserRoles != null && user.UserRoles.Any())
        {
            roles = user.UserRoles
                .Where(ur => ur.RoleType != null)
                .Select(ur => ur.RoleType!.RoleTypeName)
                .ToList();

            isGlobalAdmin = user.UserRoles
                .Any(ur => ur.RoleType != null && 
                    ur.RoleType.RoleTypeName.Equals("Global Admin", StringComparison.OrdinalIgnoreCase));
        }

        // Safely extract workstream access if WorkstreamUsers exist
        var workstreamAccess = new List<WorkstreamAccessDto>();
        if (user.WorkstreamUsers != null && user.WorkstreamUsers.Any())
        {
            workstreamAccess = user.WorkstreamUsers
                .Where(wu => wu.Workstream != null && wu.PermissionType != null)
                .Select(wu => new WorkstreamAccessDto
                {
                    WorkstreamId = wu.Workstream!.WorkstreamId,
                    WorkstreamName = wu.Workstream.WorkstreamName,
                    PermissionTypeId = wu.PermissionType!.PermissionTypeId,
                    PermissionTypeName = wu.PermissionType.PermissionTypeName
                })
                .ToList();
        }

        // Safely extract property group access if PropertyGroupUsers exist
        var propertyGroupAccess = new List<PropertyGroupAccessDto>();
        if (user.PropertyGroupUsers != null && user.PropertyGroupUsers.Any())
        {
            propertyGroupAccess = user.PropertyGroupUsers
                .Where(pgu => pgu.PropertyGroup != null && pgu.Active)
                .Select(pgu => new PropertyGroupAccessDto
                {
                    PropertyGroupId = pgu.PropertyGroup!.PropertyGroupId,
                    PropertyGroupName = pgu.PropertyGroup.PropertyGroupName ?? string.Empty
                })
                .ToList();
        }

        return new UserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            DefaultLoginLandingPage = user.DefaultLoginLandingPage,
            Roles = roles,
            WorkstreamAccess = workstreamAccess,
            PropertyGroupAccess = propertyGroupAccess,
            IsGlobalAdmin = isGlobalAdmin
        };
    }
}
