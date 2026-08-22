using EWMS.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Employees.Commands.CreateEmployee;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    private readonly IApplicationDbContext? _context;

    public CreateEmployeeCommandValidator(IApplicationDbContext? context = null)
    {
        _context = context;

        RuleFor(x => x.EmployeeCode)
            .NotEmpty().WithMessage("Employee code is required.")
            .MaximumLength(20);

        if (_context is not null)
        {
            RuleFor(x => x.EmployeeCode)
                .MustAsync(BeUniqueEmployeeCode).WithMessage("An employee with this code already exists.");
        }

        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.").MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.").MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);

        if (_context is not null)
        {
            RuleFor(x => x.Email)
                .MustAsync(BeUniqueEmail).WithMessage("An employee with this email already exists.");
        }

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits.");

        if (_context is not null)
        {
            RuleFor(x => x.PhoneNumber)
                .MustAsync(BeUniquePhoneNumber).WithMessage("An employee with this phone number already exists.");
        }

        RuleFor(x => x.AadhaarNumber)
            .NotEmpty().WithMessage("Aadhaar number is required.")
            .Matches("^[2-9][0-9]{11}$").WithMessage("Aadhaar number must be a valid 12-digit number.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Gender is required.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThanOrEqualTo(x => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Date of birth cannot be a future date.")
            .LessThan(x => x.DateOfJoining)
                .WithMessage("Date of birth must be before date of joining.");

        RuleFor(x => x.DateOfJoining)
            .NotEmpty().WithMessage("Date of joining is required.");

        RuleFor(x => x.DepartmentId).NotEmpty().WithMessage("Department is required.");
        RuleFor(x => x.DesignationId).NotEmpty().WithMessage("Designation is required.");

        if (_context is not null)
        {
            // Cross-field rule: the chosen designation must actually belong to the
            // chosen department. Runs only once both ids are present so it doesn't
            // pile a confusing second error on top of the NotEmpty rules above.
            RuleFor(x => x)
                .MustAsync(DesignationBelongsToDepartment)
                .WithMessage("The selected designation does not belong to the selected department.")
                .WithName("DesignationId")
                .When(x => x.DepartmentId != Guid.Empty && x.DesignationId != Guid.Empty);
        }
    }

    private async Task<bool> BeUniqueEmployeeCode(string employeeCode, CancellationToken cancellationToken) =>
        !await _context.Employees.AnyAsync(e => e.EmployeeCode == employeeCode, cancellationToken);

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken) =>
        !await _context.Employees.AnyAsync(e => e.Email == email, cancellationToken);

    private async Task<bool> BeUniquePhoneNumber(string phoneNumber, CancellationToken cancellationToken) =>
        !await _context.Employees.AnyAsync(e => e.PhoneNumber == phoneNumber, cancellationToken);

    private async Task<bool> DesignationBelongsToDepartment(CreateEmployeeCommand command, CancellationToken cancellationToken) =>
        await _context.Designations.AnyAsync(
            d => d.Id == command.DesignationId && d.DepartmentId == command.DepartmentId, cancellationToken);
}
