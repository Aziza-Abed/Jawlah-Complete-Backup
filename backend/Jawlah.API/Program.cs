using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jawlah.API.Filters;
using Jawlah.API.LiveTracking;
using Jawlah.API.Middleware;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Jawlah.Infrastructure.Repositories;
using Jawlah.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// setup logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/jawlah.log")
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

builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Jawlah API",
        Version = "v1.0",
        Description = "API for Al-Bireh Municipality field worker management system. Handles authentication, task tracking, GPS location services, and issue reporting."
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

builder.Services.AddDbContext<JawlahDbContext>(options =>
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
    ?? new[] { "https://jawlah.ps", "https://www.jawlah.ps" };

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

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGisService, GisService>();

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Jawlah API v1");
        options.RoutePrefix = string.Empty;
    });
}

// global exception handling
app.UseExceptionHandling();

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

// auto-import GIS zones if the table is empty
using (var scope = app.Services.CreateScope())
{
    try
    {
        var zoneRepo = scope.ServiceProvider.GetRequiredService<IZoneRepository>();
        var zones = await zoneRepo.GetActiveZonesAsync();

        if (!zones.Any())
        {
            Log.Information("Zones table is empty, attempting auto-import from shapefile...");

            // try to find shapefile in common locations
            var possiblePaths = new[]
            {
                Path.Combine(app.Environment.ContentRootPath, "..", "..", "GIS", "Blocks_WGS84.shp"),
                Path.Combine(app.Environment.ContentRootPath, "..", "GIS", "Blocks_WGS84.shp"),
                Path.Combine(app.Environment.ContentRootPath, "GIS", "Blocks_WGS84.shp"),
                @"C:\Users\hp\Documents\Jawlah\Jawlah-Repo\GIS\Blocks_WGS84.shp"
            };

            string? shapefilePath = null;
            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    shapefilePath = fullPath;
                    break;
                }
            }

            if (shapefilePath != null)
            {
                var gisService = scope.ServiceProvider.GetRequiredService<IGisService>();
                // Import zones for default municipality (Al-Bireh = 1)
                await gisService.ImportShapefileAsync(shapefilePath, municipalityId: 1);

                var importedZones = await zoneRepo.GetActiveZonesAsync();
                Log.Information("Auto-imported {Count} zones from shapefile", importedZones.Count());
            }
            else
            {
                Log.Warning("Shapefile not found. Zones table will remain empty. Use /api/zones/import-shapefile to import manually.");
            }
        }
        else
        {
            Log.Information("Found {Count} zones in database", zones.Count());
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to auto-import zones. This is not critical - zones can be imported manually.");
    }
}

try
{
    Log.Information("Starting Jawlah API...");
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
