# FTP Not Uploading Files At All

## The Problem
After deleting files and redeploying, NO new files were created. This means the FTP deployment isn't working.

## Possible Causes:

### 1. Wrong Server Directory
The `server-dir: ./` might not be resolving to the correct location. The FTP user's home directory might not be `/public_html/`.

### 2. FTP Upload Failing Silently
The FTP action might be failing but not showing errors.

### 3. Files Uploading to Wrong Location
Files might be uploading to a different directory than expected.

## What to Check:

1. **Check FTP Deployment Logs:**
   - What does the latest deployment show?
   - Are there any errors?
   - Does it say "Uploading" or "Sync complete"?

2. **Check FTP User Home Directory:**
   - When you connect via FileZilla, what directory do you start in?
   - Is it `/public_html/` or something else?

3. **Try Absolute Path:**
   - Instead of `server-dir: ./`, try the full path
   - Based on earlier info: `/domains/itsson.co.uk/public_html/` or `/public_html/`

Let me check the deployment logs and try using an absolute path.
