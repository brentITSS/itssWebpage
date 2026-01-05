using backend.Middleware;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ITSS API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS configuration - read from environment variable or config
var allowedOrigins = builder.Configuration["CORS:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) 
    ?? new[] { "http://localhost:3000", "https://itsson.co.uk" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database configuration - prefer environment variable over config file
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured. Please set 'ConnectionStrings__DefaultConnection' in Azure App Service Configuration.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// JWT Authentication - prefer environment variables over config file
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JwtSettings__SecretKey") 
    ?? jwtSettings["SecretKey"];

if (string.IsNullOrWhiteSpace(secretKey))
{
    var errorMsg = "JWT SecretKey is not configured. Please set 'JwtSettings__SecretKey' in Azure App Service Configuration.";
    throw new InvalidOperationException(errorMsg);
}

var jwtIssuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer") 
    ?? jwtSettings["Issuer"] 
    ?? "ITSS";

var jwtAudience = Environment.GetEnvironmentVariable("JwtSettings__Audience") 
    ?? jwtSettings["Audience"] 
    ?? "ITSS-Users";

var jwtExpirationMinutes = int.TryParse(
    Environment.GetEnvironmentVariable("JwtSettings__ExpirationInMinutes") 
    ?? jwtSettings["ExpirationInMinutes"], 
    out var expiration) ? expiration : 60;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Register repositories - Task 3, 4, 5: Global Admin, Property Hub Home & Admin
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IWorkstreamRepository, WorkstreamRepository>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IJournalLogRepository, JournalLogRepository>();
builder.Services.AddScoped<IContactLogRepository, ContactLogRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Register services - Task 3, 4, 5: Global Admin, Property Hub Home & Admin
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IWorkstreamService, WorkstreamService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IJournalLogService, JournalLogService>();
builder.Services.AddScoped<IContactLogService, ContactLogService>();
builder.Services.AddScoped<ITagService, TagService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Error handling middleware - log errors before UseExceptionHandler
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path}");
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
        throw;
    }
});

// Production logging
if (app.Environment.IsProduction())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            
            logger.LogError(exception, "Unhandled exception: {Message}", exception?.Message);
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            var errorResponse = new { error = "An internal server error occurred", message = exception?.Message };
            await context.Response.WriteAsJsonAsync(errorResponse);
        });
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

// Authentication middleware - Task 2: JWT validation pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
