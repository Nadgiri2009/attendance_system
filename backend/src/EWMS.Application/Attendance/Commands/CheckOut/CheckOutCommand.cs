using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Application.Tracking.Commands.StopTrackingSession;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWMS.Application.Attendance.Commands.CheckOut;

public record CheckOutCommand(
    Guid EmployeeId,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    bool IsMockLocation,
    string? Address) : IRequest<Result>;

public class CheckOutCommandHandler : IRequestHandler<CheckOutCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTime;
    private readonly ISender _sender;
    private readonly ILogger<CheckOutCommandHandler> _logger;

    public CheckOutCommandHandler(
        IApplicationDbContext context, IDateTimeService dateTime, ISender sender, ILogger<CheckOutCommandHandler> logger)
    {
        _context = context;
        _dateTime = dateTime;
        _sender = sender;
        _logger = logger;
    }

    public async Task<Result> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        var today = _dateTime.TodayUtc;

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.AttendanceDate == today, cancellationToken);

        if (record?.CheckInAtUtc == null)
            return Result.Failure("You must check in before checking out.");

        if (record.CheckOutAtUtc != null)
            return Result.Failure("You have already checked out today.");

        record.CheckOutAtUtc = _dateTime.UtcNow;
        record.CheckOutLatitude = request.Latitude;
        record.CheckOutLongitude = request.Longitude;
        record.CheckOutAddress = request.Address;
        record.TotalHours = (decimal?)Math.Round((record.CheckOutAtUtc.Value - record.CheckInAtUtc.Value).TotalHours, 2);

        _context.GpsLogs.Add(new GpsLog
        {
            EmployeeId = request.EmployeeId,
            AttendanceRecordId = record.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            IsMockLocation = request.IsMockLocation,
            RecordedAtUtc = _dateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Checkout hit a concurrency conflict while saving attendance record {AttendanceId}", record.Id);
            return Result.Failure("The attendance record was updated by another request. Please try again.");
        }

        // GPS tracking requirement: "Stop tracking immediately after
        // Check-Out." The frontend calls POST /tracking/stop directly, but
        // that's a second HTTP round trip the client might miss (tab
        // closed, network drop) — so Check-Out also stops it server-side as
        // a safety net. We avoid failing the whole checkout if no active
        // tracking session exists or if it was already stopped.
        var activeSession = await _context.TrackingSessions
            .Where(t => t.AttendanceRecordId == record.Id && t.Status == TrackingSessionStatus.Active)
            .Select(t => new { t.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSession != null)
        {
            var stopResult = await _sender.Send(
                new StopTrackingSessionCommand(activeSession.Id, request.Latitude, request.Longitude), cancellationToken);

            if (!stopResult.Succeeded)
                _logger.LogWarning(
                    "Check-Out succeeded but auto-stopping tracking session {SessionId} failed: {Errors}",
                    activeSession.Id, string.Join("; ", stopResult.Errors));
        }

        return Result.Success();
    }
}
