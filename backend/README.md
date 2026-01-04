# ITSS Backend API

ASP.NET Core 8 Web API for the ITSS (IT Support System) application.

## Prerequisites

- .NET 8 SDK
- Azure SQL Database (already provisioned)
- Visual Studio 2022 or VS Code

## Configuration

### Environment Variables

The backend application reads configuration from environment variables. For local development, create a `.env` file in the `backend/` directory based on `.env.example`.

#### Required Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Azure SQL database connection string | `Server=tcp:...;Database=ITSSDb;...` |
| `JwtSettings__SecretKey` | Secret key for JWT token generation (min 32 characters) | `YourSecretKeyForJWTTokenGeneration...` |
| `JwtSettings__Issuer` | JWT issuer name | `ITSS` |
| `JwtSettings__Audience` | JWT audience name | `ITSS-Users` |
| `JwtSettings__ExpirationInMinutes` | JWT token expiration time in minutes | `60` |
| `CORS__AllowedOrigins` | Comma-separated list of allowed CORS origins | `http://localhost:3000,https://itsson.co.uk` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development`, `Production`, or `Staging` |

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd MainWebpage/backend
   ```

2. **Create environment file**
   ```bash
   cp .env.example .env
   ```

3. **Configure environment variables**
   Edit `.env` and update the values:
   - Set your Azure SQL connection string
   - Set a secure JWT secret key (at least 32 characters)
   - Configure CORS origins if needed

4. **Restore dependencies**
   ```bash
   dotnet restore
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

   The API will be available at:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`

### Azure App Service Deployment

The backend is automatically deployed to Azure App Service via GitHub Actions when code is pushed to the `main` branch.

#### Setting up Azure App Service Configuration

1. **Get Publish Profile**
   - In Azure Portal, navigate to your App Service
   - Go to "Get publish profile" and download the `.PublishSettings` file

2. **Configure GitHub Secrets**
   In your GitHub repository, go to Settings → Secrets and variables → Actions, and add:

   - `AZURE_WEBAPP_NAME`: Your Azure App Service name
   - `AZURE_WEBAPP_PUBLISH_PROFILE`: Contents of the `.PublishSettings` file
   - `AZURE_SQL_CONNECTION_STRING`: Your Azure SQL connection string
   - `JWT_SECRET_KEY`: Your JWT secret key
   - `JWT_ISSUER`: JWT issuer (e.g., `ITSS`)
   - `JWT_AUDIENCE`: JWT audience (e.g., `ITSS-Users`)
   - `JWT_EXPIRATION_IN_MINUTES`: Token expiration (e.g., `60`)
   - `CORS_ALLOWED_ORIGINS`: Comma-separated allowed origins (e.g., `https://itsson.co.uk`)

3. **Configure App Service Settings (Alternative)**
   Alternatively, you can configure these in Azure Portal:
   - Go to your App Service → Configuration → Application settings
   - Add the following settings (use double underscores `__` instead of `:`):
     - `ConnectionStrings__DefaultConnection`
     - `JwtSettings__SecretKey`
     - `JwtSettings__Issuer`
     - `JwtSettings__Audience`
     - `JwtSettings__ExpirationInMinutes`
     - `CORS__AllowedOrigins`
     - `ASPNETCORE_ENVIRONMENT` = `Production`

#### Deployment Workflow

The GitHub Actions workflow (`.github/workflows/backend-deploy.yml`) automatically:
1. Builds the .NET application
2. Publishes the build output
3. Deploys to Azure App Service
4. Configures application settings

## Project Structure

```
backend/
├── Controllers/          # API controllers
├── Services/            # Business logic services
├── Repositories/        # Data access repositories
├── Models/              # Entity Framework models
├── DTOs/               # Data transfer objects
├── Middleware/         # Custom middleware
├── Program.cs          # Application entry point
├── appsettings.json    # Configuration (fallback only)
└── .env.example        # Environment variable template
```

## API Documentation

When running in Development mode, Swagger UI is available at `/swagger`.

## Architecture

The application follows a layered architecture:

- **Controllers**: Handle HTTP requests/responses (thin layer)
- **Services**: Business logic and validation
- **Repositories**: Data access using Entity Framework Core
- **DTOs**: Data transfer objects for API contracts
- **Middleware**: Cross-cutting concerns (authentication, authorization, logging)

## Security

- JWT-based authentication
- Password hashing using BCrypt
- CORS configuration for allowed origins
- Environment variable-based secrets (never hardcoded)
- Production error handling

## Logging

- Development: Detailed console logging
- Production: Structured logging with error handling
- Logs include request method, path, and timing

## Support

For issues or questions, please contact the development team.
