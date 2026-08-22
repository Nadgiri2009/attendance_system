using EWMS.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Employees.Commands.UpdateEmployee;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateEmployeeCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.").MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.").MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256)
            .MustAsync(BeUniqueEmail).WithMessage("Another employee with this email already exists.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits.")
            .MustAsync(BeUniquePhoneNumber).WithMessage("Another employee with this phone number already exists.");

        RuleFor(x => x.Gender).IsInEnum().WithMessage("Gender is required.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThanOrEqualTo(x => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Date of birth cannot be a future date.")
            .LessThan(x => x.DateOfJoining)
                .WithMessage("Date of birth must be before date of joining.");

        RuleFor(x => x.DateOfJoining).NotEmpty().WithMessage("Date of joining is required.");

        RuleFor(x => x.DepartmentId).NotEmpty().WithMessage("Department is required.");
        RuleFor(x => x.DesignationId).NotEmpty().WithMessage("Designation is required.");

        RuleFor(x => x)
            .MustAsync(DesignationBelongsToDepartment)
            .WithMessage("The selected designation does not belong to the selected department.")
            .WithName("DesignationId")
            .When(x => x.DepartmentId != Guid.Empty && x.DesignationId != Guid.Empty);
    }

    private async Task<bool> BeUniqueEmail(UpdateEmployeeCommand command, string email, CancellationToken cancellationToken) =>
        !await _context.Employees.AnyAsync(e => e.Email == email && e.Id != command.Id, cancellationToken);

    private async Task<bool> BeUniquePhoneNumber(UpdateEmployeeCommand command, string phoneNumber, CancellationToken cancellationToken) =>
        !await _context.Employees.AnyAsync(e => e.PhoneNumber == phoneNumber && e.Id != command.Id, cancellationToken);

    private async Task<bool> DesignationBelongsToDepartment(UpdateEmployeeCommand command, CancellationToken cancellationToken) =>
        await _context.Designations.AnyAsync(
            d => d.Id == command.DesignationId && d.DepartmentId == command.DepartmentId, cancellationToken);
}
