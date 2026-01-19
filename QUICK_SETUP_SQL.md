# Quick Setup: SQL Database Access for Managed Identity

## TL;DR - Run These SQL Commands

Connect to your `PropertyHub` database as an Azure AD admin, then run:

```sql
-- Create the user from your App Service Managed Identity
CREATE USER [itsson-api] FROM EXTERNAL PROVIDER;

-- Grant read access
ALTER ROLE db_datareader ADD MEMBER [itsson-api];

-- Grant write access
ALTER ROLE db_datawriter ADD MEMBER [itsson-api];
```

## How to Connect

**Option 1: Azure Portal Query Editor**
1. Azure Portal → Your SQL Database (`PropertyHub`)
2. Click **Query editor** (left menu)
3. Sign in with your Azure AD account

**Option 2: SQL Server Management Studio (SSMS)**
1. Connect to: `itss.database.windows.net`
2. Authentication: **Azure Active Directory - Universal with MFA**
3. Sign in with your Azure AD admin account

## Required Roles Explained

- **`db_datareader`** = SELECT permissions (read data)
- **`db_datawriter`** = INSERT, UPDATE, DELETE permissions (modify data)

These two roles are all your application needs!

## Verify It Worked

Run this to check:
```sql
SELECT 
    dp.name AS UserName,
    r.name AS RoleName
FROM sys.database_role_members rm
JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
JOIN sys.database_principals dp ON rm.member_principal_id = dp.principal_id
WHERE dp.name = 'itsson-api';
```

You should see:
- `db_datareader`
- `db_datawriter`
