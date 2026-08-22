using EWMS.Application.Common.Exceptions;
using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;

namespace EWMS.Application.Attendance.Commands.UpdateAttendance;

public record UpdateAttendanceCommand(
    Guid Id,
    DateTime CheckInAtUtc,
    DateTime? CheckOutAtUtc,
    AttendanceStatus Status,
    string? Remarks) : IRequest<Result>;

public class UpdateAttendanceCommandHandler : IRequestHandler<UpdateAttendanceCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateAttendanceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateAttendanceCommand request, CancellationToken cancellationToken)
    {
        var record = await _context.AttendanceRecords.FindAsync(new object[] { request.Id }, cancellationToken);
        if (record == null)
            throw new NotFoundException(nameof(AttendanceRecord), request.Id);

        record.CheckInAtUtc = request.CheckInAtUtc;
        record.CheckOutAtUtc = request.CheckOutAtUtc;
        record.Status = request.Status;
        record.Remarks = request.Remarks;
        record.TotalHours = request.CheckOutAtUtc.HasValue
            ? (decimal?)Math.Round((request.CheckOutAtUtc.Value - request.CheckInAtUtc).TotalHours, 2)
            : null;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
