# Postman Request Format for Login

## Exact Request Configuration

### 1. Method and URL
- **Method:** `POST`
- **URL:** `https://itsson-api.azurewebsites.net/api/auth/login`

### 2. Headers Tab
Make sure you have this header:
- **Key:** `Content-Type`
- **Value:** `application/json`

### 3. Body Tab
1. Select **"raw"** (not form-data, x-www-form-urlencoded, etc.)
2. In the dropdown next to "raw", select **"JSON"** (not Text, HTML, etc.)
3. Enter this exact JSON (copy-paste to avoid typos):

```json
{
  "email": "brent@itsson.co.uk",
  "password": "111"
}
```

## Important Notes

✅ **Field names:** Use lowercase `"email"` and `"password"` (not `Email`, `EmailAddress`, etc.)  
✅ **Content-Type:** Must be `application/json`  
✅ **Body format:** Must be "raw" with "JSON" selected  
✅ **No extra characters:** Make sure there are no trailing commas or extra characters

## Common Mistakes

❌ Missing `Content-Type: application/json` header  
❌ Using "Text" instead of "JSON" in the body dropdown  
❌ Using form-data or x-www-form-urlencoded instead of raw JSON  
❌ Field name casing wrong (e.g., `Email` or `EmailAddress` instead of `email`)  
❌ Trailing commas in JSON: `{"email": "test@test.com",}` ← WRONG

## Testing

After setting it up correctly, click **Send**. You should get:
- **200 OK** with a token if credentials are correct
- **401 Unauthorized** if credentials are wrong (but request format is correct)
- **400 Bad Request** if the request format is wrong (what you're getting now)
