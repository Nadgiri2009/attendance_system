using EWMS.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Tracking.Commands.StartTrackingSession;

public class StartTrackingSessionCommandValidator : AbstractValidator<StartTrackingSessionCommand>
{
    private readonly IApplicationDbContext _context;

    public StartTrackingSessionCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("Employee is required.");
        RuleFor(x => x.AttendanceRecordId).NotEmpty().WithMessage("Attendance record is required.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DeviceInfo).MaximumLength(500);

        // "Do not allow tracking before Check-In" / "Stop tracking
        // immediately after Check-Out": the attendance record must exist,
        // belong to this employee, be checked in, and not yet checked out.
        RuleFor(x => x)
            .MustAsync(AttendanceIsOpenForThisEmployee)
            .WithMessage("Tracking can only be started for your own currently checked-in attendance record.")
            .WithName("AttendanceRecordId");

        // "Prevent multiple active tracking sessions for the same employee."
        // (StartTrackingSessionCommandHandler additionally treats a repeat
        // call for the *same* attendance record as idempotent rather than an
        // error — this rule catches a genuinely different open session,
        // e.g. a stale session from another device/tab.)
        RuleFor(x => x)
            .MustAsync(NoOtherActiveSessionForEmployee)
            .WithMessage("An active tracking session already exists for this employee.")
            .WithName("EmployeeId");
    }

    private async Task<bool> AttendanceIsOpenForThisEmployee(StartTrackingSessionCommand command, CancellationToken cancellationToken) =>
        await _context.AttendanceRecords.AnyAsync(
            a => a.Id == command.AttendanceRecordId
                 && a.EmployeeId == command.EmployeeId
                 && a.CheckInAtUtc != null
                 && a.CheckOutAtUtc == null,
            cancellationToken);

    private async Task<bool> NoOtherActiveSessionForEmployee(StartTrackingSessionCommand command, CancellationToken cancellationToken) =>
        !await _context.TrackingSessions.AnyAsync(
            t => t.EmployeeId == command.EmployeeId
                 && t.Status == Domain.Enums.TrackingSessionStatus.Active
                 && t.AttendanceRecordId != command.AttendanceRecordId,
            cancellationToken);
}
