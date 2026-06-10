using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<List<RoleResponseDto>> GetAllRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        if (roles.Count > 0)
        {
            return roles.Select(r => new RoleResponseDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                RoleTypeId = r.RoleTypeId,
                RoleTypeName = r.RoleType?.RoleTypeName ?? string.Empty,
                CreatedDate = r.CreatedDate
            }).ToList();
        }

        // Fallback when tblRole has no definition rows yet.
        var roleTypes = await _roleRepository.GetAllRoleTypesAsync();
        return roleTypes.Select(rt => new RoleResponseDto
        {
            RoleId = rt.RoleTypeId,
            RoleName = rt.RoleTypeName,
            RoleTypeId = rt.RoleTypeId,
            RoleTypeName = rt.RoleTypeName,
            CreatedDate = DateTime.UtcNow
        }).ToList();
    }

    public async Task<RoleResponseDto?> GetRoleByIdAsync(int roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role != null)
        {
            return new RoleResponseDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                RoleTypeId = role.RoleTypeId,
                RoleTypeName = role.RoleType?.RoleTypeName ?? string.Empty,
                CreatedDate = role.CreatedDate
            };
        }

        var roleType = await _roleRepository.GetRoleTypeByIdAsync(roleId);
        if (roleType == null) return null;

        return new RoleResponseDto
        {
            RoleId = roleType.RoleTypeId,
            RoleName = roleType.RoleTypeName,
            RoleTypeId = roleType.RoleTypeId,
            RoleTypeName = roleType.RoleTypeName,
            CreatedDate = DateTime.UtcNow
        };
    }

    public async Task<List<RoleTypeDto>> GetAllRoleTypesAsync()
    {
        var roleTypes = await _roleRepository.GetAllRoleTypesAsync();
        return roleTypes.Select(rt => new RoleTypeDto
        {
            RoleTypeId = rt.RoleTypeId,
            RoleTypeName = rt.RoleTypeName
        }).ToList();
    }

    public async Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequest request)
    {
        // Since tblRole doesn't exist, this operation is not supported
        // Roles are managed through RoleTypes
        throw new InvalidOperationException("Role creation is not supported. Use RoleTypes instead.");
    }

    public async Task<RoleResponseDto?> UpdateRoleAsync(int roleId, UpdateRoleRequest request)
    {
        // Since tblRole doesn't exist, this operation is not supported
        // Roles are managed through RoleTypes
        throw new InvalidOperationException("Role updates are not supported. Use RoleTypes instead.");
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        // Since tblRole doesn't exist, this operation is not supported
        // Roles are managed through RoleTypes
        throw new InvalidOperationException("Role deletion is not supported. Use RoleTypes instead.");
    }
}
