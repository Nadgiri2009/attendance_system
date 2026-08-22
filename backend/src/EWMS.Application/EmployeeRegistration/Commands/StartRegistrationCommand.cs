using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.EmployeeRegistration.Commands;

/// <summary>
/// Command to start the employee registration flow.
/// Creates a temporary registration session.
/// </summary>
public class StartRegistrationCommand : IRequest<Result<Guid>>
{
    /// <summary>
    /// Mobile number to register (with country code).
    /// </summary>
    public string MobileNumber { get; set; } = default!;

    public StartRegistrationCommand(string mobileNumber)
    {
        MobileNumber = mobileNumber;
    }
}
