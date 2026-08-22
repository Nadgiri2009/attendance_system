namespace EWMS.Application.Auth;

public class AuthResultDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid? EmployeeId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
