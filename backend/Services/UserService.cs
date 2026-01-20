using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<UserResponseDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToUserResponseDto).ToList();
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return MapToUserResponseDto(user);
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserRequest request, int createdByUserId)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            DefaultLoginLandingPage = request.DefaultLoginLandingPage,
            IsActive = true
        };

        user = await _userRepository.CreateAsync(user);

        // Assign roles
        foreach (var roleTypeId in request.RoleIds)
        {
            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleTypeId = roleTypeId
            };
            await _roleRepository.AddUserRoleAsync(userRole);
        }

        // Reload user with roles
        user = await _userRepository.GetByIdAsync(user.UserId);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = createdByUserId,
            Action = "Create",
            EntityType = "User",
            EntityId = user.UserId,
            NewValues = $"Email: {user.Email}, FirstName: {user.FirstName}, LastName: {user.LastName}",
            CreatedDate = DateTime.UtcNow
        });

        return MapToUserResponseDto(user!);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(int userId, UpdateUserRequest request, int modifiedByUserId)
    {
        // Get user for update (tracked, no navigation properties)
        var user = await _userRepository.GetByIdForUpdateAsync(userId);
        if (user == null) return null;

        // Get old values for audit log (with navigation properties)
        var oldUser = await _userRepository.GetByIdAsync(userId);
        var oldValues = $"Email: {oldUser!.Email}, FirstName: {oldUser.FirstName}, LastName: {oldUser.LastName}, IsActive: {oldUser.IsActive}";

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.DefaultLoginLandingPage != null) user.DefaultLoginLandingPage = request.DefaultLoginLandingPage;

        // Update roles if provided
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            var currentRoles = await _roleRepository.GetUserRolesAsync(userId);
            var currentRoleTypeIds = currentRoles.Select(ur => ur.RoleTypeId).ToList();

            // Remove roles not in the new list
            foreach (var roleTypeId in currentRoleTypeIds)
            {
                if (!request.RoleIds.Contains(roleTypeId))
                {
                    await _roleRepository.RemoveUserRoleAsync(userId, roleTypeId);
                }
            }

            // Add new roles
            foreach (var roleTypeId in request.RoleIds)
            {
                if (!currentRoleTypeIds.Contains(roleTypeId))
                {
                    await _roleRepository.AddUserRoleAsync(new UserRole
                    {
                        UserId = userId,
                        RoleTypeId = roleTypeId
                    });
                }
            }
        }

        user = await _userRepository.UpdateAsync(user);
        user = await _userRepository.GetByIdAsync(userId);

        // Audit log
        var newValues = $"Email: {user!.Email}, FirstName: {user.FirstName}, LastName: {user.LastName}, IsActive: {user.IsActive}";
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "Update",
            EntityType = "User",
            EntityId = userId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedDate = DateTime.UtcNow
        });

        return MapToUserResponseDto(user);
    }

    public async Task<bool> DeleteUserAsync(int userId, int deletedByUserId)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId);
        if (user == null) return false;

        // Soft delete by deactivating
        user.IsActive = false;
        await _userRepository.UpdateAsync(user);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = deletedByUserId,
            Action = "Delete",
            EntityType = "User",
            EntityId = userId,
            OldValues = $"Email: {user.Email}",
            CreatedDate = DateTime.UtcNow
        });

        return true;
    }

    public async Task<bool> ResetPasswordAsync(int userId, ResetPasswordRequest request, int modifiedByUserId)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user);

        // Audit log (wrap in try-catch to prevent audit log failures from breaking the operation)
        try
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = modifiedByUserId,
                Action = "ResetPassword",
                EntityType = "User",
                EntityId = userId,
                CreatedDate = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the password reset
            // In production, you might want to use a proper logging framework
            Console.WriteLine($"Failed to create audit log: {ex.Message}");
        }

        return true;
    }

    private UserResponseDto MapToUserResponseDto(User user)
    {
        var roles = user.UserRoles.Select(ur => new RoleDto
        {
            RoleId = ur.RoleTypeId,
            RoleName = ur.RoleType?.RoleTypeName ?? "",
            RoleTypeName = ur.RoleType?.RoleTypeName ?? ""
        }).ToList();

        var workstreamAccess = user.WorkstreamUsers.Select(wu => new WorkstreamAccessDto
        {
            WorkstreamId = wu.Workstream.WorkstreamId,
            WorkstreamName = wu.Workstream.WorkstreamName,
            PermissionTypeId = wu.PermissionType.PermissionTypeId,
            PermissionTypeName = wu.PermissionType.PermissionTypeName
        }).ToList();

        return new UserResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            DefaultLoginLandingPage = user.DefaultLoginLandingPage,
            CreatedDate = DateTime.UtcNow, // Database doesn't have CreatedDate column, using current time as placeholder
            Roles = roles,
            WorkstreamAccess = workstreamAccess
        };
    }
}
