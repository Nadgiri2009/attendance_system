namespace EWMS.Application.Common.Interfaces;

public record TokenResult(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc);

public interface ITokenService
{
    TokenResult GenerateTokens(Guid userId, string userName, string email, IEnumerable<string> roles, Guid? employeeId);
    (bool isValid, Guid userId) ValidateRefreshToken(string refreshToken);
}
