# Test Your API Now

## ✅ What You Just Completed

You've successfully:
- ✅ Created the database user `[itsson-api]` from the Managed Identity
- ✅ Granted `db_datareader` role (read access)
- ✅ Granted `db_datawriter` role (write access)

## 🧪 Test Your API

### Step 1: Restart Your App Service (if needed)

The database permissions should work immediately, but if you want to be sure:
1. Azure Portal → **itsson-api** App Service
2. Click **Restart** (top toolbar)
3. Wait 1-2 minutes for it to restart

### Step 2: Test the Login Endpoint

**In Postman (or your API client):**

1. **Method:** `POST`
2. **URL:** `https://itsson-api.azurewebsites.net/api/auth/login`
3. **Headers:**
   - `Content-Type: application/json`
4. **Body (raw JSON):**
   ```json
   {
     "email": "brent@itsson.co.uk",
     "password": "111"
   }
   ```

### Step 3: Expected Results

**✅ Success:** You should get a `200 OK` response with a JWT token:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "userId": 1,
    "email": "brent@itsson.co.uk",
    ...
  }
}
```

**❌ Still 500 Error?** Check:
- Azure Portal → **itsson-api** → **Log stream** for detailed error messages
- Verify all App Service settings are still configured correctly
- Verify the connection string uses: `Authentication="Active Directory Default";`

## 🎉 If It Works

Congratulations! Your API is now properly configured and connected to the database. You can continue with your application development.
