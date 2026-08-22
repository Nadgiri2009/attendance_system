using EWMS.Domain.Entities;
using EWMS.Domain.Common;
using EWMS.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWMS.API.Controllers;

[Authorize(Roles = "Admin")]
public class MasterDataController : BaseApiController
{
    private readonly ApplicationDbContext _db;

    public MasterDataController(ApplicationDbContext db) => _db = db;

    [HttpGet("departments")]
    public async Task<IActionResult> Departments(CancellationToken cancellationToken) =>
        Ok(new { success = true, data = await _db.Departments.OrderBy(item => item.Name).Select(item => new { item.Id, item.Name, item.Code, item.ParentDepartmentId }).ToListAsync(cancellationToken) });

    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment([FromBody] DepartmentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code)) return BadRequest(new { message = "Name and code are required." });
        if (request.ParentDepartmentId.HasValue && !await _db.Departments.AnyAsync(item => item.Id == request.ParentDepartmentId, cancellationToken)) return BadRequest(new { message = "Parent department was not found." });
        var department = new Department { Name = request.Name.Trim(), Code = request.Code.Trim(), ParentDepartmentId = request.ParentDepartmentId };
        _db.Departments.Add(department); await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true, data = department });
    }

    [HttpPut("departments/{id:guid}")]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] DepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (department is null) return NotFound();
        if (id == request.ParentDepartmentId) return BadRequest(new { message = "A department cannot be its own parent." });
        department.Name = request.Name.Trim(); department.Code = request.Code.Trim(); department.ParentDepartmentId = request.ParentDepartmentId;
        await _db.SaveChangesAsync(cancellationToken); return Ok(new { success = true, data = department });
    }

    [HttpDelete("departments/{id:guid}")]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken) => await SoftDelete(_db.Departments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken), cancellationToken);

    [HttpGet("designations")]
    public async Task<IActionResult> Designations(CancellationToken cancellationToken) =>
        Ok(new { success = true, data = await _db.Designations.Include(item => item.Department).OrderBy(item => item.Title).Select(item => new { item.Id, item.Title, item.DepartmentId, DepartmentName = item.Department.Name, item.Level }).ToListAsync(cancellationToken) });

    [HttpPost("designations")]
    public async Task<IActionResult> CreateDesignation([FromBody] DesignationRequest request, CancellationToken cancellationToken)
    {
        if (!await _db.Departments.AnyAsync(item => item.Id == request.DepartmentId, cancellationToken)) return BadRequest(new { message = "Department was not found." });
        var designation = new Designation { Title = request.Title.Trim(), DepartmentId = request.DepartmentId, Level = request.Level };
        _db.Designations.Add(designation); await _db.SaveChangesAsync(cancellationToken); return Ok(new { success = true, data = designation });
    }

    [HttpPut("designations/{id:guid}")]
    public async Task<IActionResult> UpdateDesignation(Guid id, [FromBody] DesignationRequest request, CancellationToken cancellationToken)
    {
        var designation = await _db.Designations.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (designation is null) return NotFound();
        designation.Title = request.Title.Trim(); designation.DepartmentId = request.DepartmentId; designation.Level = request.Level;
        await _db.SaveChangesAsync(cancellationToken); return Ok(new { success = true, data = designation });
    }

    [HttpDelete("designations/{id:guid}")]
    public async Task<IActionResult> DeleteDesignation(Guid id, CancellationToken cancellationToken) => await SoftDelete(_db.Designations.FirstOrDefaultAsync(item => item.Id == id, cancellationToken), cancellationToken);

    [HttpGet("biometric-devices")]
    public async Task<IActionResult> BiometricDevices(CancellationToken cancellationToken) => Ok(new { success = true, data = await _db.BiometricDevices.OrderBy(item => item.DisplayName ?? item.DeviceId).ToListAsync(cancellationToken) });

    [HttpPost("biometric-devices")]
    public async Task<IActionResult> CreateBiometricDevice([FromBody] BiometricDeviceRequest request, CancellationToken cancellationToken)
    {
        var device = new BiometricDevice { DeviceId = request.DeviceId.Trim(), Provider = request.Provider.Trim(), DisplayName = request.DisplayName?.Trim(), ApiUrl = request.ApiUrl?.Trim(), IsActive = request.IsActive };
        _db.BiometricDevices.Add(device); await _db.SaveChangesAsync(cancellationToken); return Ok(new { success = true, data = device });
    }

    [HttpPut("biometric-devices/{id:guid}")]
    public async Task<IActionResult> UpdateBiometricDevice(Guid id, [FromBody] BiometricDeviceRequest request, CancellationToken cancellationToken)
    {
        var device = await _db.BiometricDevices.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (device is null) return NotFound();
        device.DeviceId = request.DeviceId.Trim(); device.Provider = request.Provider.Trim(); device.DisplayName = request.DisplayName?.Trim(); device.ApiUrl = request.ApiUrl?.Trim(); device.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken); return Ok(new { success = true, data = device });
    }

    [HttpDelete("biometric-devices/{id:guid}")]
    public async Task<IActionResult> DeleteBiometricDevice(Guid id, CancellationToken cancellationToken) => await SoftDelete(_db.BiometricDevices.FirstOrDefaultAsync(item => item.Id == id, cancellationToken), cancellationToken);

    private async Task<IActionResult> SoftDelete<T>(Task<T?> query, CancellationToken cancellationToken) where T : AuditableEntity
    {
        var entity = await query; if (entity is null) return NotFound(); entity.IsDeleted = true; entity.DeletedAtUtc = DateTime.UtcNow; await _db.SaveChangesAsync(cancellationToken); return Ok(new { success = true });
    }
}

public record DepartmentRequest(string Name, string Code, Guid? ParentDepartmentId);
public record DesignationRequest(string Title, Guid DepartmentId, int Level);
public record BiometricDeviceRequest(string DeviceId, string Provider, string? DisplayName, string? ApiUrl, bool IsActive);