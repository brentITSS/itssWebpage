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
        return roles.Select(MapToRoleResponseDto).ToList();
    }

    public async Task<RoleResponseDto?> GetRoleByIdAsync(int roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null) return null;

        return MapToRoleResponseDto(role);
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
        var roleType = await _roleRepository.GetRoleTypeByIdAsync(request.RoleTypeId);
        if (roleType == null)
            throw new InvalidOperationException("Invalid RoleTypeId");

        var role = new Role
        {
            RoleName = request.RoleName,
            RoleTypeId = request.RoleTypeId,
            CreatedDate = DateTime.UtcNow
        };

        role = await _roleRepository.CreateAsync(role);
        role = await _roleRepository.GetByIdAsync(role.RoleId);

        return MapToRoleResponseDto(role!);
    }

    public async Task<RoleResponseDto?> UpdateRoleAsync(int roleId, UpdateRoleRequest request)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null) return null;

        if (request.RoleName != null) role.RoleName = request.RoleName;
        if (request.RoleTypeId.HasValue)
        {
            var roleType = await _roleRepository.GetRoleTypeByIdAsync(request.RoleTypeId.Value);
            if (roleType == null)
                throw new InvalidOperationException("Invalid RoleTypeId");
            role.RoleTypeId = request.RoleTypeId.Value;
        }

        role.ModifiedDate = DateTime.UtcNow;
        role = await _roleRepository.UpdateAsync(role);
        role = await _roleRepository.GetByIdAsync(roleId);

        return MapToRoleResponseDto(role!);
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        return await _roleRepository.DeleteAsync(roleId);
    }

    private RoleResponseDto MapToRoleResponseDto(Role role)
    {
        return new RoleResponseDto
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            RoleTypeId = role.RoleTypeId,
            RoleTypeName = role.RoleType.RoleTypeName,
            CreatedDate = role.CreatedDate
        };
    }
}
