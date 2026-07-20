using ClearPulse.Api.Auth;
using ClearPulse.Core.Services;
using ClearPulse.Infrastructure.Data;
using ClearPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// --------------------------------------
// Configure Serilog
// --------------------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/ClearPulse-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        outputTemplate:
        "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("==============================================");
    Log.Information("Starting ClearPulse API");
    Log.Information("==============================================");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Configuration flags
    var allowLocalAuth = builder.Configuration.GetValue<bool>("Auth:AllowLocalAuth");
    var enableADIntegration = builder.Configuration.GetValue<bool>("Auth:EnableADIntegration");

    // --------------------------------------
    // CORS
    // --------------------------------------
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
    builder.Services.AddSwaggerGen();

    // --------------------------------------
    // Database
    // --------------------------------------
    builder.Services.AddDbContext<ClearPulseDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("ClearPulseDb")));

    // --------------------------------------
    // Services
    // --------------------------------------
    builder.Services.AddScoped<IMetricsService, MetricsService>();
    builder.Services.AddScoped<ILogService, LogService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();

    // --------------------------------------
    // Authentication
    // --------------------------------------
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = allowLocalAuth ? "Hybrid" : JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = allowLocalAuth ? "Hybrid" : JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        if (enableADIntegration)
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            options.Audience = builder.Configuration["Auth:Audience"];

            var metadataAddress = builder.Configuration["Auth:MetadataAddress"];

            if (!string.IsNullOrWhiteSpace(metadataAddress))
                options.MetadataAddress = metadataAddress;

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
            var signingKey = builder.Configuration["Auth:Local:SigningKey"];

            if (!string.IsNullOrWhiteSpace(signingKey))
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Auth:Local:Issuer"] ?? "ClearPulseLocal",
                    ValidAudience = builder.Configuration["Auth:Audience"] ?? "ClearPulseApi",
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.RequireHttpsMetadata = false;
            }
        }
    })
    .AddScheme<AuthenticationSchemeOptions, LocalAuthHandler>("Local", options => { })
    .AddPolicyScheme("Hybrid", "Hybrid AD/Local", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();

            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return JwtBearerDefaults.AuthenticationScheme;

            return "Local";
        };
    });

    // --------------------------------------
    // Authorization
    // --------------------------------------
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("Corporate", policy => policy.RequireRole("Corporate", "Admin"));
        options.AddPolicy("CircleHead", policy => policy.RequireRole("CircleHead", "Corporate", "Admin"));
    });

    var app = builder.Build();

    // --------------------------------------
    // Seed Admin
    // --------------------------------------
    if (builder.Configuration.GetValue<bool>("Auth:SeedDefaultAdmin"))
    {
        using var scope = app.Services.CreateScope();

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ClearPulseDbContext>();

            await db.Database.MigrateAsync();

            if (!db.SystemUsers.Any(x => x.Username == "local.admin"))
            {
                db.SystemUsers.Add(new ClearPulse.Core.Entities.SystemUser
                {
                    Username = "local.admin",
                    Email = "admin@clearpulse.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
                    Role = "Admin",
                    TenantId = "SBI-TENANT-1",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                });

                await db.SaveChangesAsync();

                Log.Information("Default admin user created.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while seeding default admin.");
        }
    }

    // --------------------------------------
    // Middleware
    // --------------------------------------
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // Logs every API Request automatically
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("AllowAll");

    // app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/health", () =>
    {
        return Results.Ok(new
        {
            Status = "Healthy",
            Time = DateTime.UtcNow
        });
    }).AllowAnonymous();

    Log.Information("ClearPulse API Started Successfully.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.Information("ClearPulse API Stopped.");
    Log.CloseAndFlush();
}