using EWMS.Application.Common.Exceptions;
using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;

namespace EWMS.Application.Employees.Commands.UpdateEmployee;

public record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    Gender Gender,
    DateOnly DateOfBirth,
    DateOnly DateOfJoining,
    Guid DepartmentId,
    Guid DesignationId,
    Guid? ReportingManagerId,
    bool IsActive) : IRequest<Result>;

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateEmployeeCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FindAsync(new object[] { request.Id }, cancellationToken);
        if (employee == null)
            throw new NotFoundException(nameof(Employee), request.Id);

        // Uniqueness (Email/PhoneNumber, excluding this employee) and the
        // Designation-belongs-to-Department check already ran in
        // UpdateEmployeeCommandValidator via the ValidationBehaviour pipeline.
        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Email = request.Email;
        employee.PhoneNumber = request.PhoneNumber;
        employee.Gender = request.Gender;
        employee.DateOfBirth = request.DateOfBirth;
        employee.DateOfJoining = request.DateOfJoining;
        employee.DepartmentId = request.DepartmentId;
        employee.DesignationId = request.DesignationId;
        employee.ReportingManagerId = request.ReportingManagerId;
        employee.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
