using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Application.Common.Utilities;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWMS.Application.Tracking.Commands.StopTrackingSession;

public record StopTrackingSessionCommand(
    Guid TrackingSessionId,
    double? EndLatitude,
    double? EndLongitude) : IRequest<Result>;

public class StopTrackingSessionCommandHandler : IRequestHandler<StopTrackingSessionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<StopTrackingSessionCommandHandler> _logger;

    public StopTrackingSessionCommandHandler(
        IApplicationDbContext context, IDateTimeService dateTime, ILogger<StopTrackingSessionCommandHandler> logger)
    {
        _context = context;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result> Handle(StopTrackingSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.TrackingSessions
            .FirstOrDefaultAsync(t => t.Id == request.TrackingSessionId, cancellationToken);

        if (session == null)
            return Result.Failure("Tracking session not found.");

        // Idempotent: the frontend may call stop from more than one place in
        // the same shutdown sequence (explicit Check-Out call, a
        // pagehide/beforeunload beacon, a retry) — treat a second call as a
        // harmless no-op rather than an error.
        if (session.Status == TrackingSessionStatus.Stopped)
        {
            _logger.LogInformation("StopTrackingSession: session {SessionId} was already stopped, no-op.", session.Id);
            return Result.Success();
        }

        var points = await _context.GpsLogs
            .Where(g => g.TrackingSessionId == session.Id)
            .OrderBy(g => g.RecordedAtUtc)
            .Select(g => new { g.Latitude, g.Longitude })
            .ToListAsync(cancellationToken);

        var path = new List<(double Lat, double Lon)> { (session.StartLatitude, session.StartLongitude) };
        path.AddRange(points.Select(p => (p.Latitude, p.Longitude)));

        var endLat = request.EndLatitude ?? path[^1].Lat;
        var endLon = request.EndLongitude ?? path[^1].Lon;
        if (request.EndLatitude.HasValue && request.EndLongitude.HasValue)
            path.Add((endLat, endLon));

        session.EndedAtUtc = _dateTime.UtcNow;
        session.EndLatitude = endLat;
        session.EndLongitude = endLon;
        session.TotalDistanceMeters = Math.Round(GeoDistanceCalculator.TotalPathDistanceMeters(path), 1);
        session.TotalDurationSeconds = Math.Round((session.EndedAtUtc.Value - session.StartedAtUtc).TotalSeconds, 0);
        session.TotalPointsCaptured = points.Count;
        session.Status = TrackingSessionStatus.Stopped;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stopped GPS tracking session {SessionId}: {Points} points, {Distance}m, {Duration}s",
            session.Id, session.TotalPointsCaptured, session.TotalDistanceMeters, session.TotalDurationSeconds);

        return Result.Success();
    }
}
