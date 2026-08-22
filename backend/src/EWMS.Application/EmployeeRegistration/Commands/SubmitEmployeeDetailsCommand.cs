using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.EmployeeRegistration.Commands;

/// <summary>
/// Command to submit employee details during registration.
/// </summary>
public class SubmitEmployeeDetailsCommand : IRequest<Result<bool>>
{
    public Guid SessionId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Address { get; set; } = default!;
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public string EmploymentType { get; set; } = "Permanent";
    public DateOnly DateOfJoining { get; set; }
    public string AadhaarNumber { get; set; } = default!;
    public byte[] PhotoBytes { get; set; } = Array.Empty<byte>();
    public string PhotoContentType { get; set; } = string.Empty;
}
