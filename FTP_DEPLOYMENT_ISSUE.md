# FTP Deployment Issue - DNS Resolution Failure

## The Problem
The build succeeded ✅, but the FTP deployment is failing with:
- `Error: getaddrinfo ENOTFOUND ***` (DNS resolution failure)
- "The server doesn't seem to exist. Do you have a typo?"

## Possible Causes

### 1. Incorrect FTP Server Hostname
The `HOSTINGER_FTP_SERVER` secret might have the wrong value.

**Check:**
- Go to your GitHub repository → Settings → Secrets and variables → Actions
- Verify `HOSTINGER_FTP_SERVER` matches exactly what Hostinger provides
- Common formats: `ftp.yourdomain.com` or `files.000webhost.com` or an IP address

### 2. Server Only Supports SFTP (not FTP/FTPS)
The error message hints: "Users sometimes get this error when the server only supports SFTP."

**Solution:**
- Check your Hostinger control panel to see if they support SFTP
- If SFTP is required, we need to switch the workflow to use an SFTP action instead of FTP

### 3. Hostname Typo
Double-check for typos in the hostname.

## Next Steps

1. **Verify FTP Credentials in Hostinger:**
   - Log into Hostinger control panel
   - Go to FTP section
   - Copy the exact FTP server/hostname
   - Verify it's correct in GitHub Secrets

2. **Check FTP vs SFTP:**
   - In Hostinger, check if they specify FTP or SFTP
   - If it's SFTP, we need to update the workflow to use an SFTP action

3. **Test Connection:**
   - Try connecting with an FTP client (FileZilla, WinSCP) using the same credentials
   - This will confirm if the credentials work

Would you like me to:
- Help you check the FTP server configuration?
- Update the workflow to use SFTP instead?
- Add better error handling to show the actual hostname (for debugging)?
