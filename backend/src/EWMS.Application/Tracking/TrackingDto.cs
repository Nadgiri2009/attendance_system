namespace EWMS.Application.Tracking;

public class TrackingSessionDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = default!;
    public Guid AttendanceRecordId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public string? DeviceInfo { get; set; }
    public string? DeviceIp { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public double? EndLatitude { get; set; }
    public double? EndLongitude { get; set; }
    public double? TotalDistanceMeters { get; set; }
    public double? TotalDurationSeconds { get; set; }
    public int TotalPointsCaptured { get; set; }
    public string Status { get; set; } = default!;
}

public class LocationPointDto
{
    public Guid Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? SpeedKmh { get; set; }
    public double? Heading { get; set; }
    public double? BatteryPercent { get; set; }
    public bool IsMockLocation { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}

// Response for GET /api/tracking/history/{attendanceId} — the session
// summary plus every point captured during it, in order (for polyline +
// playback rendering on the frontend).
public class TrackingHistoryDto
{
    public TrackingSessionDto Session { get; set; } = default!;
    public List<LocationPointDto> Points { get; set; } = new();
}

// Response for GET /api/tracking/live/{employeeId} — null-friendly summary
// of "where is this employee right now", or IsActive=false if they have no
// running tracking session.
public class LiveLocationDto
{
    public bool IsActive { get; set; }
    public Guid? TrackingSessionId { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    public double? LastSpeedKmh { get; set; }
    public double? LastBatteryPercent { get; set; }
    public DateTime? LastRecordedAtUtc { get; set; }
    public int PointsCapturedSoFar { get; set; }
}
