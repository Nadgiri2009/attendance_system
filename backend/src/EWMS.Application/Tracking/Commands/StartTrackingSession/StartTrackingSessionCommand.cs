using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWMS.Application.Tracking.Commands.StartTrackingSession;

public record StartTrackingSessionCommand(
    Guid EmployeeId,
    Guid AttendanceRecordId,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    double? BatteryPercent,
    string? DeviceInfo,
    string? DeviceIp) : IRequest<Result<Guid>>;

public class StartTrackingSessionCommandHandler : IRequestHandler<StartTrackingSessionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTime;
    private readonly ILogger<StartTrackingSessionCommandHandler> _logger;

    public StartTrackingSessionCommandHandler(
        IApplicationDbContext context, IDateTimeService dateTime, ILogger<StartTrackingSessionCommandHandler> logger)
    {
        _context = context;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(StartTrackingSessionCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: if the frontend calls /tracking/start twice for the
        // same attendance (e.g. a page reload right after Check-In), return
        // the existing active session instead of erroring or double-creating
        // — this is also what "Prevent duplicate tracking sessions" means in
        // practice from the client's point of view.
        var existing = await _context.TrackingSessions
            .FirstOrDefaultAsync(t => t.AttendanceRecordId == request.AttendanceRecordId, cancellationToken);

        if (existing is { Status: TrackingSessionStatus.Active })
        {
            _logger.LogInformation(
                "StartTrackingSession: reusing existing active session {SessionId} for attendance {AttendanceId}",
                existing.Id, request.AttendanceRecordId);
            return Result<Guid>.Success(existing.Id);
        }

        if (existing is { Status: TrackingSessionStatus.Stopped })
            return Result<Guid>.Failure("Tracking has already been completed for this attendance record.");

        // Employee-checked-in / not-checked-out-yet and one-active-session
        // rules already ran in StartTrackingSessionCommandValidator.
        var session = new TrackingSession
        {
            EmployeeId = request.EmployeeId,
            AttendanceRecordId = request.AttendanceRecordId,
            StartedAtUtc = _dateTime.UtcNow,
            StartLatitude = request.Latitude,
            StartLongitude = request.Longitude,
            StartAccuracyMeters = request.AccuracyMeters,
            StartBatteryPercent = request.BatteryPercent,
            DeviceInfo = request.DeviceInfo,
            DeviceIp = request.DeviceIp,
            Status = TrackingSessionStatus.Active
        };

        _context.TrackingSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Started GPS tracking session {SessionId} for employee {EmployeeId}, attendance {AttendanceId}",
            session.Id, request.EmployeeId, request.AttendanceRecordId);

        return Result<Guid>.Success(session.Id);
    }
}
