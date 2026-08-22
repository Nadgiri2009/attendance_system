using EWMS.Application.Common.Exceptions;
using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using MediatR;

namespace EWMS.Application.Employees.Commands.DeleteEmployee;

public record DeleteEmployeeCommand(Guid Id) : IRequest<Result>;

public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteEmployeeCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FindAsync(new object[] { request.Id }, cancellationToken);
        if (employee == null)
            throw new NotFoundException(nameof(Employee), request.Id);

        // Soft delete
        employee.IsDeleted = true;
        employee.DeletedAtUtc = DateTime.UtcNow;
        employee.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
