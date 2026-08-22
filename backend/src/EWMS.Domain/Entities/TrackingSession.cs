using EWMS.Domain.Common;
using EWMS.Domain.Enums;

namespace EWMS.Domain.Entities;

// One TrackingSession per Check-In. Continuous location points captured
// while it's Active are stored as GpsLog rows referencing this session's Id
// (GpsLog.TrackingSessionId) — see docs/GPS_TRACKING.md for why this reuses
// the existing GpsLog table instead of introducing a duplicate table.
public class TrackingSession : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public Guid AttendanceRecordId { get; set; }
    public AttendanceRecord AttendanceRecord { get; set; } = default!;

    public DateTime StartedAtUtc { get; set; }
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public double? StartAccuracyMeters { get; set; }
    public double? StartBatteryPercent { get; set; }
    public string? DeviceInfo { get; set; }
    public string? DeviceIp { get; set; }

    public DateTime? EndedAtUtc { get; set; }
    public double? EndLatitude { get; set; }
    public double? EndLongitude { get; set; }

    public double? TotalDistanceMeters { get; set; }
    public double? TotalDurationSeconds { get; set; }
    public int TotalPointsCaptured { get; set; }

    public TrackingSessionStatus Status { get; set; } = TrackingSessionStatus.Active;

    public ICollection<GpsLog> LocationPoints { get; set; } = new List<GpsLog>();
}
