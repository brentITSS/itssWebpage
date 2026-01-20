# Property Group Access Control Implementation Plan

## Overview
This document outlines the implementation of property group-level access control, allowing users to be assigned to specific property groups (e.g., "Keurboom", "Blue Mountain Bay") within the Property Hub workstream.

## Solution Approach
**Instead of creating separate workstreams** (which would require duplicating pages), we'll add **property group-level permissions** that filter what users can see within the same Property Hub interface.

## Architecture

### Database Changes
1. **New Table: `tblPropertyGroupUsers`**
   - Links users to specific property groups
   - Similar structure to `tblWorkstreamUsers`
   - Columns: `propertyGroupUserID`, `userID`, `propertyGrpID`, `active`

### Backend Changes

1. **New Model: `PropertyGroupUser`**
   - Maps to `tblPropertyGroupUsers` table
   - Navigation properties to `User` and `PropertyGroup`

2. **Repository Updates**
   - Add methods to `IPropertyRepository`:
     - `GetUserPropertyGroupIdsAsync(int userId)` - Get property group IDs user has access to
     - `AddPropertyGroupUserAsync(PropertyGroupUser)` - Assign user to property group
     - `RemovePropertyGroupUserAsync(int userId, int propertyGroupId)` - Remove access

3. **Service Updates**
   - `PropertyService.GetAllPropertyGroupsAsync()` - Filter by user's property group access
   - `PropertyService.GetAllPropertiesAsync()` - Filter properties by user's accessible property groups
   - Add methods to assign/remove users from property groups

4. **AuthService Updates**
   - Include `PropertyGroupAccess` in `UserDto` when mapping users
   - This allows frontend to know which property groups a user can access

5. **Controller Updates**
   - `PropertyGroupsController.GetAllPropertyGroups()` - Filter results based on current user
   - `PropertiesController.GetAllProperties()` - Filter results based on current user's property groups
   - Add endpoints to manage property group user assignments (for admins)

### Frontend Changes

1. **Permissions Page Enhancement**
   - Add a new section for "Property Group Access"
   - Allow admins to assign users to specific property groups
   - Similar UI to workstream permissions

2. **Property Hub Home Page**
   - No changes needed - it already filters based on what the API returns
   - Users will automatically only see property groups they have access to

## Access Control Logic

### Rules:
1. **Global Admins**: See all property groups (no filtering)
2. **Property Hub Admins**: See all property groups (no filtering) - can manage assignments
3. **Regular Users**: Only see property groups they're assigned to in `tblPropertyGroupUsers`
4. **Users without Property Hub workstream access**: Cannot access Property Hub at all (existing check)

### Fallback Behavior:
- If a user has Property Hub workstream access but no specific property group assignments, they see **all** property groups (backward compatible)
- This allows gradual migration - existing users continue to work, new users can be restricted

## Benefits
- ✅ Single Property Hub interface (no page duplication)
- ✅ Flexible - users can be assigned to any combination of property groups
- ✅ Scales easily as new property groups are added
- ✅ Backward compatible with existing users
- ✅ Consistent with existing workstream permission model

## Implementation Steps
1. Create database table (`CREATE_PROPERTY_GROUP_USERS_TABLE.sql`)
2. Create `PropertyGroupUser` model
3. Add to `ApplicationDbContext`
4. Add repository methods
5. Update `PropertyService` to filter by user access
6. Update `AuthService` to include property group access in user DTO
7. Update controllers to use filtered methods
8. Add admin endpoints for managing assignments
9. Update frontend Permissions page
10. Test with different user scenarios
