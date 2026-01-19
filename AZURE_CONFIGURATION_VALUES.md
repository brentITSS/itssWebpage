# Azure App Service Configuration Values

## Your Configuration Values - Exact Azure Portal Settings

Copy these **exactly** into Azure Portal → **itsson-api** → **Configuration** → **Application settings**:

### 1. Database Connection String
**Setting Name (copy exactly):** `ConnectionStrings__DefaultConnection`  
**Setting Value (copy exactly):**
```
Server=tcp:itss.database.windows.net,1433;Initial Catalog=PropertyHub;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication="Active Directory Default";
```

⚠️ **IMPORTANT - Azure AD Authentication:**  
Your connection string uses Azure AD (`Authentication="Active Directory Default"`). You must:
1. Enable **Managed Identity** on your App Service (`itsson-api`)
2. Grant the Managed Identity **SQL Database access** to `PropertyHub`
3. OR switch to SQL authentication if you prefer

To enable Managed Identity:
- Azure Portal → **itsson-api** → **Identity** → **System assigned** → **On** → **Save**

### 2. JWT Secret Key
**Setting Name (copy exactly):** `JwtSettings__SecretKey`  
**Setting Value:**
```
G7sP!zX9#qR2mYvK
```

⚠️ **Security Note:** This is 16 characters. For production, consider a longer key (32+ characters) for better security, but this will work.

### 3. JWT Issuer
**Setting Name (copy exactly):** `JwtSettings__Issuer`  
**Setting Value:**
```
itsson-api
```

### 4. JWT Audience
**Setting Name (copy exactly):** `JwtSettings__Audience`  
**Setting Value:**
```
itsson-users
```

### 5. JWT Expiration
**Setting Name (copy exactly):** `JwtSettings__ExpirationInMinutes`  
**Setting Value:**
```
60
```

### 6. CORS Allowed Origins
**Setting Name (copy exactly):** `CORS__AllowedOrigins`  
**Setting Value:**
```
https://itsson.co.uk,http://localhost:3000
```

### 7. Environment
**Setting Name (copy exactly):** `ASPNETCORE_ENVIRONMENT`  
**Setting Value:**
```
Production
```

## Step-by-Step Setup Instructions

1. **Go to Azure Portal** → Navigate to **itsson-api** App Service
2. **Click "Configuration"** in the left menu
3. **Click "+ New application setting"** for each setting above
4. **Copy the exact Setting Name** (including double underscores `__`)
5. **Copy the exact Setting Value**
6. **Click "OK"** for each setting
7. **Click "Save"** at the top (this will restart the app)
8. **Wait 1-2 minutes** for the restart

## Critical Points

✅ **Double Underscores:** All setting names MUST use double underscores `__` (not single underscore or colon)  
✅ **Exact Names:** The setting names are case-sensitive - copy them exactly  
✅ **Azure AD:** If using Azure AD auth, ensure Managed Identity is configured  
✅ **No Spaces:** Don't add spaces around the `=` sign in setting names

## After Configuration

1. **Check Log Stream:**
   - Azure Portal → **itsson-api** → **Log stream**
   - Look for startup messages and any errors

2. **Test the API:**
   - Try your Postman request again: `POST https://itsson-api.azurewebsites.net/api/auth/login`

## Troubleshooting

If you still get 500 errors after configuration:

1. **Check Log Stream** for specific error messages
2. **Verify Managed Identity** is enabled if using Azure AD authentication
3. **Double-check setting names** use double underscores `__`
4. **Ensure no typos** in setting values (especially connection string)
