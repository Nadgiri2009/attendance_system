using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace EWMS.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<Result<AuthenticatedUser>> ValidateCredentialsAsync(string userNameOrEmail, string password)
    {
        var user = await _userManager.FindByNameAsync(userNameOrEmail)
                   ?? await _userManager.FindByEmailAsync(userNameOrEmail);

        if (user == null || !user.IsActive)
            return Result<AuthenticatedUser>.Failure("Invalid credentials.");

        var valid = await _userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return Result<AuthenticatedUser>.Failure("Invalid credentials.");

        var roles = await _userManager.GetRolesAsync(user);
        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Result<AuthenticatedUser>.Success(
            new AuthenticatedUser(user.Id, user.UserName!, user.Email!, user.EmployeeId, roles.ToList()));
    }

    public async Task<Result<Guid>> CreateUserAsync(string userName, string email, string password, IEnumerable<string> roles, Guid? employeeId)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmployeeId = employeeId,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return Result<Guid>.Failure(createResult.Errors.Select(e => e.Description).ToArray());

        var roleResult = await _userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
            return Result<Guid>.Failure(roleResult.Errors.Select(e => e.Description).ToArray());

        return Result<Guid>.Success(user.Id);
    }

    public async Task<AuthenticatedUser?> GetUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new AuthenticatedUser(user.Id, user.UserName!, user.Email!, user.EmployeeId, roles.ToList());
    }
}
