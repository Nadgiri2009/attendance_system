using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.Auth.Commands.Login;

public record LoginCommand(string UserNameOrEmail, string Password)
    : IRequest<Result<AuthResultDto>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResultDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await _identityService.ValidateCredentialsAsync(
            request.UserNameOrEmail,
            request.Password);

        if (!validation.Succeeded || validation.Data == null)
        {
            return Result<AuthResultDto>.Failure("Invalid username or password.");
        }

        var user = validation.Data;

        var tokens = _tokenService.GenerateTokens(
            user.UserId,
            user.UserName,
            user.Email,
            user.Roles,
            user.EmployeeId);

        return Result<AuthResultDto>.Success(new AuthResultDto
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.Email,
            EmployeeId = user.EmployeeId,
            Roles = user.Roles
        });
    }
}