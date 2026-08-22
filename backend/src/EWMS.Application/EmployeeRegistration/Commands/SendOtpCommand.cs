using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.EmployeeRegistration.Commands;

/// <summary>
/// Command to send OTP to the registered mobile number.
/// </summary>
public class SendOtpCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    public SendOtpCommand(Guid sessionId)
    {
        SessionId = sessionId;
    }
}
