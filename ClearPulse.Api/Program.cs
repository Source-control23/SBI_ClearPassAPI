using ClearPulse.Api.Auth;
using ClearPulse.Core.Services;
using ClearPulse.Infrastructure.Data;
using ClearPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Configuration flags
var allowLocalAuth = builder.Configuration.GetValue<bool>("Auth:AllowLocalAuth");
var enableADIntegration = builder.Configuration.GetValue<bool>("Auth:EnableADIntegration");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});




builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger without complex OpenAPI configuration
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<ClearPulseDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ClearPulseDb")));

// Services
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Authentication - Hybrid approach supporting both Local and AD/Azure AD
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = allowLocalAuth ? "Hybrid" : JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = allowLocalAuth ? "Hybrid" : JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    // Azure AD / ADFS Configuration
    if (enableADIntegration)
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];

        // Optional: For ADFS, you might need to configure metadata address
        var metadataAddress = builder.Configuration["Auth:MetadataAddress"];
        if (!string.IsNullOrEmpty(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        // Optional: Configure token validation parameters
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }
    else
    {
        // Local JWT signing (for development/testing with local users)
        var signingKey = builder.Configuration["Auth:Local:SigningKey"];
        if (!string.IsNullOrEmpty(signingKey))
        {
            var keyBytes = Encoding.UTF8.GetBytes(signingKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Auth:Local:Issuer"] ?? "ClearPulseLocal",
                ValidAudience = builder.Configuration["Auth:Audience"] ?? "ClearPulseApi",
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            // Don't require HTTPS metadata for local development
            options.RequireHttpsMetadata = false;
        }
    }
})
.AddScheme<AuthenticationSchemeOptions, LocalAuthHandler>("Local", options => { })
.AddPolicyScheme("Hybrid", "Hybrid AD/Local", options =>
{
    options.ForwardDefaultSelector = ctx =>
    {
        var authHeader = ctx.Request.Headers.Authorization.ToString();

        // If Bearer token present, use JWT Bearer authentication
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        // Otherwise use Local authentication (checks SystemUsers table)
        return "Local";
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Corporate", policy => policy.RequireRole("Corporate", "Admin"));
    options.AddPolicy("CircleHead", policy => policy.RequireRole("CircleHead", "Corporate", "Admin"));
});

var app = builder.Build();

// Seed default admin user if enabled
if (builder.Configuration.GetValue<bool>("Auth:SeedDefaultAdmin"))
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ClearPulseDbContext>();
            await db.Database.MigrateAsync();

            if (!db.SystemUsers.Any(u => u.Username == "local.admin"))
            {
                db.SystemUsers.Add(new ClearPulse.Core.Entities.SystemUser
                {
                    Username = "local.admin",
                    Email = "admin@clearpulse.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
                    Role = "Admin",
                    CircleCode = null, // Admin can see all circles
                    TenantId = "SBI-TENANT-1",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    LastLogin = null
                });

                await db.SaveChangesAsync();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Default admin user created: local.admin / P@ssw0rd!");
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error seeding default admin user");
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName,
    Version = "1.0",
    AuthConfig = new
    {
        AllowLocalAuth = allowLocalAuth,
        EnableADIntegration = enableADIntegration
    }
})).AllowAnonymous();

// API Info endpoint
app.MapGet("/api/info", () => Results.Ok(new
{
    Title = "ClearPulse Device Health Dashboard API",
    Version = "1.0",
    Description = "API for SBI ClearPass Device Health Dashboard",
    Documentation = "/swagger",
    Endpoints = new
    {
        Login = "/api/auth/login",
        Dashboard = "/api/dashboard/unified",
        Health = "/health"
    },
    Authentication = new
    {
        LocalAuth = allowLocalAuth ? "Enabled" : "Disabled",
        ADIntegration = enableADIntegration ? "Enabled" : "Disabled"
    }
})).AllowAnonymous();

app.Run();