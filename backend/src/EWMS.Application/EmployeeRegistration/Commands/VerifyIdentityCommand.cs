using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.EmployeeRegistration.Commands;

/// <summary>
/// Command to verify employee identity.
/// In production, this would call real Aadhaar or third-party verification service.
/// </summary>
public class VerifyIdentityCommand : IRequest<Result<bool>>
{
    public Guid SessionId { get; set; }
    public string IdentityInput { get; set; } = default!;
}
