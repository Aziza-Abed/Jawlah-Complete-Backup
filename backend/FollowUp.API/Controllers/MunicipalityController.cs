using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Municipality;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("Municipality")]
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

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "get all municipalities")]
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

    [HttpGet("current")]
    [SwaggerOperation(Summary = "get current user municipality settings")]
    public async Task<IActionResult> GetCurrent()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("غير مصرح"));
            }

            var user = await _users.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));
            }

            var municipality = await _municipalities.GetByIdAsync(user.MunicipalityId);
            if (municipality == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("البلدية غير موجودة"));
            }

            // Return simplified settings for frontend/mobile use
            var settings = new
            {
                municipalityId = municipality.MunicipalityId,
                code = municipality.Code,
                name = municipality.Name,
                nameEnglish = municipality.NameEnglish,
                logoUrl = municipality.LogoUrl,
                // Map center (calculated from bounds)
                centerLatitude = (municipality.MinLatitude + municipality.MaxLatitude) / 2,
                centerLongitude = (municipality.MinLongitude + municipality.MaxLongitude) / 2,
                // Bounding box for map
                bounds = new
                {
                    minLatitude = municipality.MinLatitude,
                    maxLatitude = municipality.MaxLatitude,
                    minLongitude = municipality.MinLongitude,
                    maxLongitude = municipality.MaxLongitude
                },
                // Work schedule
                defaultStartTime = municipality.DefaultStartTime.ToString(@"hh\:mm"),
                defaultEndTime = municipality.DefaultEndTime.ToString(@"hh\:mm"),
                defaultGraceMinutes = municipality.DefaultGraceMinutes,
                maxAcceptableAccuracyMeters = municipality.MaxAcceptableAccuracyMeters,
                // Suggested zoom level based on area size
                defaultZoom = 13
            };

            return Ok(ApiResponse<object>.SuccessResponse(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current municipality settings");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("حدث خطأ في الخادم"));
        }
    }

    [HttpGet("default")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "get default municipality settings")]
    public async Task<IActionResult> GetDefault()
    {
        try
        {
            var municipalities = await _municipalities.GetActiveAsync();
            var municipality = municipalities.FirstOrDefault();

            if (municipality == null)
            {
                // Return fallback defaults if no municipality exists
                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    name = "FollowUp",
                    nameEnglish = "FollowUp System",
                    centerLatitude = 31.9,
                    centerLongitude = 35.2,
                    defaultZoom = 10
                }));
            }

            var settings = new
            {
                municipalityId = municipality.MunicipalityId,
                code = municipality.Code,
                name = municipality.Name,
                nameEnglish = municipality.NameEnglish,
                logoUrl = municipality.LogoUrl,
                centerLatitude = (municipality.MinLatitude + municipality.MaxLatitude) / 2,
                centerLongitude = (municipality.MinLongitude + municipality.MaxLongitude) / 2,
                defaultZoom = 13
            };

            return Ok(ApiResponse<object>.SuccessResponse(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default municipality settings");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("حدث خطأ في الخادم"));
        }
    }

    [HttpGet("active")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "get active municipalities")]
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

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "get municipality by id")]
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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "create a new municipality")]
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
                Name = InputSanitizer.SanitizeString(request.Name, 200),
                NameEnglish = InputSanitizer.SanitizeString(request.NameEnglish, 200),
                Country = InputSanitizer.SanitizeString(request.Country, 100),
                Region = InputSanitizer.SanitizeString(request.Region, 100),
                ContactEmail = request.ContactEmail,
                ContactPhone = InputSanitizer.SanitizeString(request.ContactPhone, 50),
                Address = InputSanitizer.SanitizeString(request.Address, 500),
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

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "update a municipality")]
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
                municipality.Name = InputSanitizer.SanitizeString(request.Name, 200);
            if (request.NameEnglish != null)
                municipality.NameEnglish = InputSanitizer.SanitizeString(request.NameEnglish, 200);
            if (request.Region != null)
                municipality.Region = InputSanitizer.SanitizeString(request.Region, 100);
            if (request.ContactEmail != null)
                municipality.ContactEmail = request.ContactEmail;
            if (request.ContactPhone != null)
                municipality.ContactPhone = InputSanitizer.SanitizeString(request.ContactPhone, 50);
            if (request.Address != null)
                municipality.Address = InputSanitizer.SanitizeString(request.Address, 500);
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

    [HttpPost("{id}/import-geojson")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "import zones from geojson")]
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

    [HttpGet("{id}/zones")]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "get zones for a municipality")]
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

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "deactivate a municipality")]
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

            return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم إيقاف البلدية بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate municipality {MunicipalityId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إيقاف البلدية"));
        }
    }

    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "activate a municipality")]
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

            return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم تفعيل البلدية بنجاح"));
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

// request for importing GeoJSON data
public class ImportGeoJsonRequest
{
    public string GeoJson { get; set; } = string.Empty;
}
