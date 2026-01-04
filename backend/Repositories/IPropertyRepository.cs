using backend.Models;

namespace backend.Repositories;

public interface IPropertyRepository
{
    Task<List<PropertyGroup>> GetAllPropertyGroupsAsync();
    Task<PropertyGroup?> GetPropertyGroupByIdAsync(int propertyGroupId);
    Task<PropertyGroup> CreatePropertyGroupAsync(PropertyGroup propertyGroup);
    Task<PropertyGroup> UpdatePropertyGroupAsync(PropertyGroup propertyGroup);
    Task<bool> DeletePropertyGroupAsync(int propertyGroupId);
    
    Task<List<Property>> GetAllPropertiesAsync();
    Task<List<Property>> GetPropertiesByGroupAsync(int propertyGroupId);
    Task<Property?> GetPropertyByIdAsync(int propertyId);
    Task<Property> CreatePropertyAsync(Property property);
    Task<Property> UpdatePropertyAsync(Property property);
    Task<bool> DeletePropertyAsync(int propertyId);
}
