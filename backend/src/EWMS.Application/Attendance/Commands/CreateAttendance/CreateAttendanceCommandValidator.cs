using EWMS.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Attendance.Commands.CreateAttendance;

public class CreateAttendanceCommandValidator : AbstractValidator<CreateAttendanceCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateAttendanceCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee is required.")
            .MustAsync(EmployeeExists).WithMessage("The selected employee does not exist.");

        RuleFor(x => x.AttendanceDate)
            .NotEmpty().WithMessage("Attendance date is required.")
            .LessThanOrEqualTo(x => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Attendance date cannot be a future date.");

        RuleFor(x => x.CheckInAtUtc)
            .NotEmpty().WithMessage("Check-in time is required.");

        RuleFor(x => x.CheckOutAtUtc)
            .GreaterThan(x => x.CheckInAtUtc)
                .WithMessage("Check-out time must be greater than check-in time.")
            .When(x => x.CheckOutAtUtc.HasValue);

        RuleFor(x => x.Status).IsInEnum().WithMessage("Attendance status is required.");

        RuleFor(x => x.Remarks).MaximumLength(1000);

        // Prevent duplicate attendance for the same employee and date.
        RuleFor(x => x)
            .MustAsync(NotBeDuplicate)
            .WithMessage("An attendance record already exists for this employee on this date.")
            .WithName("AttendanceDate")
            .When(x => x.EmployeeId != Guid.Empty);
    }

    private async Task<bool> EmployeeExists(Guid employeeId, CancellationToken cancellationToken) =>
        await _context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);

    private async Task<bool> NotBeDuplicate(CreateAttendanceCommand command, CancellationToken cancellationToken) =>
        !await _context.AttendanceRecords.AnyAsync(
            a => a.EmployeeId == command.EmployeeId && a.AttendanceDate == command.AttendanceDate, cancellationToken);
}
