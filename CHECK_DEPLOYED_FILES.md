# Check What API URL is Actually in the Deployed Files

## The Problem
The build logs don't show environment variable values (they're secrets). We need to check what's actually in the deployed JavaScript files.

## Option 1: Check Deployed JavaScript Files (Fastest)

1. **Go to your website:** https://www.itsson.co.uk
2. **Open browser DevTools (F12)**
3. **Go to Sources tab**
4. **Find the JavaScript files:**
   - Look for files like `main.[hash].js` in the static/js folder
   - Or go to Network tab, reload the page, find the main JavaScript file
5. **Search in the file:**
   - Press Ctrl+F (or Cmd+F on Mac)
   - Search for: `itsson-api.azurewebsites.net`
   - This will show you what URL is actually in the code

You should see either:
- `https://itsson-api.azurewebsites.net/api` (correct)
- `https://itsson-api.azurewebsites.net` (wrong - missing /api)

## Option 2: Check via File Manager (If you have FTP access)

1. Log into Hostinger File Manager
2. Navigate to `/public_html/static/js/`
3. Find the main JavaScript file (something like `main.xxxxx.js`)
4. Download it and search for `itsson-api.azurewebsites.net`

## What This Will Tell Us

- If the file shows the WRONG URL: The build used the wrong secret value, or the secret wasn't updated when the build ran
- If the file shows the CORRECT URL: But the browser is still using the wrong one, then it's definitely a browser cache issue (but we tested incognito, so this is unlikely)
