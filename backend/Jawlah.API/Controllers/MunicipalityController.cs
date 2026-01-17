using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Municipality;
using Jawlah.Core.DTOs.Zones;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

/// <summary>
/// Controller for managing municipalities.
/// Admin-only endpoints for creating, updating, and managing municipalities.
/// </summary>
[Route("api/[controller]")]
public class MunicipalityController : BaseApiController
{
    private readonly IMunicipalityRepository _municipalities;
    private readonly IZoneRepository _zones;
    private readonly IUserRepository _users;
    private readonly IGisService _gisService;
    private readonly ILogger<MunicipalityController> _logger;

    public MunicipalityController(
        IMunicipalityRepository municipalities,
        IZoneRepository zones,
        IUserRepository users,
        IGisService gisService,
        ILogger<MunicipalityController> logger)
    {
        _municipalities = municipalities;
        _zones = zones;
        _users = users;
        _gisService = gisService;
        _logger = logger;
    }

    /// <summary>
    /// Get all municipalities (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var municipalities = await _municipalities.GetAllAsync();
            var dtos = new List<MunicipalityDto>();

            foreach (var m in municipalities)
            {
                var zones = await _zones.GetActiveZonesByMunicipalityAsync(m.MunicipalityId);
                var users = await _users.GetUsersByMunicipalityAsync(m.MunicipalityId);

                dtos.Add(MapToDto(m, zones.Count(), users.Count()));
            }

            return Ok(ApiResponse<IEnumerable<MunicipalityDto>>.SuccessResponse(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get municipalities");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("حدث خطأ في الخادم"));
        }
    }

    /// <summary>
    /// Get active municipalities only (for public/mobile use)
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var municipalities = await _municipalities.GetActiveAsync();
            var dtos = municipalities.Select(m => new
            {
                m.MunicipalityId,
                m.Code,
                m.Name,
                m.NameEnglish,
                m.Country,
                m.Region,
                m.LogoUrl
            });

            return Ok(ApiResponse<object>.SuccessResponse(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active municipalities");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("حدث خطأ في الخادم"));
        }
    }

    /// <summary>
    /// Get municipality by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var municipality = await _municipalities.GetByIdAsync(id);
            if (municipality == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("البلدية غير موجودة"));
            }

            var zones = await _zones.GetActiveZonesByMunicipalityAsync(id);
            var users = await _users.GetUsersByMunicipalityAsync(id);

            return Ok(ApiResponse<MunicipalityDto>.SuccessResponse(
                MapToDto(municipality, zones.Count(), users.Count())));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get municipality {MunicipalityId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("حدث خطأ في الخادم"));
        }
    }

    /// <summary>
    /// Create a new municipality (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMunicipalityRequest request)
    {
        try
        {
            // Check if code already exists
            if (await _municipalities.CodeExistsAsync(request.Code.ToUpperInvariant()))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("رمز البلدية مستخدم بالفعل"));
            }

            // Validate bounding box
            if (request.MinLatitude >= request.MaxLatitude)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("خط العرض الأدنى يجب أن يكون أقل من الأقصى"));
            }
            if (request.MinLongitude >= request.MaxLongitude)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("خط الطول الأدنى يجب أن يكون أقل من الأقصى"));
            }

            var municipality = new Municipality
            {
                Code = request.Code.ToUpperInvariant(),
                Name = request.Name,
                NameEnglish = request.NameEnglish,
                Country = request.Country,
                Region = request.Region,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Address = request.Address,
                MinLatitude = request.MinLatitude,
                MaxLatitude = request.MaxLatitude,
                MinLongitude = request.MinLongitude,
                MaxLongitude = request.MaxLongitude,
                DefaultGraceMinutes = request.DefaultGraceMinutes,
                MaxAcceptableAccuracyMeters = request.MaxAcceptableAccuracyMeters,
                LicenseExpiresAt = request.LicenseExpiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Parse work schedule times if provided
            if (!string.IsNullOrEmpty(request.DefaultStartTime) && TimeSpan.TryParse(request.DefaultStartTime, out var startTime))
            {
                municipality.DefaultStartTime = startTime;
            }
            if (!string.IsNullOrEmpty(request.DefaultEndTime) && TimeSpan.TryParse(request.DefaultEndTime, out var endTime))
            {
                municipality.DefaultEndTime = endTime;
            }

            await _municipalities.AddAsync(municipality);
            await _municipalities.SaveChangesAsync();

            _logger.LogInformation("Created municipality {Code} ({Name})", municipality.Code, municipality.Name);

            return CreatedAtAction(nameof(GetById), new { id = municipality.MunicipalityId },
                ApiResponse<MunicipalityDto>.SuccessResponse(MapToDto(municipality, 0, 0), "تم إنشاء البلدية بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create municipality");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء البلدية"));
        }
    }

    /// <summary>
    /// Update a municipality (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMunicipalityRequest request)
    {
        try
        {
            var municipality = await _municipalities.GetByIdAsync(id);
            if (municipality == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("البلدية غير موجودة"));
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.Name))
                municipality.Name = request.Name;
            if (request.NameEnglish != null)
                municipality.NameEnglish = request.NameEnglish;
            if (request.Region != null)
                municipality.Region = request.Region;
            if (request.ContactEmail != null)
                municipality.ContactEmail = request.ContactEmail;
            if (request.ContactPhone != null)
                municipality.ContactPhone = request.ContactPhone;
            if (request.Address != null)
                municipality.Address = request.Address;
            if (request.LogoUrl != null)
                municipality.LogoUrl = request.LogoUrl;

            // Update bounding box if all provided
            if (request.MinLatitude.HasValue && request.MaxLatitude.HasValue &&
                request.MinLongitude.HasValue && request.MaxLongitude.HasValue)
            {
                if (request.MinLatitude >= request.MaxLatitude)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("خط العرض الأدنى يجب أن يكون أقل من الأقصى"));
                }
                if (request.MinLongitude >= request.MaxLongitude)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("خط الطول الأدنى يجب أن يكون أقل من الأقصى"));
                }

                municipality.MinLatitude = request.MinLatitude.Value;
                municipality.MaxLatitude = request.MaxLatitude.Value;
                municipality.MinLongitude = request.MinLongitude.Value;
                municipality.MaxLongitude = request.MaxLongitude.Value;
            }

            // Update work schedule
            if (!string.IsNullOrEmpty(request.DefaultStartTime) && TimeSpan.TryParse(request.DefaultStartTime, out var startTime))
            {
                municipality.DefaultStartTime = startTime;
            }
            if (!string.IsNullOrEmpty(request.DefaultEndTime) && TimeSpan.TryParse(request.DefaultEndTime, out var endTime))
            {
                municipality.DefaultEndTime = endTime;
            }
            if (request.DefaultGraceMinutes.HasValue)
                municipality.DefaultGraceMinutes = request.DefaultGraceMinutes.Value;
            if (request.MaxAcceptableAccuracyMeters.HasValue)
                municipality.MaxAcceptableAccuracyMeters = request.MaxAcceptableAccuracyMeters.Value;

            if (request.IsActive.HasValue)
                municipality.IsActive = request.IsActive.Value;
            if (request.LicenseExpiresAt.HasValue)
                municipality.LicenseExpiresAt = request.LicenseExpiresAt.Value;

            municipality.UpdatedAt = DateTime.UtcNow;

            await _municipalities.UpdateAsync(municipality);
            await _municipalities.SaveChangesAsync();

            _logger.LogInformation("Updated municipality {MunicipalityId}", id);

            var zones = await _zones.GetActiveZonesByMunicipalityAsync(id);
            var users = await _users.GetUsersByMunicipalityAsync(id);

            return Ok(ApiResponse<MunicipalityDto>.SuccessResponse(
                MapToDto(municipality, zones.Count(), users.Count()), "تم تحديث البلدية بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update municipality {MunicipalityId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل تحديث البلدية"));
        }
    }

    /// <summary>
    /// Import zones from GeoJSON for a municipality (Admin only)
    /// </summary>
    [HttpPost("{id}/import-geojson")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportGeoJson(int id, [FromBody] ImportGeoJsonRequest request)
    {
        try
        {
            var municipality = await _municipalities.GetByIdAsync(id);
            if (municipality == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("البلدية غير موجودة"));
            }

            if (string.IsNullOrEmpty(request.GeoJson))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("محتوى GeoJSON مطلوب"));
            }

            await _gisService.ImportGeoJsonStringAsync(request.GeoJson, id);

            var zones = await _zones.GetActiveZonesByMunicipalityAsync(id);

            _logger.LogInformation("Imported GeoJSON for municipality {MunicipalityId}. Total zones: {Count}", id, zones.Count());

            return Ok(ApiResponse<object>.SuccessResponse(
                new { TotalZones = zones.Count() },
                "تم استيراد المناطق بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import GeoJSON for municipality {MunicipalityId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل استيراد ملف GeoJSON"));
        }
    }

    /// <summary>
    /// Get zones for a municipality
    /// </summary>
    [HttpGet("{id}/zones")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetZones(int id)
    {
        try
        {
            var municipality = await _municipalities.GetByIdAsync(id);
            if (municipality == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("البلدية غير موجودة"));
            }

            var zones = await _zones.GetActiveZonesByMunicipalityAsync(id);
            var zoneDtos = zones.Select(z => new
            {
                z.ZoneId,
                z.ZoneCode,
                z.ZoneName,
                z.Description,
                z.District,
                z.CenterLatitude,
                z.CenterLongitude,
                z.AreaSquareMeters,
                z.IsActive
            });

            return Ok(ApiResponse<object>.SuccessResponse(zoneDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get zones for municipality {MunicipalityId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("حدث خطأ في الخادم"));
        }
    }

    /// <summary>
    /// Deactivate a municipality (Admin only)
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            var municipality = await _municipalities.GetByIdAsync(id);
            if (municipality == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("البلدية غير موجودة"));
            }

            municipality.IsActive = false;
            municipality.UpdatedAt = DateTime.UtcNow;

            await _municipalities.UpdateAsync(municipality);
            await _municipalities.SaveChangesAsync();

            _logger.LogInformation("Deactivated municipality {MunicipalityId}", id);

            return Ok(ApiResponse<object>.SuccessResponse(null, "تم إيقاف البلدية بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate municipality {MunicipalityId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إيقاف البلدية"));
        }
    }

    /// <summary>
    /// Activate a municipality (Admin only)
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(int id)
    {
        try
        {
            var municipality = await _municipalities.GetByIdAsync(id);
            if (municipality == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("البلدية غير موجودة"));
            }

            municipality.IsActive = true;
            municipality.UpdatedAt = DateTime.UtcNow;

            await _municipalities.UpdateAsync(municipality);
            await _municipalities.SaveChangesAsync();

            _logger.LogInformation("Activated municipality {MunicipalityId}", id);

            return Ok(ApiResponse<object>.SuccessResponse(null, "تم تفعيل البلدية بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate municipality {MunicipalityId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل تفعيل البلدية"));
        }
    }

    private static MunicipalityDto MapToDto(Municipality m, int zonesCount, int usersCount)
    {
        return new MunicipalityDto
        {
            MunicipalityId = m.MunicipalityId,
            Code = m.Code,
            Name = m.Name,
            NameEnglish = m.NameEnglish,
            Country = m.Country,
            Region = m.Region,
            ContactEmail = m.ContactEmail,
            ContactPhone = m.ContactPhone,
            Address = m.Address,
            LogoUrl = m.LogoUrl,
            MinLatitude = m.MinLatitude,
            MaxLatitude = m.MaxLatitude,
            MinLongitude = m.MinLongitude,
            MaxLongitude = m.MaxLongitude,
            DefaultStartTime = m.DefaultStartTime.ToString(@"hh\:mm\:ss"),
            DefaultEndTime = m.DefaultEndTime.ToString(@"hh\:mm\:ss"),
            DefaultGraceMinutes = m.DefaultGraceMinutes,
            MaxAcceptableAccuracyMeters = m.MaxAcceptableAccuracyMeters,
            IsActive = m.IsActive,
            LicenseExpiresAt = m.LicenseExpiresAt,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            ZonesCount = zonesCount,
            UsersCount = usersCount
        };
    }
}

/// <summary>
/// Request for importing GeoJSON data
/// </summary>
public class ImportGeoJsonRequest
{
    public string GeoJson { get; set; } = string.Empty;
}
