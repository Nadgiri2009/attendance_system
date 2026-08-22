using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResultDto>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly ITokenService _tokenService;
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(ITokenService tokenService, IIdentityService identityService)
    {
        _tokenService = tokenService;
        _identityService = identityService;
    }

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var (isValid, userId) = _tokenService.ValidateRefreshToken(request.RefreshToken);
        if (!isValid)
            return Result<AuthResultDto>.Failure("Invalid or expired refresh token.");

        var user = await _identityService.GetUserAsync(userId);
        if (user == null)
            return Result<AuthResultDto>.Failure("User not found.");

        var tokens = _tokenService.GenerateTokens(user.UserId, user.UserName, user.Email, user.Roles, user.EmployeeId);

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
