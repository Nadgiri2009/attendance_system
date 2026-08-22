using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.Auth.Commands.Register;

public record RegisterCommand(
    string UserName,
    string Email,
    string Password,
    Guid? EmployeeId,
    List<string>? Roles) : IRequest<Result<Guid>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var roles = request.Roles is { Count: > 0 } ? request.Roles : new List<string> { "Employee" };

        var result = await _identityService.CreateUserAsync(
            request.UserName, request.Email, request.Password, roles, request.EmployeeId);

        return result;
    }
}
