using EWMS.Application.Common.Exceptions;
using EWMS.Application.Common.Interfaces;
using EWMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Attendance.Queries.GetAttendanceById;

public record GetAttendanceByIdQuery(Guid Id) : IRequest<AttendanceDto>;

public class GetAttendanceByIdQueryHandler : IRequestHandler<GetAttendanceByIdQuery, AttendanceDto>
{
    private readonly IApplicationDbContext _context;

    public GetAttendanceByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<AttendanceDto> Handle(GetAttendanceByIdQuery request, CancellationToken cancellationToken)
    {
        var attendance = await _context.AttendanceRecords
            .Include(a => a.Employee)
            .Where(a => a.Id == request.Id)
            .Select(AttendanceDto.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (attendance == null)
            throw new NotFoundException(nameof(AttendanceRecord), request.Id);

        return attendance;
    }
}
