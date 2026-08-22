using EWMS.Application.Auth.Commands.Login;
using EWMS.Application.Auth.Commands.RefreshToken;
using EWMS.Application.Auth.Commands.Register;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWMS.API.Controllers;

public class AuthController : BaseApiController
{
    [HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login(LoginCommand command)
{
    var result = await Mediator.Send(command);

    if (!result.Succeeded)
        return Unauthorized(new
        {
            success = false,
            message = result.Errors.FirstOrDefault()
        });

    return Ok(new
    {
        success = true,
        data = result.Data
    });
}

    [HttpPost("register")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { userId = result.Data } });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return Unauthorized(new { success = false, message = result.Errors.FirstOrDefault() });

        return Ok(new { success = true, data = result.Data });
    }
}
