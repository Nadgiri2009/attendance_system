using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.EmployeeRegistration.Commands;

/// <summary>
/// Command to start biometric enrollment.
/// </summary>
public class StartBiometricEnrollmentCommand : IRequest<Result<bool>>
{
    public Guid SessionId { get; set; }
    public int RequiredFingers { get; set; } = 8;
}

/// <summary>
/// Command to enroll a single finger.
/// </summary>
public class EnrollFingerCommand : IRequest<Result<bool>>
{
    public Guid SessionId { get; set; }
    public int FingerNumber { get; set; }
    public string TemplateDataBase64 { get; set; } = default!;
}

/// <summary>
/// Command to complete biometric enrollment after all required fingers are enrolled.
/// </summary>
public class CompleteBiometricEnrollmentCommand : IRequest<Result<bool>>
{
    public Guid SessionId { get; set; }
}
