using EWMS.Application.Common.Models;

namespace EWMS.Application.Common.Interfaces;

public record AuthenticatedUser(Guid UserId, string UserName, string Email, Guid? EmployeeId, IReadOnlyList<string> Roles);

public interface IIdentityService
{
    Task<Result<AuthenticatedUser>> ValidateCredentialsAsync(string userNameOrEmail, string password);
    Task<Result<Guid>> CreateUserAsync(string userName, string email, string password, IEnumerable<string> roles, Guid? employeeId);
    Task<AuthenticatedUser?> GetUserAsync(Guid userId);
}
