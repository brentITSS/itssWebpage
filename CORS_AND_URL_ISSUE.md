# CORS Error + URL Still Wrong

## The Situation
1. ✅ Build verification shows correct URL (with `/api`)
2. ❌ Browser still shows wrong URL (missing `/api`)
3. ⚠️ New error: CORS policy blocking requests

## Two Issues to Fix:

### Issue 1: URL Still Wrong in Browser
Even though the build is correct, the browser is showing the wrong URL. This suggests:
- Browser cache (most likely)
- Old JavaScript file being served
- Multiple JS files and looking at the wrong one

### Issue 2: CORS Error
The backend needs to allow requests from `https://www.itsson.co.uk`

## Next Steps:

1. **First, verify what's actually on the server:**
   - Check Hostinger File Manager
   - Download a JavaScript file
   - Search for the API URL - does it have `/api`?

2. **If files on server are correct:**
   - Clear browser cache completely
   - Use incognito mode
   - Check Network tab to see what URL is being called

3. **Fix CORS on backend:**
   - Azure App Service → Configuration
   - Check `CORS__AllowedOrigins` setting
   - Must include: `https://www.itsson.co.uk,https://itsson.co.uk`

4. **Test again:**
   - With correct URL + CORS fixed, login should work
