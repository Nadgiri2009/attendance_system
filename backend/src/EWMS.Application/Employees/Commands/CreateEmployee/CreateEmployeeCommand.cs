using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EWMS.Application.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string AadhaarNumber,
    Gender Gender,
    DateOnly DateOfBirth,
    DateOnly DateOfJoining,
    Guid DepartmentId,
    Guid DesignationId,
    Guid? ReportingManagerId) : IRequest<Result<Guid>>;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateEmployeeCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Uniqueness (EmployeeCode/Email/PhoneNumber) and the Designation-belongs-
        // to-Department check already ran in CreateEmployeeCommandValidator via
        // the MediatR ValidationBehaviour pipeline before this handler is ever
        // invoked — see that file for the single source of truth on those rules.
        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            AadhaarNumber = request.AadhaarNumber,
            AadhaarLast8Hash = SHA256.HashData(Encoding.UTF8.GetBytes(request.AadhaarNumber[^8..])),
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            DateOfJoining = request.DateOfJoining,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            ReportingManagerId = request.ReportingManagerId,
            IsActive = true
        };

        _context.Employees.Add(employee);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(employee.Id);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                if (sqlEx.Message.Contains("IX_Employees_Email", StringComparison.OrdinalIgnoreCase))
                    return Result<Guid>.Failure("An employee with this email already exists.");

                if (sqlEx.Message.Contains("IX_Employees_EmployeeCode", StringComparison.OrdinalIgnoreCase))
                    return Result<Guid>.Failure("An employee with this code already exists.");

                if (sqlEx.Message.Contains("IX_Employees_PhoneNumber", StringComparison.OrdinalIgnoreCase))
                    return Result<Guid>.Failure("An employee with this phone number already exists.");

                return Result<Guid>.Failure("An employee with this information already exists.");
            }

            throw;
        }
    }
}
