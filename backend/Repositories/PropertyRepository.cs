using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace backend.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly ApplicationDbContext _context;

    public PropertyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PropertyGroup>> GetAllPropertyGroupsAsync()
    {
        return await _context.PropertyGroups
            .Include(pg => pg.Properties)
            .ToListAsync();
    }

    public async Task<PropertyGroup?> GetPropertyGroupByIdAsync(int propertyGroupId)
    {
        return await _context.PropertyGroups
            .Include(pg => pg.Properties)
            .FirstOrDefaultAsync(pg => pg.PropertyGroupId == propertyGroupId);
    }

    public async Task<PropertyGroup> CreatePropertyGroupAsync(PropertyGroup propertyGroup)
    {
        _context.PropertyGroups.Add(propertyGroup);
        await _context.SaveChangesAsync();
        return propertyGroup;
    }

    public async Task<PropertyGroup> UpdatePropertyGroupAsync(PropertyGroup propertyGroup)
    {
        _context.PropertyGroups.Update(propertyGroup);
        await _context.SaveChangesAsync();
        return propertyGroup;
    }

    public async Task<bool> DeletePropertyGroupAsync(int propertyGroupId)
    {
        var propertyGroup = await _context.PropertyGroups.FindAsync(propertyGroupId);
        if (propertyGroup == null) return false;

        _context.PropertyGroups.Remove(propertyGroup);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Property>> GetAllPropertiesAsync()
    {
        return await _context.Properties
            .Include(p => p.PropertyGroup)
            .ToListAsync();
    }

    public async Task<List<Property>> GetPropertiesByGroupAsync(int propertyGroupId)
    {
        return await _context.Properties
            .Include(p => p.PropertyGroup)
            .Where(p => p.PropertyGroupId == propertyGroupId)
            .ToListAsync();
    }

    public async Task<Property?> GetPropertyByIdAsync(int propertyId)
    {
        return await _context.Properties
            .Include(p => p.PropertyGroup)
            .FirstOrDefaultAsync(p => p.PropertyId == propertyId);
    }

    public async Task<Property> CreatePropertyAsync(Property property)
    {
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();
        return property;
    }

    public async Task<Property> UpdatePropertyAsync(Property property)
    {
        _context.Properties.Update(property);
        await _context.SaveChangesAsync();
        return property;
    }

    public async Task<bool> DeletePropertyAsync(int propertyId)
    {
        var property = await _context.Properties.FindAsync(propertyId);
        if (property == null) return false;

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();
        return true;
    }

    // Property Group User access methods
    public async Task<List<int>> GetUserPropertyGroupIdsAsync(int userId)
    {
        return await _context.PropertyGroupUsers
            .Where(pgu => pgu.UserId == userId && pgu.Active)
            .Select(pgu => pgu.PropertyGroupId)
            .ToListAsync();
    }

    public async Task<PropertyGroupUser?> GetPropertyGroupUserAsync(int userId, int propertyGroupId)
    {
        return await _context.PropertyGroupUsers
            .FirstOrDefaultAsync(pgu => pgu.UserId == userId && pgu.PropertyGroupId == propertyGroupId);
    }

    public async Task<PropertyGroupUser> AddPropertyGroupUserAsync(PropertyGroupUser propertyGroupUser)
    {
        // Check if already exists
        var existing = await GetPropertyGroupUserAsync(propertyGroupUser.UserId, propertyGroupUser.PropertyGroupId);
        if (existing != null)
        {
            // Reactivate if inactive
            existing.Active = true;
            _context.PropertyGroupUsers.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        _context.PropertyGroupUsers.Add(propertyGroupUser);
        await _context.SaveChangesAsync();
        return propertyGroupUser;
    }

    public async Task<bool> RemovePropertyGroupUserAsync(int userId, int propertyGroupId)
    {
        var propertyGroupUser = await GetPropertyGroupUserAsync(userId, propertyGroupId);
        if (propertyGroupUser == null) return false;

        // Soft delete by setting active to false
        propertyGroupUser.Active = false;
        _context.PropertyGroupUsers.Update(propertyGroupUser);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PropertyGroupUser>> GetPropertyGroupUsersByGroupAsync(int propertyGroupId)
    {
        return await _context.PropertyGroupUsers
            .Include(pgu => pgu.User)
            .Where(pgu => pgu.PropertyGroupId == propertyGroupId && pgu.Active)
            .ToListAsync();
    }
}
