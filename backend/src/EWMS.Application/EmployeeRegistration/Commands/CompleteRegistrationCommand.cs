using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.EmployeeRegistration.Commands;

/// <summary>
/// Command to finalize registration and create permanent employee record.
/// </summary>
public class CompleteRegistrationCommand : IRequest<Result<Guid>>
{
    public Guid SessionId { get; set; }
}
