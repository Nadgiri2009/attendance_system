using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Attendance.Commands.CreateAttendance;

// Manual attendance entry (e.g. HR back-filling a missed punch or recording
// attendance for an employee who doesn't self-check-in via GPS). Distinct
// from CheckInCommand, which is the employee-facing "today, for myself" flow.
public record CreateAttendanceCommand(
    Guid EmployeeId,
    DateOnly AttendanceDate,
    DateTime CheckInAtUtc,
    DateTime? CheckOutAtUtc,
    AttendanceStatus Status,
    string? Remarks) : IRequest<Result<Guid>>;

public class CreateAttendanceCommandHandler : IRequestHandler<CreateAttendanceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTime;

    public CreateAttendanceCommandHandler(IApplicationDbContext context, IDateTimeService dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<Result<Guid>> Handle(CreateAttendanceCommand request, CancellationToken cancellationToken)
    {
        // Employee existence, the future-date check, check-out > check-in,
        // and the duplicate employee+date rule all ran in
        // CreateAttendanceCommandValidator before this handler runs.
        var record = new AttendanceRecord
        {
            EmployeeId = request.EmployeeId,
            AttendanceDate = request.AttendanceDate,
            CheckInAtUtc = request.CheckInAtUtc,
            CheckOutAtUtc = request.CheckOutAtUtc,
            Status = request.Status,
            Remarks = request.Remarks,
            TotalHours = request.CheckOutAtUtc.HasValue
                ? (decimal?)Math.Round((request.CheckOutAtUtc.Value - request.CheckInAtUtc).TotalHours, 2)
                : null
        };

        _context.AttendanceRecords.Add(record);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            
            // Create a tracking session for manual attendance records so they can
            // have GPS route history if needed.
            var trackingSession = new TrackingSession
            {
                EmployeeId = request.EmployeeId,
                AttendanceRecordId = record.Id,
                StartLatitude = 0,
                StartLongitude = 0,
                StartedAtUtc = request.CheckInAtUtc,
                Status = request.CheckOutAtUtc.HasValue ? TrackingSessionStatus.Stopped : TrackingSessionStatus.Active
            };
            
            if (request.CheckOutAtUtc.HasValue)
            {
                trackingSession.EndedAtUtc = request.CheckOutAtUtc;
                trackingSession.EndLatitude = 0;
                trackingSession.EndLongitude = 0;
                trackingSession.TotalDurationSeconds = (long)Math.Round((request.CheckOutAtUtc.Value - request.CheckInAtUtc).TotalSeconds);
            }
            
            _context.TrackingSessions.Add(trackingSession);
            await _context.SaveChangesAsync(cancellationToken);
            
            return Result<Guid>.Success(record.Id);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                sqlEx.Number == 2601 &&
                sqlEx.Message.Contains("IX_AttendanceRecords_EmployeeId_AttendanceDate", StringComparison.OrdinalIgnoreCase))
            {
                return Result<Guid>.Failure("An attendance record already exists for this employee on this date.");
            }

            throw;
        }
    }
}
