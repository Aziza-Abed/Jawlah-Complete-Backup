using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using FollowUp.Core.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.RateLimiting;
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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Catch unhandled exceptions that would crash the process (exit code -1)
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    var ex = args.ExceptionObject as Exception;
    Console.Error.WriteLine($"FATAL UNHANDLED EXCEPTION: {ex?.Message}");
    Console.Error.WriteLine(ex?.ToString());
    Log.Fatal(ex, "Unhandled exception crashed the process");
    Log.CloseAndFlush();
};

TaskScheduler.UnobservedTaskException += (sender, args) =>
{
    Log.Error(args.Exception, "Unobserved task exception");
    args.SetObserved(); // Prevent process crash from unobserved task exceptions
};

// setup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId:l} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/followup-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// add controllers and json settings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Prevent StackOverflowException from circular entity references (e.g., User → Supervisor → SupervisedWorkers → ...)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // Allow Arabic/Unicode characters without escaping, while still escaping HTML-dangerous chars (<, >, &)
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
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
    options.EnableAnnotations();
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
            // Use split queries globally to prevent cartesian explosion from multiple Include() calls
            // (e.g., Task includes Photos + Team → rows multiply, causing OOM with large datasets)
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            // increase command timeout to 60 seconds (default is 30)
            sqlOptions.CommandTimeout(AppConstants.DbCommandTimeoutSeconds);
            // enable connection resiliency with retry logic
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: AppConstants.DbMaxRetryCount,
                maxRetryDelay: TimeSpan.FromSeconds(AppConstants.DbMaxRetryDelaySeconds),
                errorNumbersToAdd: null
            );
        }
    ));

// setup authentication using JWT tokens
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured");

// reject dev placeholder keys in production
if (!builder.Environment.IsDevelopment() &&
    secretKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "JWT SecretKey is a development placeholder! Set a unique secret via environment variable JwtSettings__SecretKey");
}

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
        // SignalR WebSocket: extract JWT from query string since WebSocket can't use Authorization header
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
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

builder.Services.AddAuthorization(options =>
{
    // require authentication by default on all endpoints
    // endpoints that should be public must be explicitly marked with [AllowAnonymous]
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

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

builder.Services.AddSingleton<IPasswordHasher<FollowUp.Core.Entities.User>, PasswordHasher<FollowUp.Core.Entities.User>>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
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
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddHostedService<TaskGenerationBackgroundService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGisService, GisService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IOtpService, OtpService>();

builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// request size limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = AppConstants.MaxUploadSizeBytes;
});

// Kestrel server limits for file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = AppConstants.MaxUploadSizeBytes;
});

// Rate limiting: protect against brute force and abuse
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Auth endpoints: N requests/minute per IP (login, OTP, resend)
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AppConstants.AuthRateLimitRequestsPerMinute,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Upload endpoints: N requests/minute per IP
    options.AddPolicy("upload", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AppConstants.UploadRateLimitRequestsPerMinute,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Set QuestPDF license once at startup (instead of per-request in IssuesController)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var disableGeofencing = app.Configuration.GetValue<bool>("DeveloperMode:DisableGeofencing", false);
var mockSms = app.Configuration.GetValue<bool>("DeveloperMode:MockSms", false);

// block dev-only settings in production
if (!app.Environment.IsDevelopment())
{
    if (disableGeofencing)
    {
        Log.Fatal("SECURITY: Geofencing is DISABLED in non-Development environment!");
        throw new InvalidOperationException("Geofencing can only be disabled in Development environment");
    }
    if (mockSms)
    {
        Log.Fatal("SECURITY: MockSms is ENABLED in non-Development environment! OTPs will not be sent.");
        throw new InvalidOperationException("MockSms can only be enabled in Development environment");
    }
}

if (disableGeofencing)
{
    Console.WriteLine("WARNING: Geofencing validation is DISABLED!");
    Console.WriteLine("Workers can login from any location. This is for development only.");
}

if (mockSms)
{
    Console.WriteLine("WARNING: SMS is in MOCK mode! OTPs are logged, not sent.");
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

// CorrelationId middleware: trace requests across logs (before exception handling so errors are tagged too)
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N")[..12];
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

// Enable response compression (reduces payload size by 70-90%) — must be before exception handling
// so that error responses are also compressed
app.UseResponseCompression();

// global exception handling
app.UseExceptionHandling();

// Only redirect to HTTPS in production (dev runs on HTTP only)
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// basic security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// static files disabled - files served through FilesController for authentication
// app.UseStaticFiles();

if (app.Environment.IsDevelopment())
    app.UseCors("AllowAll");
else
    app.UseCors("Production");

app.UseSerilogRequestLogging();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TrackingHub>("/hubs/tracking");

// Ensure database schema is up-to-date (applies any pending migrations)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();
    await context.Database.MigrateAsync();
    Log.Information("Database migrations applied successfully");
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

                string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "GIS");
                Log.Information("Looking for GIS files in: {StoragePath}", storagePath);

                string? FindGisFile(string name) =>
                    File.Exists(Path.Combine(storagePath, name)) ? Path.Combine(storagePath, name) : null;

                var bordersFile = FindGisFile("borders.geojson");
                if (bordersFile != null)
                {
                    Log.Information("Importing Borders from: {Path}", bordersFile);
                    await gisService.ImportGeoJsonAsync(bordersFile, municipId, FollowUp.Core.Enums.GisFileType.Borders);
                }

                var quartersFile = FindGisFile("quarters.geojson");
                if (quartersFile != null)
                {
                    Log.Information("Importing Quarters from: {Path}", quartersFile);
                    await gisService.ImportGeoJsonAsync(quartersFile, municipId, FollowUp.Core.Enums.GisFileType.Quarters);
                }

                var blocksFile = FindGisFile("blocks.geojson");
                if (blocksFile != null)
                {
                    Log.Information("Importing Blocks from: {Path}", blocksFile);
                    await gisService.ImportBlocksFromGeoJsonAsync(blocksFile, municipId);
                }

                Log.Information("GIS Auto-Import Completed.");
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
