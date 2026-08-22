using EWMS.Application.Common.Exceptions;
using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using MediatR;

namespace EWMS.Application.Attendance.Commands.DeleteAttendance;

public record DeleteAttendanceCommand(Guid Id) : IRequest<Result>;

public class DeleteAttendanceCommandHandler : IRequestHandler<DeleteAttendanceCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteAttendanceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteAttendanceCommand request, CancellationToken cancellationToken)
    {
        var record = await _context.AttendanceRecords.FindAsync(new object[] { request.Id }, cancellationToken);
        if (record == null)
            throw new NotFoundException(nameof(AttendanceRecord), request.Id);

        // Soft delete, consistent with DeleteEmployeeCommand — AttendanceRecord
        // already carries IsDeleted/DeletedAtUtc (AuditableEntity) and already
        // has a global query filter (see ApplicationDbContext.OnModelCreating),
        // so this row simply stops appearing in any query from this point on.
        record.IsDeleted = true;
        record.DeletedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
