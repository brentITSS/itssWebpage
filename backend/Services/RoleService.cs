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
        // Since tblRole doesn't exist, return RoleTypes as roles
        var roleTypes = await _roleRepository.GetAllRoleTypesAsync();
        return roleTypes.Select(rt => new RoleResponseDto
        {
            RoleId = rt.RoleTypeId, // Use RoleTypeId as RoleId
            RoleName = rt.RoleTypeName, // Use RoleTypeName as RoleName
            RoleTypeId = rt.RoleTypeId,
            RoleTypeName = rt.RoleTypeName,
            CreatedDate = DateTime.UtcNow // RoleType doesn't have CreatedDate, use current date
        }).ToList();
    }

    public async Task<RoleResponseDto?> GetRoleByIdAsync(int roleId)
    {
        // Since tblRole doesn't exist, treat roleId as roleTypeId
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
