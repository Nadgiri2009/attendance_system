using EWMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Attendance.Queries.GetTodayStatus;

public record GetTodayStatusQuery(Guid EmployeeId) : IRequest<AttendanceDto?>;

public class GetTodayStatusQueryHandler : IRequestHandler<GetTodayStatusQuery, AttendanceDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTime;

    public GetTodayStatusQueryHandler(IApplicationDbContext context, IDateTimeService dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<AttendanceDto?> Handle(GetTodayStatusQuery request, CancellationToken cancellationToken)
    {
        var today = _dateTime.TodayUtc;

        return await _context.AttendanceRecords
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == request.EmployeeId && a.AttendanceDate == today)
            .Select(AttendanceDto.Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
