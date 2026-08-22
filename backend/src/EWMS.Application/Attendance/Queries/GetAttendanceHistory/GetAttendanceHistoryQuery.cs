using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Attendance.Queries.GetAttendanceHistory;

public record GetAttendanceHistoryQuery(
    Guid? EmployeeId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Search,
    AttendanceStatus? Status,
    string? SortBy = null,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 30) : IRequest<PaginatedList<AttendanceDto>>;

public class GetAttendanceHistoryQueryHandler : IRequestHandler<GetAttendanceHistoryQuery, PaginatedList<AttendanceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAttendanceHistoryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PaginatedList<AttendanceDto>> Handle(GetAttendanceHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AttendanceRecords
            .Include(a => a.Employee)
            .AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(a => a.AttendanceDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.AttendanceDate <= request.ToDate.Value);

        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(a =>
                a.Employee.FirstName.ToLower().Contains(term) ||
                a.Employee.LastName.ToLower().Contains(term) ||
                a.Employee.EmployeeCode.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(AttendanceDto.Projection)
            .ToListAsync(cancellationToken);

        return new PaginatedList<AttendanceDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private static IQueryable<AttendanceRecord> ApplySorting(IQueryable<AttendanceRecord> query, string? sortBy, bool descending)
    {
        Func<IQueryable<AttendanceRecord>, IOrderedQueryable<AttendanceRecord>> orderBy = sortBy?.ToLowerInvariant() switch
        {
            "employeename" => q => descending
                ? q.OrderByDescending(a => a.Employee.FirstName).ThenByDescending(a => a.Employee.LastName)
                : q.OrderBy(a => a.Employee.FirstName).ThenBy(a => a.Employee.LastName),
            "status" => q => descending ? q.OrderByDescending(a => a.Status) : q.OrderBy(a => a.Status),
            "totalhours" => q => descending ? q.OrderByDescending(a => a.TotalHours) : q.OrderBy(a => a.TotalHours),
            "checkinatutc" => q => descending ? q.OrderByDescending(a => a.CheckInAtUtc) : q.OrderBy(a => a.CheckInAtUtc),
            _ => q => descending ? q.OrderByDescending(a => a.AttendanceDate) : q.OrderBy(a => a.AttendanceDate)
        };

        return orderBy(query);
    }
}
