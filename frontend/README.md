# ITSS Frontend

React TypeScript application for the ITSS (IT Support System) frontend.

## Prerequisites

- Node.js 18.x or higher
- npm or yarn

## Configuration

### Environment Variables

The frontend application reads configuration from environment variables. For local development, create a `.env` file in the `frontend/` directory based on `.env.example`.

#### Required Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `REACT_APP_API_URL` | Backend API base URL | `https://localhost:5001/api` (local) or `https://your-api.azurewebsites.net/api` (production) |

**Note**: React environment variables must be prefixed with `REACT_APP_` to be accessible in the application.

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd MainWebpage/frontend
   ```

2. **Create environment file**
   ```bash
   cp .env.example .env
   ```

3. **Configure environment variables**
   Edit `.env` and set your API URL:
   ```env
   REACT_APP_API_URL=https://localhost:5001/api
   ```

4. **Install dependencies**
   ```bash
   npm install
   ```

5. **Run the development server**
   ```bash
   npm start
   ```

   The application will be available at `http://localhost:3000`

## Building for Production

```bash
npm run build
```

This creates an optimized production build in the `build/` directory.

### Production Build Optimization

- Code splitting
- Minification
- Tree shaking
- Dead code elimination
- Source map generation (disabled in production)

## Deployment

The frontend is automatically deployed to Hostinger via GitHub Actions when code is pushed to the `main` branch.

### Hostinger FTP Deployment Setup

1. **Get FTP Credentials**
   - From your Hostinger control panel, get your FTP server, username, and password

2. **Configure GitHub Secrets**
   In your GitHub repository, go to Settings → Secrets and variables → Actions, and add:

   - `HOSTINGER_FTP_SERVER`: Your Hostinger FTP server (e.g., `ftp.yourdomain.com`)
   - `HOSTINGER_FTP_USERNAME`: Your FTP username
   - `HOSTINGER_FTP_PASSWORD`: Your FTP password
   - `REACT_APP_API_URL`: Production API URL (e.g., `https://your-api.azurewebsites.net/api`)

3. **Deployment Workflow**

   The GitHub Actions workflow (`.github/workflows/frontend-deploy.yml`) automatically:
   1. Installs dependencies
   2. Creates `.env.production` file with API URL
   3. Builds the React application
   4. Deploys the `build/` directory to Hostinger via FTP

### Manual Deployment

If you prefer to deploy manually:

1. **Build the application**
   ```bash
   npm run build
   ```

2. **Upload to Hostinger**
   - Use FTP client (FileZilla, WinSCP, etc.)
   - Upload all files from `build/` directory to `public_html/` on Hostinger

## Project Structure

```
frontend/
├── public/              # Static assets
├── src/
│   ├── components/     # Reusable React components
│   ├── pages/          # Page components
│   ├── services/       # API service layer
│   ├── routes/         # React Router configuration
│   └── App.tsx         # Main application component
├── package.json        # Dependencies and scripts
└── .env.example        # Environment variable template
```

## Available Scripts

- `npm start`: Runs the app in development mode
- `npm run build`: Creates a production build
- `npm test`: Runs the test suite
- `npm run eject`: Ejects from Create React App (irreversible)

## Environment-Specific Configuration

### Development
- Hot module reloading
- Source maps enabled
- Detailed error messages
- API URL: `http://localhost:5001/api`

### Production
- Optimized build
- Minified code
- Source maps disabled
- API URL: Set via `REACT_APP_API_URL` environment variable

### Staging (Optional)
- Similar to production but with staging API URL
- Can be configured via environment-specific `.env.staging` file

## CORS Configuration

Ensure the backend CORS settings include your frontend domain:
- Development: `http://localhost:3000`
- Production: Your Hostinger domain (e.g., `https://itsson.co.uk`)

## Troubleshooting

### Build Errors
- Clear `node_modules` and `package-lock.json`, then run `npm install` again
- Ensure Node.js version matches requirements (18.x+)

### API Connection Issues
- Verify `REACT_APP_API_URL` is set correctly
- Check CORS settings on the backend
- Ensure the backend is running and accessible

### Deployment Issues
- Verify FTP credentials in GitHub Secrets
- Check Hostinger FTP server is accessible
- Ensure `build/` directory contains all necessary files

## Support

For issues or questions, please contact the development team.
