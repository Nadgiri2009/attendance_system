using EWMS.Application.Departments.Queries.GetDepartments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWMS.API.Controllers;

[Authorize]
public class DepartmentsController : BaseApiController
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var result = await Mediator.Send(new GetDepartmentsQuery());
        return Ok(new { success = true, data = result });
    }
}
