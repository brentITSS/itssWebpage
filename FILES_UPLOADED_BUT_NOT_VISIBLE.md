# Files Uploaded But Not Visible

## The Problem
FTP logs show files were uploaded successfully, but they're not visible in `/static/js/` directory.

## Possible Causes:

### 1. Files Uploaded to Wrong Location
The FTP user's home directory might be different, so `/public_html/` resolves to a different path.

### 2. Need to Check Root Directory
Files might be in `/public_html/` root instead of subdirectories.

### 3. FTP User Home Directory Issue
The FTP user might start in a different directory than expected.

## What to Check:

1. **Check `/public_html/` root directory:**
   - Go up one level from `/static/js/`
   - Are the files in `/public_html/` root?
   - Look for `index.html`, `asset-manifest.json`, etc.

2. **Connect via FileZilla:**
   - Use FileZilla with the same FTP credentials
   - What directory do you start in?
   - Navigate to see where files actually are

3. **Check if files are in a different location:**
   - Maybe files are in `/domains/itsson.co.uk/public_html/` or similar

Let me help you figure out where the files actually are.
