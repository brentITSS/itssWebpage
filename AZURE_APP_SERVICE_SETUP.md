# Azure App Service Configuration Guide

## Quick Setup Checklist

Your Azure App Service (`itsson-api`) needs the following Application Settings configured to resolve the 500 Internal Server Error.

## Required Application Settings

Go to Azure Portal → **itsson-api** → **Configuration** → **Application settings**

### 1. Database Connection String

**Name:** `ConnectionStrings__DefaultConnection`  
**Value:** Your Azure SQL connection string  
**Example:**
```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=ITSSDb;Persist Security Info=False;User ID=YOUR_USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

**Important:** Use double underscores `__` (not a single `:`)

### 2. JWT Settings

**Name:** `JwtSettings__SecretKey`  
**Value:** A secure secret key (minimum 32 characters)  
**Example:** `YourSecretKeyForJWTTokenGeneration-ShouldBeLongAndSecure-AtLeast32Characters`

**Name:** `JwtSettings__Issuer`  
**Value:** `ITSS`

**Name:** `JwtSettings__Audience`  
**Value:** `ITSS-Users`

**Name:** `JwtSettings__ExpirationInMinutes`  
**Value:** `60`

### 3. CORS Settings

**Name:** `CORS__AllowedOrigins`  
**Value:** Comma-separated list of allowed origins  
**Example:** `https://itsson.co.uk,http://localhost:3000`

### 4. Environment

**Name:** `ASPNETCORE_ENVIRONMENT`  
**Value:** `Production`

## How to Add Settings

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your App Service: **itsson-api**
3. Click **Configuration** in the left menu
4. Click **+ New application setting** for each setting
5. Enter the **Name** and **Value**
6. Click **OK** and then **Save** at the top
7. The app will automatically restart

## Verifying Configuration

After adding the settings, the application should restart. You can verify by:

1. Checking **Log stream** in Azure Portal to see if the app started successfully
2. Testing the API endpoint: `https://itsson-api.azurewebsites.net/api/auth/login`

## Troubleshooting

If you still get 500 errors after adding settings:

1. Check **Log stream** for detailed error messages
2. Verify connection string format (double underscores `__`)
3. Ensure all required settings are present
4. Check that the database server allows Azure App Service IP addresses
