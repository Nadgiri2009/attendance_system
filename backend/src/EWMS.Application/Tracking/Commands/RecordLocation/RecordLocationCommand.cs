using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Tracking.Commands.RecordLocation;

public record RecordLocationCommand(
    Guid TrackingSessionId,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    double? SpeedKmh,
    double? Heading,
    double? BatteryPercent,
    bool IsMockLocation,
    DateTime? RecordedAtUtc) : IRequest<Result>;

public class RecordLocationCommandHandler : IRequestHandler<RecordLocationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTime;

    public RecordLocationCommandHandler(IApplicationDbContext context, IDateTimeService dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(RecordLocationCommand request, CancellationToken cancellationToken)
    {
        // "Tracking session must be Active" already ran in the validator.
        var session = await _context.TrackingSessions
            .FirstOrDefaultAsync(t => t.Id == request.TrackingSessionId, cancellationToken);

        if (session == null)
            return Result.Failure("Tracking session not found.");

        _context.GpsLogs.Add(new GpsLog
        {
            EmployeeId = session.EmployeeId,
            TrackingSessionId = session.Id,
            AttendanceRecordId = session.AttendanceRecordId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            SpeedKmh = request.SpeedKmh,
            Heading = request.Heading,
            BatteryPercent = request.BatteryPercent,
            IsMockLocation = request.IsMockLocation,
            // Prefer the client-captured timestamp (points may be queued
            // offline and synced later) but fall back to server time.
            RecordedAtUtc = request.RecordedAtUtc ?? _dateTime.UtcNow
        });

        session.TotalPointsCaptured += 1;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
