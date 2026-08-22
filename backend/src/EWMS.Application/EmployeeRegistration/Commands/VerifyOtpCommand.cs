using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.EmployeeRegistration.Commands;

/// <summary>
/// Command to verify OTP for registration session.
/// </summary>
public class VerifyOtpCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// OTP code entered by user.
    /// </summary>
    public string Otp { get; set; } = default!;

    public VerifyOtpCommand(Guid sessionId, string otp)
    {
        SessionId = sessionId;
        Otp = otp;
    }
}
