using EWMS.Domain.Common;
using EWMS.Domain.Enums;

namespace EWMS.Domain.Entities;

public class AttendanceRecord : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public DateOnly AttendanceDate { get; set; }

    public DateTime? CheckInAtUtc { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public string? CheckInAddress { get; set; }

    /// <summary>
    /// Biometric/identity verification reference for check-in.
    /// </summary>
    public string? CheckInVerificationReference { get; set; }

    /// <summary>
    /// Device ID where check-in biometric was scanned.
    /// </summary>
    public string? CheckInDeviceId { get; set; }

    public DateTime? CheckOutAtUtc { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public string? CheckOutAddress { get; set; }

    /// <summary>
    /// Biometric/identity verification reference for check-out.
    /// </summary>
    public string? CheckOutVerificationReference { get; set; }

    /// <summary>
    /// Device ID where check-out biometric was scanned.
    /// </summary>
    public string? CheckOutDeviceId { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.PendingApproval;
    public decimal? TotalHours { get; set; }
    public bool IsMockLocationSuspected { get; set; }
    public string? Remarks { get; set; }

    public ICollection<GpsLog> GpsLogs { get; set; } = new List<GpsLog>();
}
