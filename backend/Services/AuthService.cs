using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace backend.Services;

/// <summary>
/// Authentication service that handles login, password verification, and JWT token generation.
/// All authentication queries map exactly to the existing tblUser table structure.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IAuditLogRepository auditLogRepository,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _auditLogRepository = auditLogRepository;
        _httpContextAccessor = httpContextAccessor;
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

        // Verify password safely; handle legacy bcrypt prefixes from older systems.
        if (!TryVerifyPassword(request.Password, user.PasswordHash, out var shouldRehash))
            return null;

        if (shouldRehash)
        {
            // Upgrade legacy hash format after successful login.
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            await _userRepository.UpdateAsync(user);
        }

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var userDto = MapToUserDto(user);
        await TryAuditSuccessfulLoginAsync(user);

        return new LoginResponse
        {
            Token = token,
            User = userDto
        };
    }

    private async Task TryAuditSuccessfulLoginAsync(User user)
    {
        try
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = user.UserId,
                Action = "Login",
                EntityType = "Auth",
                EntityId = user.UserId,
                NewValues = $"Successful login for {user.Email}",
                CreatedDate = DateTime.UtcNow,
                IpAddress = ipAddress
            });
        }
        catch
        {
            // Do not block login if audit logging fails.
        }
    }

    private static bool TryVerifyPassword(string plainPassword, string storedHash, out bool shouldRehash)
    {
        shouldRehash = false;

        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        var hash = storedHash.Trim();

        try
        {
            if (BCrypt.Net.BCrypt.Verify(plainPassword, hash))
                return true;
        }
        catch
        {
            // Try normalized legacy prefixes below.
        }

        var normalizedHash = NormalizeLegacyBcryptPrefix(hash);
        if (normalizedHash == hash)
            return false;

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(plainPassword, normalizedHash))
                return false;

            shouldRehash = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeLegacyBcryptPrefix(string hash)
    {
        if (hash.StartsWith("$2y$"))
            return "$2a$" + hash[4..];

        if (hash.StartsWith("$2x$"))
            return "$2a$" + hash[4..];

        return hash;
    }

    public async Task<ForgotPasswordResponse> RequestPasswordResetAsync(string email)
    {
        var genericMessage =
            "If an account exists for that email, you can use the password reset link to set a new password.";

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || !user.IsActive)
        {
            return new ForgotPasswordResponse { Message = genericMessage };
        }

        var plainToken = GenerateResetToken();
        var tokenHash = HashResetToken(plainToken);
        var ttlMinutes = _configuration.GetValue("PasswordReset:TokenExpirationMinutes", 60);
        var now = DateTime.UtcNow;
        await _passwordResetTokenRepository.CreateForUserAsync(
            user.UserId,
            tokenHash,
            now.AddMinutes(ttlMinutes),
            now);

        var resetPath = $"/ResetPassword?token={Uri.EscapeDataString(plainToken)}";

        var exposeToken = _configuration.GetValue("PasswordReset:ReturnResetTokenInResponse", false);
        if (exposeToken)
        {
            return new ForgotPasswordResponse
            {
                Message =
                    "Reset token returned only because PasswordReset:ReturnResetTokenInResponse is enabled. Turn it off in production and send the link by email instead.",
                ResetToken = plainToken,
                ResetPath = resetPath
            };
        }

        return new ForgotPasswordResponse { Message = genericMessage };
    }

    public async Task<bool> CompletePasswordResetAsync(string token, string newPassword)
    {
        var tokenHash = HashResetToken(token);
        var utcNow = DateTime.UtcNow;
        var userId = await _passwordResetTokenRepository.TryConsumeTokenAsync(tokenHash, utcNow);
        if (userId == null)
            return false;

        var user = await _userRepository.GetByIdForUpdateAsync(userId.Value);
        if (user == null || !user.IsActive)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = false;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId);
        if (user == null || !user.IsActive)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = false;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    private static string GenerateResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashResetToken(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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
            MatchesPropertyHubWorkstreamName(wa.WorkstreamName) &&
            wa.PermissionTypeName.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        return propertyHubAccess != null;
    }

    /// <inheritdoc />
    public bool CanMutatePropertyHubOperationalData(UserDto user)
    {
        if (user.IsGlobalAdmin) return true;

        var ranks = user.WorkstreamAccess
            .Where(wa => MatchesPropertyHubWorkstreamName(wa.WorkstreamName))
            .Select(wa => GetPropertyHubOperationalWriteRank(wa.PermissionTypeName))
            .ToList();

        if (ranks.Count == 0) return false;

        // Edit (2) or Admin (3) on a Property Hub workstream may mutate operational records; View (1) is read-only.
        return ranks.Max() >= 2;
    }

    private static bool MatchesPropertyHubWorkstreamName(string workstreamName)
    {
        return workstreamName.Equals("Property Hub", StringComparison.OrdinalIgnoreCase) ||
               workstreamName.Contains("Property", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Rank for operational write access: 0 = none, 1 = view/read, 2 = edit, 3 = admin.
    /// Unknown permission names default to view-only (1) for safety.
    /// </summary>
    private static int GetPropertyHubOperationalWriteRank(string? permissionTypeName)
    {
        if (string.IsNullOrWhiteSpace(permissionTypeName)) return 0;

        var p = permissionTypeName.Trim().ToLowerInvariant();
        if (p == "admin") return 3;
        if (p is "edit" or "editor") return 2;
        if (p is "view" or "read" or "readonly") return 1;
        return 1;
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
            IsGlobalAdmin = isGlobalAdmin,
            MustChangePassword = user.MustChangePassword
        };
    }
}
