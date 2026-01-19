# Azure SQL Database - Managed Identity Setup Guide

## Overview

Your App Service (`itsson-api`) uses Azure AD authentication with Managed Identity. The Managed Identity needs to be granted access to your SQL Database (`PropertyHub`).

## Required Database Roles

For your application, assign these **two database-level roles** to the Managed Identity:

✅ **`db_datareader`** - Read data from all tables (SELECT)  
✅ **`db_datawriter`** - Insert, update, and delete data in all tables (INSERT, UPDATE, DELETE)

**That's it!** Your application only performs CRUD operations (no schema changes/migrations), so you only need these two roles.

## Step-by-Step Setup

### Step 1: Get Your App Service Managed Identity Object ID

1. Go to Azure Portal → **itsson-api** App Service
2. Click **Identity** in the left menu
3. Under **System assigned**, ensure **Status** is **On**
4. Copy the **Object (principal) ID** (e.g., `a1b2c3d4-e5f6-7890-abcd-ef1234567890`)
5. Also note the **Principal name** (e.g., `itsson-api`)

### Step 2: Connect to SQL Database as Azure AD Admin

You need to connect to your SQL Database using an Azure AD administrator account.

**Option A: Using Azure Portal Query Editor**
1. Go to Azure Portal → Your SQL Database (`PropertyHub`)
2. Click **Query editor** in the left menu
3. Sign in with your Azure AD account (must be SQL Server admin)

**Option B: Using SQL Server Management Studio (SSMS)**
1. Open SSMS
2. Connect to: `itss.database.windows.net`
3. Select **Authentication:** `Azure Active Directory - Universal with MFA`
4. Sign in with your Azure AD admin account

**Option C: Using Azure Data Studio**
1. Open Azure Data Studio
2. Create new connection to `itss.database.windows.net`
3. Authentication: **Azure Active Directory**
4. Sign in with your Azure AD admin account

### Step 3: Create Database User and Grant Roles

Once connected to the `PropertyHub` database, run these SQL commands:

```sql
-- Replace 'itsson-api' with your App Service name
-- Replace the Object ID with your actual Managed Identity Object ID from Step 1

-- Create the user from the Managed Identity
CREATE USER [itsson-api] FROM EXTERNAL PROVIDER;

-- Grant read access to all tables
ALTER ROLE db_datareader ADD MEMBER [itsson-api];

-- Grant write access to all tables
ALTER ROLE db_datawriter ADD MEMBER [itsson-api];
```

**Important Notes:**
- The user name `[itsson-api]` should match your App Service name
- If the user already exists, skip the `CREATE USER` line
- You can verify the user was created with: `SELECT * FROM sys.database_principals WHERE name = 'itsson-api'`

### Step 4: Verify the Setup

Run this query to verify the roles were assigned:

```sql
-- Check assigned roles
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

### Step 5: Test the Connection

1. Go back to Azure Portal → **itsson-api** → **Log stream**
2. Restart the App Service (Configuration → Save again, or use Restart button)
3. Check the logs for successful database connection
4. Test your API endpoint: `POST https://itsson-api.azurewebsites.net/api/auth/login`

## Alternative: Using Object ID Instead of App Service Name

If the App Service name doesn't work, you can use the Object (principal) ID directly:

```sql
-- Create user using Object ID
CREATE USER [a1b2c3d4-e5f6-7890-abcd-ef1234567890] FROM EXTERNAL PROVIDER;

-- Grant roles
ALTER ROLE db_datareader ADD MEMBER [a1b2c3d4-e5f6-7890-abcd-ef1234567890];
ALTER ROLE db_datawriter ADD MEMBER [a1b2c3d4-e5f6-7890-abcd-ef1234567890];
```

## Troubleshooting

### Error: "Principal 'itsson-api' could not be found"
- Verify the Managed Identity is enabled (Status = On)
- Try using the Object (principal) ID instead of the App Service name
- Ensure you're connected as an Azure AD admin

### Error: "Cannot find the user 'itsson-api'"
- The user might already exist - try just the `ALTER ROLE` commands
- Verify the App Service name matches exactly (case-sensitive)

### Error: "Cannot alter the role 'db_datareader'"
- Ensure you're connected to the correct database (`PropertyHub`)
- Verify you're signed in as an Azure AD administrator

### Connection still fails after setup
- Check App Service Log Stream for detailed error messages
- Verify the connection string in App Service Configuration uses: `Authentication="Active Directory Default";`
- Ensure the App Service and SQL Database are in the same Azure AD tenant

## Summary

**Required Roles (both are needed):**
- ✅ `db_datareader` (read access - SELECT)
- ✅ `db_datawriter` (write access - INSERT, UPDATE, DELETE)

That's all you need! Your application doesn't modify database schema, so no `db_ddladmin` role is required.
