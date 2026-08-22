using EWMS.Application.Designations.Queries.GetDesignations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWMS.API.Controllers;

[Authorize]
public class DesignationsController : BaseApiController
{
    // ?departmentId= is the piece the Employee form was missing: it lets the
    // UI populate a real dropdown of designations that belong to the selected
    // department, instead of requiring a hand-typed GUID (see docs/BUGFIXES.md).
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] Guid? departmentId)
    {
        var result = await Mediator.Send(new GetDesignationsQuery(departmentId));
        return Ok(new { success = true, data = result });
    }
}
