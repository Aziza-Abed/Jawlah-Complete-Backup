using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Departments;
using FollowUp.Core.Entities;
using FollowUp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
public class DepartmentsController : BaseApiController
{
    private readonly FollowUpDbContext _context;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(FollowUpDbContext context, ILogger<DepartmentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all departments
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = null)
    {
        var query = _context.Departments.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(d => d.IsActive);
        }

        var departments = await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto
            {
                DepartmentId = d.DepartmentId,
                Name = d.Name,
                NameEnglish = d.NameEnglish,
                Code = d.Code,
                Description = d.Description,
                IsActive = d.IsActive,
                UsersCount = _context.Users.Count(u => u.DepartmentId == d.DepartmentId),
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<DepartmentDto>>.SuccessResponse(departments));
    }

    /// <summary>
    /// Get department by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetById(int id)
    {
        var department = await _context.Departments
            .Where(d => d.DepartmentId == id)
            .Select(d => new DepartmentDto
            {
                DepartmentId = d.DepartmentId,
                Name = d.Name,
                NameEnglish = d.NameEnglish,
                Code = d.Code,
                Description = d.Description,
                IsActive = d.IsActive,
                UsersCount = _context.Users.Count(u => u.DepartmentId == d.DepartmentId),
                CreatedAt = d.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (department == null)
            return NotFound(ApiResponse<object>.ErrorResponse("القسم غير موجود"));

        return Ok(ApiResponse<DepartmentDto>.SuccessResponse(department));
    }

    /// <summary>
    /// Create a new department (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.ErrorResponse("اسم القسم مطلوب"));

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.ErrorResponse("رمز القسم مطلوب"));

        // Check if code already exists
        var existingCode = await _context.Departments.AnyAsync(d => d.Code == request.Code);
        if (existingCode)
            return BadRequest(ApiResponse<object>.ErrorResponse("رمز القسم موجود مسبقاً"));

        // Get municipality ID (assuming single tenant for now)
        var municipality = await _context.Municipalities.FirstOrDefaultAsync();
        if (municipality == null)
            return BadRequest(ApiResponse<object>.ErrorResponse("لم يتم تكوين البلدية بعد"));

        var department = new Department
        {
            MunicipalityId = municipality.MunicipalityId,
            Name = InputSanitizer.SanitizeString(request.Name, 100),
            NameEnglish = InputSanitizer.SanitizeString(request.NameEnglish, 100),
            Code = request.Code.Trim().ToUpper(),
            Description = InputSanitizer.SanitizeString(request.Description, 500),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department created: {Name} ({Code}) by user {UserId}",
            department.Name, department.Code, GetCurrentUserId());

        var dto = new DepartmentDto
        {
            DepartmentId = department.DepartmentId,
            Name = department.Name,
            NameEnglish = department.NameEnglish,
            Code = department.Code,
            Description = department.Description,
            IsActive = department.IsActive,
            UsersCount = 0,
            CreatedAt = department.CreatedAt
        };

        return Ok(ApiResponse<DepartmentDto>.SuccessResponse(dto, "تم إنشاء القسم بنجاح"));
    }

    /// <summary>
    /// Update department (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
            return NotFound(ApiResponse<object>.ErrorResponse("القسم غير موجود"));

        // Check if new code already exists (if changing code)
        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != department.Code)
        {
            var existingCode = await _context.Departments.AnyAsync(d => d.Code == request.Code && d.DepartmentId != id);
            if (existingCode)
                return BadRequest(ApiResponse<object>.ErrorResponse("رمز القسم موجود مسبقاً"));
            department.Code = request.Code.Trim().ToUpper();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            department.Name = InputSanitizer.SanitizeString(request.Name, 100);

        if (request.NameEnglish != null)
            department.NameEnglish = InputSanitizer.SanitizeString(request.NameEnglish, 100);

        if (request.Description != null)
            department.Description = InputSanitizer.SanitizeString(request.Description, 500);

        if (request.IsActive.HasValue)
            department.IsActive = request.IsActive.Value;

        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Department updated: {Name} ({Code}) by user {UserId}",
            department.Name, department.Code, GetCurrentUserId());

        var usersCount = await _context.Users.CountAsync(u => u.DepartmentId == id);

        var dto = new DepartmentDto
        {
            DepartmentId = department.DepartmentId,
            Name = department.Name,
            NameEnglish = department.NameEnglish,
            Code = department.Code,
            Description = department.Description,
            IsActive = department.IsActive,
            UsersCount = usersCount,
            CreatedAt = department.CreatedAt
        };

        return Ok(ApiResponse<DepartmentDto>.SuccessResponse(dto, "تم تحديث القسم بنجاح"));
    }

    /// <summary>
    /// Delete department (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
            return NotFound(ApiResponse<object>.ErrorResponse("القسم غير موجود"));

        // Check if department has users
        var usersCount = await _context.Users.CountAsync(u => u.DepartmentId == id);
        if (usersCount > 0)
            return BadRequest(ApiResponse<object>.ErrorResponse($"لا يمكن حذف القسم لأنه يحتوي على {usersCount} مستخدم. قم بنقلهم أولاً."));

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department deleted: {Name} ({Code}) by user {UserId}",
            department.Name, department.Code, GetCurrentUserId());

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم حذف القسم بنجاح"));
    }
}
