using EWMS.Application.Common.Interfaces;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Tracking.Queries.GetLiveLocation;

public record GetLiveLocationQuery(Guid EmployeeId) : IRequest<LiveLocationDto>;

// Additive to the /tracking/live/{employeeId} endpoint from the spec: the
// live dashboard (requirement 7) needs to plot *every* currently-tracked
// employee, not just one, so this returns the same LiveLocationDto shape
// for every session that's currently Active. Exposed as GET
// /api/tracking/live (no id) in TrackingController.
public record GetAllLiveLocationsQuery : IRequest<List<LiveLocationDto>>;

public class GetLiveLocationQueryHandler :
    IRequestHandler<GetLiveLocationQuery, LiveLocationDto>,
    IRequestHandler<GetAllLiveLocationsQuery, List<LiveLocationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLiveLocationQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<LiveLocationDto> Handle(GetLiveLocationQuery request, CancellationToken cancellationToken)
    {
        var session = await _context.TrackingSessions
            .Include(t => t.Employee)
            .Where(t => t.EmployeeId == request.EmployeeId && t.Status == TrackingSessionStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        if (session == null)
        {
            return new LiveLocationDto { IsActive = false, EmployeeId = request.EmployeeId };
        }

        return await BuildDto(session, cancellationToken);
    }

    public async Task<List<LiveLocationDto>> Handle(GetAllLiveLocationsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _context.TrackingSessions
            .Include(t => t.Employee)
            .Where(t => t.Status == TrackingSessionStatus.Active)
            .ToListAsync(cancellationToken);

        var result = new List<LiveLocationDto>(sessions.Count);
        foreach (var session in sessions)
            result.Add(await BuildDto(session, cancellationToken));

        return result;
    }

    private async Task<LiveLocationDto> BuildDto(Domain.Entities.TrackingSession session, CancellationToken cancellationToken)
    {
        var lastPoint = await _context.GpsLogs
            .Where(g => g.TrackingSessionId == session.Id)
            .OrderByDescending(g => g.RecordedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new LiveLocationDto
        {
            IsActive = true,
            TrackingSessionId = session.Id,
            EmployeeId = session.EmployeeId,
            EmployeeName = session.Employee.FirstName + " " + session.Employee.LastName,
            StartedAtUtc = session.StartedAtUtc,
            LastLatitude = lastPoint?.Latitude ?? session.StartLatitude,
            LastLongitude = lastPoint?.Longitude ?? session.StartLongitude,
            LastSpeedKmh = lastPoint?.SpeedKmh,
            LastBatteryPercent = lastPoint?.BatteryPercent ?? session.StartBatteryPercent,
            LastRecordedAtUtc = lastPoint?.RecordedAtUtc ?? session.StartedAtUtc,
            PointsCapturedSoFar = session.TotalPointsCaptured
        };
    }
}
