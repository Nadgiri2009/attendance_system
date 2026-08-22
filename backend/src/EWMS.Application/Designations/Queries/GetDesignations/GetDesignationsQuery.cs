using EWMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Designations.Queries.GetDesignations;

public record DesignationDto(Guid Id, string Title, Guid DepartmentId, int Level);

// Optional DepartmentId filter powers the cascading Department -> Designation
// dropdown on the Employee form (a designation must belong to the selected
// department per the Employee validation rules).
public record GetDesignationsQuery(Guid? DepartmentId) : IRequest<List<DesignationDto>>;

public class GetDesignationsQueryHandler : IRequestHandler<GetDesignationsQuery, List<DesignationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDesignationsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<DesignationDto>> Handle(GetDesignationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Designations.AsQueryable();

        if (request.DepartmentId.HasValue)
            query = query.Where(d => d.DepartmentId == request.DepartmentId.Value);

        return await query
            .OrderBy(d => d.Title)
            .Select(d => new DesignationDto(d.Id, d.Title, d.DepartmentId, d.Level))
            .ToListAsync(cancellationToken);
    }
}
