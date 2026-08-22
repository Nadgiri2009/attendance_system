using EWMS.Application.Tracking.Commands.RecordLocation;
using EWMS.Application.Tracking.Commands.StartTrackingSession;
using EWMS.Application.Tracking.Commands.StopTrackingSession;
using EWMS.Application.Tracking.Queries.GetLiveLocation;
using EWMS.Application.Tracking.Queries.GetTrackingHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWMS.API.Controllers;

[Authorize]
public class TrackingController : BaseApiController
{
    [HttpPost("start")]
    public async Task<IActionResult> Start(StartTrackingSessionCommand command)
    {
        var result = await Mediator.Send(command with { DeviceIp = GetClientIp() });
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { trackingSessionId = result.Data } });
    }

    private string? GetClientIp()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return HttpContext?.Connection.RemoteIpAddress?.ToString();
    }

    [HttpPost("location")]
    public async Task<IActionResult> RecordLocation(RecordLocationCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop(StopTrackingSessionCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true });
    }

    [HttpGet("history/{attendanceId:guid}")]
    public async Task<IActionResult> GetHistory(Guid attendanceId)
    {
        var result = await Mediator.Send(new GetTrackingHistoryQuery(attendanceId));
        return Ok(new { success = true, data = result });
    }

    [HttpGet("live/{employeeId:guid}")]
    public async Task<IActionResult> GetLive(Guid employeeId)
    {
        var result = await Mediator.Send(new GetLiveLocationQuery(employeeId));
        return Ok(new { success = true, data = result });
    }

    // Additive beyond the spec's endpoint list: the live tracking dashboard
    // (requirement 7) needs every currently-tracked employee at once, not
    // one at a time, so admins/HR/managers can see them all on one map.
    [HttpGet("live")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetAllLive()
    {
        var result = await Mediator.Send(new GetAllLiveLocationsQuery());
        return Ok(new { success = true, data = result });
    }
}
