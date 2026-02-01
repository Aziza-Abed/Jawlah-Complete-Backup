using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FollowUp.API.Filters;
using FollowUp.API.LiveTracking;
using FollowUp.API.Middleware;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using FollowUp.Infrastructure.Data;
using FollowUp.Infrastructure.Repositories;
using FollowUp.Infrastructure.Services;
using FollowUp.Infrastructure.BackgroundServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// setup logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/followup.log")
    .CreateLogger();

builder.Host.UseSerilog();

// add controllers and json settings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Allow Arabic/Unicode characters to be output without escaping
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

// Enable response compression (gzip/brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FollowUp API",
        Version = "v1.0",
        Description = "API for FollowUp field worker management system. Handles authentication, task tracking, GPS location services, and issue reporting."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    options.OperationFilter<FileUploadOperationFilter>();
});

// setup the database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<FollowUpDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.UseNetTopologySuite();
            // increase command timeout to 60 seconds (default is 30)
            sqlOptions.CommandTimeout(60);
            // enable connection resiliency with retry logic
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
        }
    ));

// setup authentication using JWT tokens
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured");

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Debug("Token validated for user: {User}",
                context.Principal?.Identity?.Name ?? "Unknown");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var productionOrigins = builder.Configuration.GetSection("Cors:ProductionOrigins").Get<string[]>()
    ?? new[] { "https://followup.ps", "https://www.followup.ps" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(productionOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// register all repositories and services (Dependency Injection)
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddHttpContextAccessor();

// Add password hasher for database seeding
builder.Services.AddSingleton<IPasswordHasher<FollowUp.Core.Entities.User>, PasswordHasher<FollowUp.Core.Entities.User>>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<IZoneRepository, ZoneRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ILocationHistoryRepository, LocationHistoryRepository>();
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
builder.Services.AddScoped<IMunicipalityRepository, MunicipalityRepository>();
builder.Services.AddScoped<IAppealRepository, AppealRepository>();
builder.Services.AddScoped<ITaskTemplateRepository, TaskTemplateRepository>();
builder.Services.AddScoped<IGisFileRepository, GisFileRepository>();
builder.Services.AddHostedService<TaskGenerationBackgroundService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGisService, GisService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IOtpService, OtpService>();

builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<AuditLogService>();

// request size limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB max
});

var app = builder.Build();

var disableGeofencing = app.Configuration.GetValue<bool>("DeveloperMode:DisableGeofencing", false);

if (disableGeofencing && !app.Environment.IsDevelopment())
{
    Log.Fatal("SECURITY WARNING: Geofencing is DISABLED in non-Development environment!");
    throw new InvalidOperationException("Geofencing can only be disabled in Development environment");
}

if (disableGeofencing)
{
    Console.WriteLine("WARNING: Geofencing validation is DISABLED!");
    Console.WriteLine("Workers can login from any location. This is for development only.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FollowUp API v1");
        options.RoutePrefix = string.Empty;
    });
}

// global exception handling
app.UseExceptionHandling();

// Enable response compression (reduces payload size by 70-90%)
app.UseResponseCompression();

app.UseHttpsRedirection();

// basic security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // remove server info headers
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Remove("X-Powered-By");

    await next();
});

// static files disabled - files served through FilesController for authentication
// app.UseStaticFiles();

if (app.Environment.IsDevelopment())
    app.UseCors("AllowAll");
else
    app.UseCors("Production");

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TrackingHub>("/hubs/tracking");

// seed database with initial data (UTF-8 safe via Entity Framework)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<FollowUp.Core.Entities.User>>();
        var seeder = new DatabaseSeeder(context, passwordHasher);
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to seed database.");
    }
}

// auto-import GIS zones if the table is empty
using (var scope = app.Services.CreateScope())
{
    try
    {
        var zoneRepo = scope.ServiceProvider.GetRequiredService<IZoneRepository>();
        var gisService = scope.ServiceProvider.GetRequiredService<IGisService>();
        var muniRepo = scope.ServiceProvider.GetRequiredService<IMunicipalityRepository>();

        var zones = await zoneRepo.GetActiveZonesAsync();

        if (!zones.Any())
        {
            Log.Information("Zones table is empty. Attempting to auto-import GIS files...");

            // Get the default municipality (Al-Bireh)
            var municipalities = await muniRepo.GetAllAsync();
            var municipality = municipalities.FirstOrDefault();

            if (municipality != null)
            {
                int municipId = municipality.MunicipalityId;

                // Priority 1: Check Storage/GIS folder (admin uploaded files)
                string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "GIS");

                // Priority 2: Fallback to repo GIS folder (legacy/initial setup)
                string gisBasePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "GIS"));

                if (!Directory.Exists(gisBasePath))
                {
                    // Fallback to try finding it if we are in bin/Debug
                    gisBasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "GIS"));
                }

                // Helper to find file in Storage first, then fallback
                string? FindGisFile(string storageName, string[] legacyNames)
                {
                    // Check Storage/GIS first
                    var storagefile = Path.Combine(storagePath, storageName);
                    if (File.Exists(storagefile)) return storagefile;

                    // Fallback to legacy locations
                    foreach (var name in legacyNames)
                    {
                        var legacyFile = Path.Combine(gisBasePath, name);
                        if (File.Exists(legacyFile)) return legacyFile;
                    }
                    return null;
                }

                Log.Information("Looking for GIS files in Storage: {StoragePath} and Legacy: {LegacyPath}", storagePath, gisBasePath);

                // 1. Urban Master Plan / Borders
                var bordersFile = FindGisFile("borders.geojson", new[] { "Urban_Master_Plan_Borders_1_WGS84_CORRECT.geojson" });
                if (bordersFile != null)
                {
                    Log.Information("Importing Borders from: {Path}", bordersFile);
                    await gisService.ImportGeoJsonAsync(bordersFile, municipId);
                }

                // 2. Quarters (Neighborhoods)
                var quartersFile = FindGisFile("quarters.geojson", new[] { "Quarters(Neighborhoods)_WGS84_CORRECT.geojson" });
                if (quartersFile != null)
                {
                    Log.Information("Importing Quarters from: {Path}", quartersFile);
                    await gisService.ImportGeoJsonAsync(quartersFile, municipId);
                }

                // 3. Blocks
                var blocksFile = FindGisFile("blocks.geojson", new[] { "Blocks_WGS84.geojson" });
                if (blocksFile != null)
                {
                    Log.Information("Importing Blocks from: {Path}", blocksFile);
                    await gisService.ImportBlocksFromGeoJsonAsync(blocksFile, municipId);
                }

                Log.Information("✅ GIS Auto-Import Completed.");
            }
            else
            {
                Log.Warning("No municipality found to assign GIS data to. Skipping auto-import.");
            }
        }
        else
        {
            Log.Information("Found {Count} zones in database - Skipping auto-import", zones.Count());
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to auto-import zones. This is not critical - zones can be imported manually.");
    }
}

try
{
    Log.Information("Starting FollowUp API...");
    Log.Information("Swagger UI available at: http://localhost:5000");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
