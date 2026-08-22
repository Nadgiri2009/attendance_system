using EWMS.Application.Employees.Commands.CreateEmployee;
using EWMS.Application.Employees.Commands.DeleteEmployee;
using EWMS.Application.Employees.Commands.UpdateEmployee;
using EWMS.Application.Employees.Queries.GetEmployeeById;
using EWMS.Application.Employees.Queries.GetEmployees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWMS.API.Controllers;

[Authorize]
public class EmployeesController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetEmployeesQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetEmployeeByIdQuery(id));
        return Ok(new { success = true, data = result });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateEmployeeCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return CreatedAtAction(nameof(GetById), new { id = result.Data }, new { success = true, data = new { id = result.Data } });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { success = false, message = "Route id and payload id do not match." });

        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteEmployeeCommand(id));
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true });
    }
}
