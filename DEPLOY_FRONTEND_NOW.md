# Deploy Frontend Now - Quick Guide

## ✅ GitHub Secrets Added
Great! You've added all the required secrets to GitHub.

## Next Step: Trigger the Deployment

You have two options to deploy:

### Option 1: Manually Trigger Workflow (Recommended for First Deployment)

1. **Go to GitHub Repository**
   - Navigate to: `https://github.com/brentITSS/itssWebpage`

2. **Click on "Actions" Tab**
   - This is at the top of your repository

3. **Select "Deploy Frontend to Hostinger" workflow**
   - You should see it in the list of workflows on the left

4. **Click "Run workflow" button**
   - It's on the right side of the page
   - Make sure "main" branch is selected
   - Click the green "Run workflow" button

5. **Monitor the Deployment**
   - You'll see the workflow run in real-time
   - Green checkmark = success ✅
   - Red X = failure (check logs)

### Option 2: Push a Change (Auto-trigger)

If you make any change to files in the `frontend/` directory and push to `main`, the workflow will automatically trigger.

## What to Expect

Once the workflow runs successfully:
- ✅ React app will be built
- ✅ Files will be uploaded to Hostinger via FTP
- ✅ Your site at `www.itsson.co.uk` should be live!

## Verify Deployment

After the workflow completes:
1. Wait 1-2 minutes for DNS/files to propagate
2. Visit: `https://www.itsson.co.uk`
3. You should see your React application!

## Troubleshooting

If the workflow fails:
- Check the workflow logs in GitHub Actions
- Verify FTP credentials are correct
- Ensure FTP account has access to `public_html` directory
- Check that `REACT_APP_API_URL` is set correctly
