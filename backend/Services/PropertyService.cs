using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class PropertyService : IPropertyService
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public PropertyService(IPropertyRepository propertyRepository, IAuditLogRepository auditLogRepository)
    {
        _propertyRepository = propertyRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<PropertyGroupResponseDto>> GetAllPropertyGroupsAsync()
    {
        var propertyGroups = await _propertyRepository.GetAllPropertyGroupsAsync();
        return propertyGroups.Select(pg => new PropertyGroupResponseDto
        {
            PropertyGroupId = pg.PropertyGroupId,
            PropertyGroupName = pg.PropertyGroupName,
            Description = pg.Description,
            CreatedDate = pg.CreatedDate,
            PropertyCount = pg.Properties.Count
        }).ToList();
    }

    public async Task<PropertyGroupResponseDto?> GetPropertyGroupByIdAsync(int propertyGroupId)
    {
        var propertyGroup = await _propertyRepository.GetPropertyGroupByIdAsync(propertyGroupId);
        if (propertyGroup == null) return null;

        return new PropertyGroupResponseDto
        {
            PropertyGroupId = propertyGroup.PropertyGroupId,
            PropertyGroupName = propertyGroup.PropertyGroupName,
            Description = propertyGroup.Description,
            CreatedDate = propertyGroup.CreatedDate,
            PropertyCount = propertyGroup.Properties.Count
        };
    }

    public async Task<PropertyGroupResponseDto> CreatePropertyGroupAsync(CreatePropertyGroupRequest request, int createdByUserId)
    {
        var propertyGroup = new PropertyGroup
        {
            PropertyGroupName = request.PropertyGroupName,
            Description = request.Description,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        propertyGroup = await _propertyRepository.CreatePropertyGroupAsync(propertyGroup);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = createdByUserId,
            Action = "Create",
            EntityType = "PropertyGroup",
            EntityId = propertyGroup.PropertyGroupId,
            NewValues = $"Name: {propertyGroup.PropertyGroupName}",
            CreatedDate = DateTime.UtcNow
        });

        return new PropertyGroupResponseDto
        {
            PropertyGroupId = propertyGroup.PropertyGroupId,
            PropertyGroupName = propertyGroup.PropertyGroupName,
            Description = propertyGroup.Description,
            CreatedDate = propertyGroup.CreatedDate,
            PropertyCount = 0
        };
    }

    public async Task<PropertyGroupResponseDto?> UpdatePropertyGroupAsync(int propertyGroupId, UpdatePropertyGroupRequest request, int modifiedByUserId)
    {
        var propertyGroup = await _propertyRepository.GetPropertyGroupByIdAsync(propertyGroupId);
        if (propertyGroup == null) return null;

        var oldValues = $"Name: {propertyGroup.PropertyGroupName}, Description: {propertyGroup.Description}";

        if (request.PropertyGroupName != null) propertyGroup.PropertyGroupName = request.PropertyGroupName;
        if (request.Description != null) propertyGroup.Description = request.Description;
        propertyGroup.ModifiedDate = DateTime.UtcNow;
        propertyGroup.ModifiedByUserId = modifiedByUserId;

        propertyGroup = await _propertyRepository.UpdatePropertyGroupAsync(propertyGroup);

        // Audit log
        var newValues = $"Name: {propertyGroup.PropertyGroupName}, Description: {propertyGroup.Description}";
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "Update",
            EntityType = "PropertyGroup",
            EntityId = propertyGroupId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedDate = DateTime.UtcNow
        });

        return new PropertyGroupResponseDto
        {
            PropertyGroupId = propertyGroup.PropertyGroupId,
            PropertyGroupName = propertyGroup.PropertyGroupName,
            Description = propertyGroup.Description,
            CreatedDate = propertyGroup.CreatedDate,
            PropertyCount = propertyGroup.Properties.Count
        };
    }

    public async Task<bool> DeletePropertyGroupAsync(int propertyGroupId, int deletedByUserId)
    {
        var propertyGroup = await _propertyRepository.GetPropertyGroupByIdAsync(propertyGroupId);
        if (propertyGroup == null) return false;

        var result = await _propertyRepository.DeletePropertyGroupAsync(propertyGroupId);

        if (result)
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = deletedByUserId,
                Action = "Delete",
                EntityType = "PropertyGroup",
                EntityId = propertyGroupId,
                OldValues = $"Name: {propertyGroup.PropertyGroupName}",
                CreatedDate = DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<List<PropertyResponseDto>> GetAllPropertiesAsync()
    {
        var properties = await _propertyRepository.GetAllPropertiesAsync();
        return properties.Select(MapToPropertyResponseDto).ToList();
    }

    public async Task<List<PropertyResponseDto>> GetPropertiesByGroupAsync(int propertyGroupId)
    {
        var properties = await _propertyRepository.GetPropertiesByGroupAsync(propertyGroupId);
        return properties.Select(MapToPropertyResponseDto).ToList();
    }

    public async Task<PropertyResponseDto?> GetPropertyByIdAsync(int propertyId)
    {
        var property = await _propertyRepository.GetPropertyByIdAsync(propertyId);
        if (property == null) return null;

        return MapToPropertyResponseDto(property);
    }

    public async Task<PropertyResponseDto> CreatePropertyAsync(CreatePropertyRequest request, int createdByUserId)
    {
        var property = new Property
        {
            PropertyGroupId = request.PropertyGroupId,
            PropertyName = request.PropertyName,
            Address = request.Address,
            PostCode = request.PostCode,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        property = await _propertyRepository.CreatePropertyAsync(property);
        property = await _propertyRepository.GetPropertyByIdAsync(property.PropertyId);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = createdByUserId,
            Action = "Create",
            EntityType = "Property",
            EntityId = property.PropertyId,
            NewValues = $"Name: {property.PropertyName}, Address: {property.Address}",
            CreatedDate = DateTime.UtcNow
        });

        return MapToPropertyResponseDto(property!);
    }

    public async Task<PropertyResponseDto?> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request, int modifiedByUserId)
    {
        var property = await _propertyRepository.GetPropertyByIdAsync(propertyId);
        if (property == null) return null;

        var oldValues = $"Name: {property.PropertyName}, Address: {property.Address}, PostCode: {property.PostCode}";

        if (request.PropertyGroupId.HasValue) property.PropertyGroupId = request.PropertyGroupId.Value;
        if (request.PropertyName != null) property.PropertyName = request.PropertyName;
        if (request.Address != null) property.Address = request.Address;
        if (request.PostCode != null) property.PostCode = request.PostCode;
        property.ModifiedDate = DateTime.UtcNow;
        property.ModifiedByUserId = modifiedByUserId;

        property = await _propertyRepository.UpdatePropertyAsync(property);
        property = await _propertyRepository.GetPropertyByIdAsync(propertyId);

        // Audit log
        var newValues = $"Name: {property!.PropertyName}, Address: {property.Address}, PostCode: {property.PostCode}";
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "Update",
            EntityType = "Property",
            EntityId = propertyId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedDate = DateTime.UtcNow
        });

        return MapToPropertyResponseDto(property);
    }

    public async Task<bool> DeletePropertyAsync(int propertyId, int deletedByUserId)
    {
        var property = await _propertyRepository.GetPropertyByIdAsync(propertyId);
        if (property == null) return false;

        var result = await _propertyRepository.DeletePropertyAsync(propertyId);

        if (result)
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = deletedByUserId,
                Action = "Delete",
                EntityType = "Property",
                EntityId = propertyId,
                OldValues = $"Name: {property.PropertyName}",
                CreatedDate = DateTime.UtcNow
            });
        }

        return result;
    }

    private PropertyResponseDto MapToPropertyResponseDto(Property property)
    {
        return new PropertyResponseDto
        {
            PropertyId = property.PropertyId,
            PropertyGroupId = property.PropertyGroupId,
            PropertyGroupName = property.PropertyGroup.PropertyGroupName,
            PropertyName = property.PropertyName,
            Address = property.Address,
            PostCode = property.PostCode,
            CreatedDate = property.CreatedDate
        };
    }
}
