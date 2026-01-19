# Next Step After Adding "Reader" Role

## What You Just Did
You assigned the Managed Identity a "Reader" role at the SQL Server level. This is good, but **it's not enough**.

## What You Need to Do Next

You need to grant **database-level permissions** inside the `PropertyHub` database itself.

### Step-by-Step:

1. **Connect to the PropertyHub database** as an Azure AD admin:
   - Azure Portal → **PropertyHub** database → **Query editor**
   - Or use SQL Server Management Studio (SSMS)

2. **Run these SQL commands** (make sure you're connected to the `PropertyHub` database, not the master database):

```sql
-- Create a user from your App Service Managed Identity
CREATE USER [itsson-api] FROM EXTERNAL PROVIDER;

-- Grant read access
ALTER ROLE db_datareader ADD MEMBER [itsson-api];

-- Grant write access  
ALTER ROLE db_datawriter ADD MEMBER [itsson-api];
```

3. **Verify it worked:**
```sql
SELECT dp.name AS UserName, r.name AS RoleName
FROM sys.database_role_members rm
JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
JOIN sys.database_principals dp ON rm.member_principal_id = dp.principal_id
WHERE dp.name = 'itsson-api';
```

You should see:
- `db_datareader`
- `db_datawriter`

## Important Notes

- ⚠️ The "Reader" role you added is at the **SQL Server level** (server-level)
- ✅ You also need **database-level roles** (db_datareader, db_datawriter) inside the `PropertyHub` database
- ✅ Make sure you're running the SQL commands in the correct database (`PropertyHub`, not `master`)

## After Running These Commands

1. The App Service should be able to connect to the database
2. Test your API: `POST https://itsson-api.azurewebsites.net/api/auth/login`
3. The 500 error should be resolved!
