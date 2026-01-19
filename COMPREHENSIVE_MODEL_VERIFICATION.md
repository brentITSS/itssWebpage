# Comprehensive Database Model Verification

## Purpose
This document tracks the systematic verification and fixing of all database model mappings to ensure they match the Azure SQL Database schema exactly.

## SQL Query to Run First

**IMPORTANT:** Please run `COMPREHENSIVE_SCHEMA_CHECK.sql` in your Azure SQL Database and share the complete results. This will show us:
1. All tables that exist
2. All columns for each table
3. Data types and nullability

## Models to Verify

### ✅ Already Verified (Based on User's SQL Results)
- ✅ Tenant - Fixed (uses secondName, tenantEmail, mobile, tenancyID)
- ✅ Tenancy - Fixed (uses tenancyStartDate, tenancyEndDate, monthlyRentalCharge)
- ✅ TagType - Fixed (uses tagID, tagName, tagDescription)
- ✅ TagLog - Fixed (no EntityType/EntityId columns, uses direct FKs)
- ✅ JournalType - Fixed (uses journalType, not journalTypeName)
- ✅ JournalSubType - Fixed (uses subType, not journalSubTypeName)
- ✅ JournalLog - Fixed (no Amount/Description in DB, computed properties added)
- ✅ ContactLogType - Fixed (uses contactType, contactTypeDescription)
- ✅ ContactLog - Fixed (uses contactBy, contactNotes, no Subject column)
- ✅ ContactLogAttachment - Fixed (uses attachmentDescription, computed file props)
- ✅ PropertyGroup - Fixed (uses propertyGrpID, groupName)
- ✅ Property - Fixed (uses propertyGrpID, address fields)

### ⚠️ Needs Verification
- ⚠️ **JournalLogAttachment** - Error: "Invalid column name 'journalLogAttachmentID'"
  - Need to confirm actual column names
  - Current: journalLogAttachmentID, journalLogID
  - Possible alternatives: journalLogAttachmentId, journalLogAttachment_Id, etc.

### ❓ Need Full Schema Check
- ❓ User - Need to verify all columns
- ❓ RoleType - Need to verify all columns  
- ❓ UserRole - Need to verify all columns (especially primary key column name)
- ❓ Workstream - Need to verify all columns
- ❓ WorkstreamUser - Need to verify all columns
- ❓ PermissionType - Need to verify all columns
- ❓ AuditLog - Need to verify all columns

## Common Issues to Watch For

1. **Column Name Casing**: Database might use:
   - PascalCase: `PropertyId`
   - camelCase: `propertyId`  
   - lowercase: `propertyid`
   - Mixed with ID: `propertyID` or `PropertyID`

2. **Missing Columns**: Some models might reference columns that don't exist in DB

3. **Wrong Nullability**: Properties marked as required but DB allows NULL

4. **Navigation Properties**: Incorrect foreign key relationships

## Fixes Applied So Far

1. ✅ Removed non-existent columns (CreatedDate, ModifiedDate, etc. from most models)
2. ✅ Fixed column name casing to match database (camelCase with ID)
3. ✅ Added computed properties for backward compatibility where needed
4. ✅ Fixed relationships (Tenant->Tenancy, not Tenancy->Tenant)
5. ✅ Temporarily disabled JournalLogAttachment includes until schema confirmed

## Next Steps

1. **Run COMPREHENSIVE_SCHEMA_CHECK.sql** and share results
2. Fix all remaining column mappings
3. Re-enable JournalLogAttachment includes
4. Test all screens/buttons/links
5. Verify no remaining mapping errors
