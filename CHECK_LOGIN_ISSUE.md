# Login Not Working - Quick Checks

## First, check the browser console:

1. **Open browser developer tools:**
   - Press F12
   - Go to **Console** tab
   - Try to login
   - Look for any error messages (red text)

2. **Check Network tab:**
   - Go to **Network** tab in browser dev tools
   - Try to login
   - Look for a request to `/auth/login`
   - Click on it to see:
     - **Request URL**: What URL is it trying to connect to?
     - **Status**: What status code? (200 = success, 404 = not found, 500 = server error, CORS error = CORS issue)
     - **Response**: What error message?

## Common Issues:

### Issue 1: API URL is localhost
**Symptom:** Network tab shows request going to `http://localhost:5001/api/auth/login`

**Fix:** The `REACT_APP_API_URL` GitHub Secret is not set or not being used. It should be: `https://itsson-api.azurewebsites.net/api`

### Issue 2: CORS Error
**Symptom:** Console shows "CORS policy" or "Access-Control-Allow-Origin" error

**Fix:** Backend CORS needs to include `https://www.itsson.co.uk` in Azure App Service settings

### Issue 3: 404 Not Found
**Symptom:** Network tab shows 404 error

**Fix:** API endpoint might be wrong, or backend is not deployed correctly

### Issue 4: 500 Server Error
**Symptom:** Network tab shows 500 error

**Fix:** Backend API has an error - check Azure App Service logs

## Please provide:
1. What URL is shown in the Network tab for the login request?
2. What status code? (200, 404, 500, CORS error?)
3. What error message (if any)?
