using AutoMapper;
using AutoMapper.QueryableExtensions;
using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Employees.Queries.GetEmployees;

public record GetEmployeesQuery(
    string? Search,
    Guid? DepartmentId,
    bool? IsActive,
    string? SortBy = null,
    bool SortDescending = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<EmployeeDto>>;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, PaginatedList<EmployeeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEmployeesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.ReportingManager)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.EmployeeCode.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term));
        }

        if (request.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(e => e.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<EmployeeDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PaginatedList<EmployeeDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    // Explicit allow-list of sortable columns (rather than a dynamic-LINQ
    // string) keeps this safe from injection and keeps the sortable set in
    // sync with what EmployeeDto/the Employees grid actually displays.
    private static IQueryable<Domain.Entities.Employee> ApplySorting(
        IQueryable<Domain.Entities.Employee> query, string? sortBy, bool descending)
    {
        Func<IQueryable<Domain.Entities.Employee>, IOrderedQueryable<Domain.Entities.Employee>> orderBy = sortBy?.ToLowerInvariant() switch
        {
            "employeecode" => q => descending ? q.OrderByDescending(e => e.EmployeeCode) : q.OrderBy(e => e.EmployeeCode),
            "lastname" => q => descending ? q.OrderByDescending(e => e.LastName) : q.OrderBy(e => e.LastName),
            "email" => q => descending ? q.OrderByDescending(e => e.Email) : q.OrderBy(e => e.Email),
            "department" or "departmentname" => q => descending ? q.OrderByDescending(e => e.Department.Name) : q.OrderBy(e => e.Department.Name),
            "designation" or "designationtitle" => q => descending ? q.OrderByDescending(e => e.Designation.Title) : q.OrderBy(e => e.Designation.Title),
            "dateofjoining" => q => descending ? q.OrderByDescending(e => e.DateOfJoining) : q.OrderBy(e => e.DateOfJoining),
            _ => q => descending ? q.OrderByDescending(e => e.FirstName) : q.OrderBy(e => e.FirstName)
        };

        return orderBy(query);
    }
}
