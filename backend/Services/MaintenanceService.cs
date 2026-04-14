using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IPropertyRepository _propertyRepository;

    public MaintenanceService(IMaintenanceRepository maintenanceRepository, IPropertyRepository propertyRepository)
    {
        _maintenanceRepository = maintenanceRepository;
        _propertyRepository = propertyRepository;
    }

    private static bool CanAccessMaintenance(Maintenance m, List<int> userGroupIds, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        if (isGlobalAdmin || isPropertyHubAdmin) return true;
        if (userGroupIds.Count == 0) return true;
        return userGroupIds.Contains(m.PropertyGroupId);
    }

    public async Task<List<MaintenanceTypeDto>> GetAllMaintenanceTypesAsync()
    {
        var types = await _maintenanceRepository.GetAllMaintenanceTypesAsync();
        return types.Select(MapTypeToDto).ToList();
    }

    public async Task<MaintenanceTypeDto?> GetMaintenanceTypeByIdAsync(int id)
    {
        var entity = await _maintenanceRepository.GetMaintenanceTypeByIdAsync(id);
        return entity == null ? null : MapTypeToDto(entity);
    }

    public async Task<MaintenanceTypeDto> CreateMaintenanceTypeAsync(CreateMaintenanceTypeRequest request)
    {
        var entity = new MaintenanceType
        {
            MaintenanceTypeName = request.MaintenanceTypeName,
            Description = request.Description,
            IsActive = request.IsActive ?? true,
            CreatedDate = DateTime.UtcNow,
        };
        entity = await _maintenanceRepository.CreateMaintenanceTypeAsync(entity);
        return MapTypeToDto(entity);
    }

    public async Task<MaintenanceTypeDto?> UpdateMaintenanceTypeAsync(int id, UpdateMaintenanceTypeRequest request)
    {
        var entity = await _maintenanceRepository.GetMaintenanceTypeByIdAsync(id);
        if (entity == null) return null;

        if (request.MaintenanceTypeName != null) entity.MaintenanceTypeName = request.MaintenanceTypeName;
        if (request.Description != null) entity.Description = request.Description;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive;

        entity = await _maintenanceRepository.UpdateMaintenanceTypeAsync(entity);
        return MapTypeToDto(entity);
    }

    public async Task<bool> IsMaintenanceTypeInUseAsync(int id)
    {
        return await _maintenanceRepository.CountMaintenancesByTypeAsync(id) > 0;
    }

    public async Task<bool> DeleteMaintenanceTypeAsync(int id)
    {
        return await _maintenanceRepository.DeleteMaintenanceTypeAsync(id);
    }

    public async Task<List<MaintenanceStatusDto>> GetAllMaintenanceStatusesAsync()
    {
        var list = await _maintenanceRepository.GetAllMaintenanceStatusesAsync();
        return list.Select(MapStatusToDto).ToList();
    }

    public async Task<MaintenanceStatusDto?> GetMaintenanceStatusByIdAsync(int id)
    {
        var e = await _maintenanceRepository.GetMaintenanceStatusByIdAsync(id);
        return e == null ? null : MapStatusToDto(e);
    }

    public async Task<MaintenanceStatusDto> CreateMaintenanceStatusAsync(CreateMaintenanceStatusRequest request)
    {
        var entity = new MaintenanceStatus
        {
            MaintenanceStatusName = request.MaintenanceStatusName,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 0,
            IsActive = request.IsActive ?? true,
            CreatedDate = DateTime.UtcNow,
        };
        entity = await _maintenanceRepository.CreateMaintenanceStatusAsync(entity);
        return MapStatusToDto(entity);
    }

    public async Task<MaintenanceStatusDto?> UpdateMaintenanceStatusAsync(int id, UpdateMaintenanceStatusRequest request)
    {
        var entity = await _maintenanceRepository.GetMaintenanceStatusByIdAsync(id);
        if (entity == null) return null;

        if (request.MaintenanceStatusName != null) entity.MaintenanceStatusName = request.MaintenanceStatusName;
        if (request.Description != null) entity.Description = request.Description;
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive;

        entity = await _maintenanceRepository.UpdateMaintenanceStatusAsync(entity);
        return MapStatusToDto(entity);
    }

    public async Task<bool> IsMaintenanceStatusInUseAsync(int id)
    {
        return await _maintenanceRepository.CountMaintenancesByStatusAsync(id) > 0;
    }

    public async Task<bool> DeleteMaintenanceStatusAsync(int id)
    {
        return await _maintenanceRepository.DeleteMaintenanceStatusAsync(id);
    }

    public async Task<List<MaintenanceResponseDto>> GetAllMaintenancesForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        var all = await _maintenanceRepository.GetAllAsync();
        var userGroupIds = await _propertyRepository.GetUserPropertyGroupIdsAsync(userId);

        return all
            .Where(m => CanAccessMaintenance(m, userGroupIds, isGlobalAdmin, isPropertyHubAdmin))
            .Select(MapToDto)
            .ToList();
    }

    public async Task<MaintenanceResponseDto?> GetMaintenanceByIdForUserAsync(int id, int userId, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        var m = await _maintenanceRepository.GetByIdAsync(id);
        if (m == null) return null;

        var userGroupIds = await _propertyRepository.GetUserPropertyGroupIdsAsync(userId);
        if (!CanAccessMaintenance(m, userGroupIds, isGlobalAdmin, isPropertyHubAdmin))
            return null;

        return MapToDto(m);
    }

    public async Task<MaintenanceResponseDto> CreateMaintenanceAsync(CreateMaintenanceRequest request)
    {
        var propertyId = NormalizeOptionalPropertyId(request.PropertyId);
        await EnsurePropertyScopeIsValidAsync(propertyId, request.PropertyGroupId);

        var entity = new Maintenance
        {
            PropertyGroupId = request.PropertyGroupId,
            PropertyId = propertyId,
            MaintenanceTypeId = request.MaintenanceTypeId,
            MaintenanceStatusId = request.MaintenanceStatusId,
            Summary = request.Summary,
            DetailNotes = request.DetailNotes,
            WorkDate = request.WorkDate,
            CreatedDate = DateTime.UtcNow,
        };

        entity = await _maintenanceRepository.CreateAsync(entity);
        var loaded = await _maintenanceRepository.GetByIdAsync(entity.MaintenanceId);
        return MapToDto(loaded!);
    }

    public async Task<MaintenanceResponseDto?> UpdateMaintenanceAsync(int id, UpdateMaintenanceRequest request)
    {
        var entity = await _maintenanceRepository.GetByIdAsync(id);
        if (entity == null) return null;

        var propId = request.PropertyId.HasValue
            ? NormalizeOptionalPropertyId(request.PropertyId)
            : entity.PropertyId;
        var grpId = request.PropertyGroupId ?? entity.PropertyGroupId;
        await EnsurePropertyScopeIsValidAsync(propId, grpId);

        if (request.PropertyGroupId.HasValue) entity.PropertyGroupId = request.PropertyGroupId.Value;
        if (request.PropertyId.HasValue) entity.PropertyId = NormalizeOptionalPropertyId(request.PropertyId);
        if (request.MaintenanceTypeId.HasValue) entity.MaintenanceTypeId = request.MaintenanceTypeId.Value;
        entity.MaintenanceStatusId = request.MaintenanceStatusId;
        if (request.Summary != null) entity.Summary = request.Summary;
        if (request.DetailNotes != null) entity.DetailNotes = request.DetailNotes;
        if (request.WorkDate.HasValue) entity.WorkDate = request.WorkDate;

        await _maintenanceRepository.UpdateAsync(entity);
        var loaded = await _maintenanceRepository.GetByIdAsync(id);
        return MapToDto(loaded!);
    }

    public async Task<bool> DeleteMaintenanceAsync(int id)
    {
        return await _maintenanceRepository.DeleteAsync(id);
    }

    private async Task EnsurePropertyScopeIsValidAsync(int? propertyId, int propertyGroupId)
    {
        var group = await _propertyRepository.GetPropertyGroupByIdAsync(propertyGroupId);
        if (group == null)
            throw new InvalidOperationException("Property group not found.");

        if (!propertyId.HasValue)
            return;

        var prop = await _propertyRepository.GetPropertyByIdAsync(propertyId.Value);
        if (prop == null)
            throw new InvalidOperationException("Property not found.");
        if (prop.PropertyGroupId != propertyGroupId)
            throw new InvalidOperationException("Selected property does not belong to the selected property group.");
    }

    private static int? NormalizeOptionalPropertyId(int? propertyId)
    {
        if (!propertyId.HasValue || propertyId.Value <= 0)
            return null;
        return propertyId.Value;
    }

    private static MaintenanceTypeDto MapTypeToDto(MaintenanceType t)
    {
        return new MaintenanceTypeDto
        {
            MaintenanceTypeId = t.MaintenanceTypeId,
            MaintenanceTypeName = t.MaintenanceTypeName,
            Description = t.Description,
            IsActive = t.IsActive,
            CreatedDate = t.CreatedDate,
        };
    }

    private static MaintenanceStatusDto MapStatusToDto(MaintenanceStatus s)
    {
        return new MaintenanceStatusDto
        {
            MaintenanceStatusId = s.MaintenanceStatusId,
            MaintenanceStatusName = s.MaintenanceStatusName,
            Description = s.Description,
            SortOrder = s.SortOrder,
            IsActive = s.IsActive,
            CreatedDate = s.CreatedDate,
        };
    }

    private static MaintenanceResponseDto MapToDto(Maintenance m)
    {
        return new MaintenanceResponseDto
        {
            MaintenanceId = m.MaintenanceId,
            PropertyGroupId = m.PropertyGroupId,
            PropertyGroupName = m.PropertyGroup?.PropertyGroupName,
            PropertyId = m.PropertyId,
            PropertyName = m.Property?.PropertyName,
            MaintenanceTypeId = m.MaintenanceTypeId,
            MaintenanceTypeName = m.MaintenanceType?.MaintenanceTypeName ?? string.Empty,
            MaintenanceStatusId = m.MaintenanceStatusId,
            MaintenanceStatusName = m.MaintenanceStatus?.MaintenanceStatusName,
            Summary = m.Summary,
            DetailNotes = m.DetailNotes,
            WorkDate = m.WorkDate,
            CreatedDate = m.CreatedDate,
        };
    }
}
