namespace EWMS.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    Guid? EmployeeId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
