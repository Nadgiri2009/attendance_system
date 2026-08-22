using FluentValidation;

namespace EWMS.Application.Attendance.Commands.UpdateAttendance;

public class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
{
    public UpdateAttendanceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.CheckInAtUtc).NotEmpty().WithMessage("Check-in time is required.");

        RuleFor(x => x.CheckOutAtUtc)
            .GreaterThan(x => x.CheckInAtUtc)
                .WithMessage("Check-out time must be greater than check-in time.")
            .When(x => x.CheckOutAtUtc.HasValue);

        RuleFor(x => x.Status).IsInEnum().WithMessage("Attendance status is required.");

        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
