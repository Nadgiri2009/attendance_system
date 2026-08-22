using EWMS.Domain.Common;

namespace EWMS.Domain.Entities;

public class GpsLog : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid? AttendanceRecordId { get; set; }
    public AttendanceRecord? AttendanceRecord { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? SpeedKmh { get; set; }
    public double? Heading { get; set; }
    public double? BatteryPercent { get; set; }
    public bool IsMockLocation { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    // Nullable: check-in/check-out already write one ad-hoc GpsLog row each
    // (see CheckInCommand/CheckOutCommand) that isn't part of a continuous
    // tracking session. Points captured by the continuous GPS tracking
    // feature (TrackingSession) always set this.
    public Guid? TrackingSessionId { get; set; }
    public TrackingSession? TrackingSession { get; set; }
}
