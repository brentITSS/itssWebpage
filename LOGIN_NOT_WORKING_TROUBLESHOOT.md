# Login Not Working - Troubleshooting

## The Issue
The login screen appears, but authentication fails.

## Possible Causes

### 1. API URL Not Configured
The frontend might not be connecting to the backend API.

**Check GitHub Secrets:**
- `REACT_APP_API_URL` should be: `https://itsson-api.azurewebsites.net/api`

### 2. CORS Issue
The backend might not be allowing requests from `www.itsson.co.uk`

**Check Azure App Service:**
- `CORS__AllowedOrigins` should include: `https://www.itsson.co.uk,https://itsson.co.uk`

### 3. Backend API Not Accessible
The backend API might be down or unreachable.

**Test:** Try accessing: `https://itsson-api.azurewebsites.net/api/auth/login` (via Postman or browser)

### 4. Browser Console Errors
Check the browser console (F12) for errors when trying to login.

## Quick Checks

1. **Browser Console (F12):**
   - Open browser developer tools (F12)
   - Go to Console tab
   - Try to login
   - Look for any error messages (especially CORS errors or 404/500 errors)

2. **Network Tab:**
   - Go to Network tab in browser dev tools
   - Try to login
   - Look for the login request
   - Check if it's going to the right URL
   - Check the response status code

3. **Verify API URL:**
   - The request should go to: `https://itsson-api.azurewebsites.net/api/auth/login`
   - If it's going to `localhost:5001` or a different URL, the API URL isn't configured correctly
