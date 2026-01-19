# Fix Hostinger Default Page Issue - Complete Solution

## The Problem
You're seeing the default Hostinger page instead of your React app at `www.itsson.co.uk`.

## Root Cause
The most common causes are:
1. **Default Hostinger files still exist** (index.php, default index.html)
2. **Files not in the correct location** on Hostinger
3. **Browser cache** showing old content
4. **Deployment didn't complete** successfully

## Complete Fix - Step by Step

### Step 1: Manually Delete Default Files on Hostinger

**This is the most important step!**

1. **Log into Hostinger hPanel**
   - Go to: https://hpanel.hostinger.com/
   - Log in with your Hostinger credentials

2. **Open File Manager**
   - Click on "File Manager" in the left menu
   - Navigate to `/public_html/` directory

3. **Delete ALL default Hostinger files:**
   - Look for and DELETE these files if they exist:
     - `index.php` (default Hostinger file)
     - `index.html` (if it's the default Hostinger one - check the content)
     - `default.html`
     - `cpanel-default.html`
     - Any `.htaccess` file that might be redirecting
     - Any other default Hostinger files

4. **Verify your React app files exist:**
   - You should see:
     - `index.html` (your React app's index.html)
     - `static/` folder (with `js/` and `css/` subfolders)
     - `asset-manifest.json`
     - `manifest.json`
     - `robots.txt` (if present)

### Step 2: Trigger a Fresh Deployment

1. **Go to GitHub Actions**
   - Navigate to: https://github.com/brentITSS/itssWebpage/actions

2. **Find "Deploy Frontend to Hostinger" workflow**
   - Click on it in the left sidebar

3. **Click "Run workflow"**
   - Select "main" branch
   - Click the green "Run workflow" button

4. **Wait for deployment to complete**
   - Watch the workflow run
   - Make sure all steps show green checkmarks ✅

### Step 3: Verify Files on Hostinger After Deployment

1. **Go back to Hostinger File Manager**
2. **Check `/public_html/` directory:**
   - `index.html` should be there (your React app)
   - `static/` folder should exist
   - Check file timestamps - they should be very recent (just now)

3. **Open `index.html` in File Manager**
   - Right-click → View/Edit
   - It should start with `<!DOCTYPE html>` and contain React app content
   - It should NOT be the default Hostinger page

### Step 4: Clear Browser Cache Completely

**This is critical!**

1. **Close ALL browser windows completely**

2. **Clear browser cache:**
   - **Chrome/Edge:** Press `Ctrl+Shift+Delete`
     - Select "Cached images and files"
     - Time range: "All time"
     - Click "Clear data"
   - **Firefox:** Press `Ctrl+Shift+Delete`
     - Select "Cache"
     - Time range: "Everything"
     - Click "Clear Now"

3. **Open a NEW incognito/private window**
   - Chrome: `Ctrl+Shift+N`
   - Firefox: `Ctrl+Shift+P`
   - Edge: `Ctrl+Shift+N`

4. **Visit the site:**
   - Go to: `https://www.itsson.co.uk`
   - You should now see your React app!

### Step 5: Verify It's Working

1. **Check the page content:**
   - You should see your React app (Login page)
   - NOT the default Hostinger page

2. **Open browser DevTools (F12):**
   - Go to "Network" tab
   - Check "Disable cache" checkbox
   - Reload the page
   - Look for your React app's JavaScript files loading

3. **Check the console:**
   - Go to "Console" tab
   - Should see no errors (or only expected ones)
   - Should NOT see errors about missing files

## If It Still Doesn't Work

### Check 1: Verify Files Are Actually Deployed

1. In Hostinger File Manager, check:
   - Are files in `/public_html/` or somewhere else?
   - What are the file timestamps? (should be recent)
   - How many files are in `/public_html/static/js/`? (should be at least 1)

### Check 2: Verify Domain Configuration

1. In Hostinger hPanel:
   - Go to "Domains" section
   - Verify `itsson.co.uk` is pointing to the correct hosting account
   - Check DNS settings if needed

### Check 3: Check for .htaccess Issues

1. In Hostinger File Manager:
   - Look for `.htaccess` file in `/public_html/`
   - If it exists, check its contents
   - It might be redirecting to default page
   - You may need to delete or modify it

### Check 4: Verify GitHub Secrets

1. Go to GitHub → Settings → Secrets and variables → Actions
2. Verify these secrets exist:
   - `HOSTINGER_FTP_SERVER`
   - `HOSTINGER_FTP_USERNAME`
   - `HOSTINGER_FTP_PASSWORD`
   - `REACT_APP_API_URL` (should be `https://itsson-api.azurewebsites.net/api`)

## Quick Test: Manual File Upload

If automated deployment isn't working, test with manual upload:

1. **Build locally:**
   ```bash
   cd frontend
   npm install
   npm run build
   ```

2. **Upload via File Manager:**
   - In Hostinger File Manager, go to `/public_html/`
   - Delete everything in that directory
   - Upload ALL files from `frontend/build/` to `/public_html/`
   - Make sure `index.html` is in the root of `/public_html/`

3. **Test the site:**
   - Visit `www.itsson.co.uk`
   - Should now work!

## Success Indicators

✅ You see your React app's login page  
✅ Browser console shows no major errors  
✅ Network tab shows your React app's JS/CSS files loading  
✅ The page is NOT the default Hostinger page  

## Next Steps After Fix

Once it's working:
1. Test login functionality
2. Verify API connection works
3. Test other features of your app

---

**Most Important:** Delete the default Hostinger files FIRST, then trigger a fresh deployment. This is usually the root cause!
