using EWMS.Application.Common.Models;
using MediatR;

namespace EWMS.Application.EmployeeRegistration.Queries;

/// <summary>
/// Query to get current registration status.
/// </summary>
public class GetRegistrationStatusQuery : IRequest<Result<RegistrationStatusResponse>>
{
    public Guid SessionId { get; set; }
}

/// <summary>
/// Query to get biometric enrollment status.
/// </summary>
public class GetBiometricStatusQuery : IRequest<Result<BiometricStatusResponse>>
{
    public Guid SessionId { get; set; }
}

/// <summary>
/// Response for registration status query.
/// </summary>
public class RegistrationStatusResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public int CurrentStep { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response for biometric enrollment status query.
/// </summary>
public class BiometricStatusResponse
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RequiredFingers { get; set; }
    public int EnrolledFingers { get; set; }
    public List<int> EnrolledFingerNumbers { get; set; } = new();
    public int ProgressPercentage { get; set; }
    public string? ErrorMessage { get; set; }
}
