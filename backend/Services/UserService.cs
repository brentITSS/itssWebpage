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
            IsActive = true
        };

        user = await _userRepository.CreateAsync(user);

        // Assign roles
        foreach (var roleId in request.RoleIds)
        {
            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = roleId,
                CreatedDate = DateTime.UtcNow
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
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var oldValues = $"Email: {user.Email}, FirstName: {user.FirstName}, LastName: {user.LastName}, IsActive: {user.IsActive}";

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        // Update roles if provided
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            var currentRoles = await _roleRepository.GetUserRolesAsync(userId);
            var currentRoleIds = currentRoles.Select(ur => ur.RoleId).ToList();

            // Remove roles not in the new list
            foreach (var roleId in currentRoleIds)
            {
                if (!request.RoleIds.Contains(roleId))
                {
                    await _roleRepository.RemoveUserRoleAsync(userId, roleId);
                }
            }

            // Add new roles
            foreach (var roleId in request.RoleIds)
            {
                if (!currentRoleIds.Contains(roleId))
                {
                    await _roleRepository.AddUserRoleAsync(new UserRole
                    {
                        UserId = userId,
                        RoleId = roleId,
                        CreatedDate = DateTime.UtcNow
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
        var user = await _userRepository.GetByIdAsync(userId);
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
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "ResetPassword",
            EntityType = "User",
            EntityId = userId,
            CreatedDate = DateTime.UtcNow
        });

        return true;
    }

    private UserResponseDto MapToUserResponseDto(User user)
    {
        var roles = user.UserRoles.Select(ur => new RoleDto
        {
            RoleId = ur.Role.RoleId,
            RoleName = ur.Role.RoleName,
            RoleTypeName = ur.Role.RoleType.RoleTypeName
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
            CreatedDate = DateTime.UtcNow, // Database doesn't have CreatedDate column, using current time as placeholder
            Roles = roles,
            WorkstreamAccess = workstreamAccess
        };
    }
}
