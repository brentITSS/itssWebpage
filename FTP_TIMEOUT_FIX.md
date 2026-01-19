# FTP Timeout Error - Troubleshooting

## The Issue
The build succeeded but FTP deployment is timing out with "Timeout (control socket)".

## Possible Causes

### 1. Temporary Network Issue
FTP servers sometimes have temporary connectivity issues.

**Solution:** Wait a few minutes and trigger the workflow again manually.

### 2. FTP Server Blocking GitHub Actions IPs
Some FTP servers block connections from cloud IPs (like GitHub Actions).

**Solution:** Check Hostinger firewall settings or contact Hostinger support.

### 3. FTP Credentials Issue
Incorrect credentials can cause timeouts.

**Solution:** Verify FTP credentials in GitHub Secrets match Hostinger.

### 4. FTP Server Overloaded
The FTP server might be temporarily busy.

**Solution:** Try again in a few minutes.

## Quick Fixes to Try

### Option 1: Retry the Deployment
1. Go to GitHub Actions tab
2. Find the failed workflow run
3. Click "Re-run all jobs" or "Re-run failed jobs"

### Option 2: Use FTPS (Secure FTP)
Hostinger might require secure FTP. Let's try adding FTPS support.

### Option 3: Manual Upload (Temporary Workaround)
If FTP continues to fail, you can manually upload the build files:
1. Build locally: `cd frontend && npm run build`
2. Upload `build/` folder contents to Hostinger via FileZilla

Let me know if you want to try adding FTPS support or if you want to try retrying first.
