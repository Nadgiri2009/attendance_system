using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Attendance.Commands.CheckIn;

public record CheckInCommand(
    Guid EmployeeId,
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    bool IsMockLocation,
    string? Address) : IRequest<Result<Guid>>;

public class CheckInCommandHandler : IRequestHandler<CheckInCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTime;

    public CheckInCommandHandler(IApplicationDbContext context, IDateTimeService dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<Result<Guid>> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var today = _dateTime.TodayUtc;

        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.AttendanceDate == today, cancellationToken);

        if (existing is { CheckInAtUtc: not null })
            return Result<Guid>.Failure("You have already checked in today.");

        var record = existing ?? new AttendanceRecord
        {
            EmployeeId = request.EmployeeId,
            AttendanceDate = today
        };

        record.CheckInAtUtc = _dateTime.UtcNow;
        record.CheckInLatitude = request.Latitude;
        record.CheckInLongitude = request.Longitude;
        record.CheckInAddress = request.Address;
        record.IsMockLocationSuspected = request.IsMockLocation;
        record.Status = AttendanceStatus.Present;

        record.GpsLogs.Add(new GpsLog
        {
            EmployeeId = request.EmployeeId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            IsMockLocation = request.IsMockLocation,
            RecordedAtUtc = _dateTime.UtcNow
        });

        if (existing == null)
            _context.AttendanceRecords.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            
            // Auto-start tracking session for this check-in
            var trackingSession = new TrackingSession
            {
                EmployeeId = request.EmployeeId,
                AttendanceRecordId = record.Id,
                StartLatitude = request.Latitude,
                StartLongitude = request.Longitude,
                StartAccuracyMeters = request.AccuracyMeters,
                StartedAtUtc = _dateTime.UtcNow,
                Status = TrackingSessionStatus.Active
            };
            
            _context.TrackingSessions.Add(trackingSession);
            await _context.SaveChangesAsync(cancellationToken);
            
            return Result<Guid>.Success(record.Id);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            // Convert duplicate attendance record inserts into a user-friendly
            // validation failure rather than a generic server error.
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                sqlEx.Number == 2601 &&
                sqlEx.Message.Contains("IX_AttendanceRecords_EmployeeId_AttendanceDate", StringComparison.OrdinalIgnoreCase))
            {
                return Result<Guid>.Failure("You have already checked in today.");
            }

            throw;
        }
    }
}

