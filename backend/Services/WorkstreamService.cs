using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class WorkstreamService : IWorkstreamService
{
    private readonly IWorkstreamRepository _workstreamRepository;
    private readonly IUserRepository _userRepository;

    public WorkstreamService(IWorkstreamRepository workstreamRepository, IUserRepository userRepository)
    {
        _workstreamRepository = workstreamRepository;
        _userRepository = userRepository;
    }

    public async Task<List<WorkstreamResponseDto>> GetAllWorkstreamsAsync()
    {
        var workstreams = await _workstreamRepository.GetAllAsync();
        return workstreams.Select(MapToWorkstreamResponseDto).ToList();
    }

    public async Task<WorkstreamResponseDto?> GetWorkstreamByIdAsync(int workstreamId)
    {
        var workstream = await _workstreamRepository.GetByIdAsync(workstreamId);
        if (workstream == null) return null;

        return MapToWorkstreamResponseDto(workstream);
    }

    public async Task<List<PermissionTypeDto>> GetAllPermissionTypesAsync()
    {
        var permissionTypes = await _workstreamRepository.GetAllPermissionTypesAsync();
        return permissionTypes.Select(pt => new PermissionTypeDto
        {
            PermissionTypeId = pt.PermissionTypeId,
            PermissionTypeName = pt.PermissionTypeName,
            Description = pt.Description
        }).ToList();
    }

    public async Task<WorkstreamResponseDto> CreateWorkstreamAsync(CreateWorkstreamRequest request)
    {
        var workstream = new Workstream
        {
            WorkstreamName = request.WorkstreamName,
            Description = request.Description,
            IsActive = true,
        };

        workstream = await _workstreamRepository.CreateAsync(workstream);
        return MapToWorkstreamResponseDto(workstream);
    }

    public async Task<WorkstreamResponseDto?> UpdateWorkstreamAsync(int workstreamId, UpdateWorkstreamRequest request)
    {
        var workstream = await _workstreamRepository.GetByIdAsync(workstreamId);
        if (workstream == null) return null;

        if (request.WorkstreamName != null) workstream.WorkstreamName = request.WorkstreamName;
        if (request.Description != null) workstream.Description = request.Description;
        if (request.IsActive.HasValue) workstream.IsActive = request.IsActive.Value;

        workstream = await _workstreamRepository.UpdateAsync(workstream);
        return MapToWorkstreamResponseDto(workstream);
    }

    public async Task<bool> DeleteWorkstreamAsync(int workstreamId)
    {
        return await _workstreamRepository.DeleteAsync(workstreamId);
    }

    public async Task<List<UserResponseDto>> GetWorkstreamUsersAsync(int workstreamId)
    {
        var workstreamUsers = await _workstreamRepository.GetWorkstreamUsersAsync(workstreamId);
        var userIds = workstreamUsers.Select(wu => wu.UserId).ToList();

        var users = await _userRepository.GetAllAsync();
        return users
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new UserResponseDto
            {
                UserId = u.UserId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                CreatedDate = DateTime.UtcNow, // Database doesn't have CreatedDate column, using current time as placeholder
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    RoleId = ur.RoleTypeId,
                    RoleName = ur.RoleType?.RoleTypeName ?? "",
                    RoleTypeName = ur.RoleType?.RoleTypeName ?? ""
                }).ToList(),
                WorkstreamAccess = u.WorkstreamUsers
                    .Where(wu => wu.WorkstreamId == workstreamId)
                    .Select(wu => new WorkstreamAccessDto
                    {
                        WorkstreamId = wu.Workstream.WorkstreamId,
                        WorkstreamName = wu.Workstream.WorkstreamName,
                        PermissionTypeId = wu.PermissionType.PermissionTypeId,
                        PermissionTypeName = wu.PermissionType.PermissionTypeName
                    }).ToList()
            }).ToList();
    }

    public async Task AssignUserToWorkstreamAsync(int workstreamId, AssignWorkstreamUserRequest request)
    {
        var workstream = await _workstreamRepository.GetByIdAsync(workstreamId);
        if (workstream == null)
            throw new InvalidOperationException("Workstream not found");

        var permissionType = await _workstreamRepository.GetPermissionTypeByIdAsync(request.PermissionTypeId);
        if (permissionType == null)
            throw new InvalidOperationException("PermissionType not found");

        var workstreamUser = new WorkstreamUser
        {
            UserId = request.UserId,
            WorkstreamId = workstreamId,
            PermissionTypeId = request.PermissionTypeId,
        };

        await _workstreamRepository.AddWorkstreamUserAsync(workstreamUser);
    }

    public async Task RemoveUserFromWorkstreamAsync(int workstreamId, int userId)
    {
        await _workstreamRepository.RemoveWorkstreamUserAsync(workstreamId, userId);
    }

    private WorkstreamResponseDto MapToWorkstreamResponseDto(Workstream workstream)
    {
        return new WorkstreamResponseDto
        {
            WorkstreamId = workstream.WorkstreamId,
            WorkstreamName = workstream.WorkstreamName,
            Description = workstream.Description,
            IsActive = workstream.IsActive,
            CreatedDate = DateTime.UtcNow // Database doesn't have CreatedDate column, using current time as placeholder
        };
    }
}
