using EWMS.Application.Common.Exceptions;
using EWMS.Application.Common.Interfaces;
using EWMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Tracking.Queries.GetTrackingHistory;

public record GetTrackingHistoryQuery(Guid AttendanceRecordId) : IRequest<TrackingHistoryDto>;

public class GetTrackingHistoryQueryHandler : IRequestHandler<GetTrackingHistoryQuery, TrackingHistoryDto>
{
    private readonly IApplicationDbContext _context;

    public GetTrackingHistoryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<TrackingHistoryDto> Handle(GetTrackingHistoryQuery request, CancellationToken cancellationToken)
    {
        var trackingSession = await _context.TrackingSessions
            .Include(t => t.Employee)
            .Where(t => t.AttendanceRecordId == request.AttendanceRecordId)
            .FirstOrDefaultAsync(cancellationToken);

        if (trackingSession == null)
            throw new NotFoundException(nameof(TrackingSession), request.AttendanceRecordId);

        // Ensure employee data is available
        if (trackingSession.Employee == null)
            throw new InvalidOperationException($"Employee data not found for tracking session {trackingSession.Id}");

        var session = new TrackingSessionDto
        {
            Id = trackingSession.Id,
            EmployeeId = trackingSession.EmployeeId,
            EmployeeName = (trackingSession.Employee.FirstName ?? "Unknown") + " " + (trackingSession.Employee.LastName ?? "Employee"),
            AttendanceRecordId = trackingSession.AttendanceRecordId,
            StartedAtUtc = trackingSession.StartedAtUtc,
            StartLatitude = trackingSession.StartLatitude,
            StartLongitude = trackingSession.StartLongitude,
            DeviceInfo = trackingSession.DeviceInfo,
            EndedAtUtc = trackingSession.EndedAtUtc,
            EndLatitude = trackingSession.EndLatitude,
            EndLongitude = trackingSession.EndLongitude,
            TotalDistanceMeters = trackingSession.TotalDistanceMeters,
            TotalDurationSeconds = trackingSession.TotalDurationSeconds,
            TotalPointsCaptured = trackingSession.TotalPointsCaptured,
            DeviceIp = trackingSession.DeviceIp,
            Status = trackingSession.Status.ToString()
        };

        var points = await _context.GpsLogs
            .Where(g => g.TrackingSessionId == session.Id)
            .OrderBy(g => g.RecordedAtUtc)
            .Select(g => new LocationPointDto
            {
                Id = g.Id,
                Latitude = g.Latitude,
                Longitude = g.Longitude,
                AccuracyMeters = g.AccuracyMeters,
                SpeedKmh = g.SpeedKmh,
                Heading = g.Heading,
                BatteryPercent = g.BatteryPercent,
                IsMockLocation = g.IsMockLocation,
                RecordedAtUtc = g.RecordedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new TrackingHistoryDto { Session = session, Points = points };
    }
}
