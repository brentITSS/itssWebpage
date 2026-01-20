using backend.DTOs;

namespace backend.Services;

public interface IPropertyService
{
    Task<List<PropertyGroupResponseDto>> GetAllPropertyGroupsAsync();
    Task<List<PropertyGroupResponseDto>> GetAllPropertyGroupsForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin);
    Task<PropertyGroupResponseDto?> GetPropertyGroupByIdAsync(int propertyGroupId);
    Task<PropertyGroupResponseDto> CreatePropertyGroupAsync(CreatePropertyGroupRequest request, int createdByUserId);
    Task<PropertyGroupResponseDto?> UpdatePropertyGroupAsync(int propertyGroupId, UpdatePropertyGroupRequest request, int modifiedByUserId);
    Task<bool> DeletePropertyGroupAsync(int propertyGroupId, int deletedByUserId);
    
    Task<List<PropertyResponseDto>> GetAllPropertiesAsync();
    Task<List<PropertyResponseDto>> GetAllPropertiesForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin);
    Task<List<PropertyResponseDto>> GetPropertiesByGroupAsync(int propertyGroupId);
    Task<PropertyResponseDto?> GetPropertyByIdAsync(int propertyId);
    Task<PropertyResponseDto> CreatePropertyAsync(CreatePropertyRequest request, int createdByUserId);
    Task<PropertyResponseDto?> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request, int modifiedByUserId);
    Task<bool> DeletePropertyAsync(int propertyId, int deletedByUserId);
}
