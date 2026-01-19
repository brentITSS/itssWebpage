using System.Linq;
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
            PropertyGroupName = pg.PropertyGroupName ?? string.Empty,
            Description = null,
            CreatedDate = DateTime.UtcNow,
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
            PropertyGroupName = propertyGroup.PropertyGroupName ?? string.Empty,
            Description = null,
            CreatedDate = DateTime.UtcNow,
            PropertyCount = propertyGroup.Properties.Count
        };
    }

    public async Task<PropertyGroupResponseDto> CreatePropertyGroupAsync(CreatePropertyGroupRequest request, int createdByUserId)
    {
        var propertyGroup = new PropertyGroup
        {
            PropertyGroupName = request.PropertyGroupName,
            IsActive = true
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
            PropertyGroupName = propertyGroup.PropertyGroupName ?? string.Empty,
            Description = null,
            CreatedDate = DateTime.UtcNow,
            PropertyCount = 0
        };
    }

    public async Task<PropertyGroupResponseDto?> UpdatePropertyGroupAsync(int propertyGroupId, UpdatePropertyGroupRequest request, int modifiedByUserId)
    {
        var propertyGroup = await _propertyRepository.GetPropertyGroupByIdAsync(propertyGroupId);
        if (propertyGroup == null) return null;

        var oldValues = $"Name: {propertyGroup.PropertyGroupName}";

        if (request.PropertyGroupName != null) propertyGroup.PropertyGroupName = request.PropertyGroupName;

        propertyGroup = await _propertyRepository.UpdatePropertyGroupAsync(propertyGroup);

        // Audit log
        var newValues = $"Name: {propertyGroup.PropertyGroupName}";
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
            PropertyGroupName = propertyGroup.PropertyGroupName ?? string.Empty,
            Description = null,
            CreatedDate = DateTime.UtcNow,
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
        // Parse address if provided (split by comma or newline)
        string? address1 = null;
        string? address2 = null;
        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            var addressParts = request.Address.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (addressParts.Length > 0) address1 = addressParts[0];
            if (addressParts.Length > 1) address2 = string.Join(", ", addressParts.Skip(1));
        }

        var property = new Property
        {
            PropertyGroupId = request.PropertyGroupId,
            PropertyName = request.PropertyName,
            Address1 = address1,
            Address2 = address2,
            PostCode = request.PostCode,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdByUserId.ToString(),
            IsActive = true
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
        if (request.PostCode != null) property.PostCode = request.PostCode;
        
        // Parse address if provided
        if (request.Address != null)
        {
            var addressParts = request.Address.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (addressParts.Length > 0) property.Address1 = addressParts[0];
            if (addressParts.Length > 1) property.Address2 = string.Join(", ", addressParts.Skip(1));
        }

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
            PropertyGroupId = property.PropertyGroupId ?? 0,
            PropertyGroupName = property.PropertyGroup?.PropertyGroupName ?? string.Empty,
            PropertyName = property.PropertyName ?? string.Empty,
            Address = property.Address,
            PostCode = property.PostCode,
            CreatedDate = property.CreatedDate ?? DateTime.UtcNow
        };
    }
}
