using System.Linq.Expressions;
using EWMS.Domain.Entities;

namespace EWMS.Application.Attendance;

public class AttendanceDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = default!;
    public DateOnly AttendanceDate { get; set; }
    public DateTime? CheckInAtUtc { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public DateTime? CheckOutAtUtc { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public string Status { get; set; } = default!;
    public decimal? TotalHours { get; set; }
    public bool IsMockLocationSuspected { get; set; }
    // BUG FIX: Remarks existed on the AttendanceRecord entity/table but was
    // never surfaced here, so it was silently dropped from every API response
    // even though the frontend/DB fully supported it.
    public string? Remarks { get; set; }

    // Single reusable EF Core projection. Previously this Select(...) shape
    // was hand-copied inside GetTodayStatusQuery and GetAttendanceHistoryQuery
    // (and would have been copied a third time for the new GetAttendanceById/
    // Create/Update handlers), which is exactly the kind of duplicate
    // implementation this fix pass was asked to remove. Every attendance
    // query now uses this one expression.
    public static Expression<Func<AttendanceRecord, AttendanceDto>> Projection => a => new AttendanceDto
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId,
        EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
        AttendanceDate = a.AttendanceDate,
        CheckInAtUtc = a.CheckInAtUtc,
        CheckInLatitude = a.CheckInLatitude,
        CheckInLongitude = a.CheckInLongitude,
        CheckOutAtUtc = a.CheckOutAtUtc,
        CheckOutLatitude = a.CheckOutLatitude,
        CheckOutLongitude = a.CheckOutLongitude,
        Status = a.Status.ToString(),
        TotalHours = a.TotalHours,
        IsMockLocationSuspected = a.IsMockLocationSuspected,
        Remarks = a.Remarks
    };
}
