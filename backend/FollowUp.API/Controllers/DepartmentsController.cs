using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Departments;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("Departments")]
public class DepartmentsController : BaseApiController
{
    private readonly IDepartmentRepository _departments;
    private readonly IMunicipalityRepository _municipalities;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(
        IDepartmentRepository departments,
        IMunicipalityRepository municipalities,
        ILogger<DepartmentsController> logger)
    {
        _departments = departments;
        _municipalities = municipalities;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "get all departments")]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = null)
    {
        var results = await _departments.GetAllWithUserCountAsync(activeOnly);

        var departments = results.Select(r => MapToDto(r.Department, r.UserCount)).ToList();

        return Ok(ApiResponse<List<DepartmentDto>>.SuccessResponse(departments));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "get department by id")]
    public async Task<IActionResult> GetById(int id)
    {
        var (department, userCount) = await _departments.GetByIdWithUserCountAsync(id);

        if (department == null)
            return NotFound(ApiResponse<object>.ErrorResponse("القسم غير موجود"));

        return Ok(ApiResponse<DepartmentDto>.SuccessResponse(MapToDto(department, userCount)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "create a new department")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<object>.ErrorResponse("اسم القسم مطلوب"));

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ApiResponse<object>.ErrorResponse("رمز القسم مطلوب"));

        // Check if code already exists
        if (await _departments.CodeExistsAsync(request.Code))
            return BadRequest(ApiResponse<object>.ErrorResponse("رمز القسم موجود مسبقاً"));

        // Get municipality ID (assuming single tenant for now)
        var allMunicipalities = await _municipalities.GetAllAsync();
        var municipality = allMunicipalities.FirstOrDefault();
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

        await _departments.AddAsync(department);
        await _departments.SaveChangesAsync();

        _logger.LogInformation("Department created: {Name} ({Code}) by user {UserId}",
            department.Name, department.Code, GetCurrentUserId());

        return Ok(ApiResponse<DepartmentDto>.SuccessResponse(MapToDto(department, 0), "تم إنشاء القسم بنجاح"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "update a department")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var department = await _departments.GetByIdAsync(id);
        if (department == null)
            return NotFound(ApiResponse<object>.ErrorResponse("القسم غير موجود"));

        // Check if new code already exists (if changing code)
        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != department.Code)
        {
            if (await _departments.CodeExistsAsync(request.Code, excludeId: id))
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

        await _departments.UpdateAsync(department);
        await _departments.SaveChangesAsync();

        _logger.LogInformation("Department updated: {Name} ({Code}) by user {UserId}",
            department.Name, department.Code, GetCurrentUserId());

        var usersCount = await _departments.GetUserCountAsync(id);

        return Ok(ApiResponse<DepartmentDto>.SuccessResponse(MapToDto(department, usersCount), "تم تحديث القسم بنجاح"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "delete a department")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _departments.GetByIdAsync(id);
        if (department == null)
            return NotFound(ApiResponse<object>.ErrorResponse("القسم غير موجود"));

        // Check if department has users
        var usersCount = await _departments.GetUserCountAsync(id);
        if (usersCount > 0)
            return BadRequest(ApiResponse<object>.ErrorResponse($"لا يمكن حذف القسم لأنه يحتوي على {usersCount} مستخدم. قم بنقلهم أولاً."));

        await _departments.DeleteAsync(department);
        await _departments.SaveChangesAsync();

        _logger.LogInformation("Department deleted: {Name} ({Code}) by user {UserId}",
            department.Name, department.Code, GetCurrentUserId());

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم حذف القسم بنجاح"));
    }

    // map a department entity to DTO
    private static DepartmentDto MapToDto(Department d, int usersCount) => new()
    {
        DepartmentId = d.DepartmentId,
        Name = d.Name,
        NameEnglish = d.NameEnglish,
        Code = d.Code,
        Description = d.Description,
        IsActive = d.IsActive,
        UsersCount = usersCount,
        CreatedAt = d.CreatedAt
    };
}
