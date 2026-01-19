# Simple Fix Options for 500 Error

## The Problem
Your API is returning a 500 Internal Server Error when you try to login via Postman.

## Why This Happens
The connection string you're using has `Authentication="Active Directory Default"` which requires Managed Identity setup. If Managed Identity isn't configured correctly, the database connection fails → 500 error.

## Two Solutions (Choose One)

### Option 1: Use SQL Authentication (EASIER - Recommended)

Change your connection string to use SQL Server authentication instead of Azure AD.

**In Azure Portal → itsson-api → Configuration → Application settings:**

**Setting Name:** `ConnectionStrings__DefaultConnection`  
**Setting Value (replace with SQL auth):**
```
Server=tcp:itss.database.windows.net,1433;Initial Catalog=PropertyHub;User ID=YOUR_SQL_USERNAME;Password=YOUR_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

**What you need:**
- SQL Server username (if you don't have one, create a SQL login)
- SQL Server password

**Pros:**
- ✅ Much simpler setup
- ✅ No Managed Identity configuration needed
- ✅ Works immediately

**Cons:**
- ⚠️ Password stored in App Settings (but that's normal for Azure)

### Option 2: Use Azure AD Authentication (MORE COMPLEX)

Keep Azure AD authentication but configure Managed Identity properly.

**Pros:**
- ✅ More secure (no passwords)
- ✅ Better for enterprise environments

**Cons:**
- ❌ Requires Managed Identity setup
- ❌ More steps to configure
- ❌ Can be confusing if you're not familiar with Azure AD

## My Recommendation

**Use Option 1 (SQL Authentication)** unless you specifically need Azure AD authentication for compliance/security reasons.

## Next Steps

1. **Do you have SQL Server credentials?**
   - If YES → Use Option 1, update the connection string
   - If NO → You'll need to create a SQL login first

2. **Which option do you want to use?**
   - Tell me and I'll guide you through it step-by-step

## What You Need to Know

The 500 error is happening because the app can't connect to the database. Once we fix the connection string (either option), the error should go away and your API will work.
