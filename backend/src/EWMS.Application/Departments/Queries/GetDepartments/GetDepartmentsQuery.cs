using EWMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Departments.Queries.GetDepartments;

public record DepartmentDto(Guid Id, string Name, string Code, Guid? ParentDepartmentId);

public record GetDepartmentsQuery : IRequest<List<DepartmentDto>>;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, List<DepartmentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Departments
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Code, d.ParentDepartmentId))
            .ToListAsync(cancellationToken);
    }
}
