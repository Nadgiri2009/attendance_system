using EWMS.Domain.Common;
using EWMS.Domain.Enums;

namespace EWMS.Domain.Entities;

/// <summary>
/// Temporary session for employee registration workflow.
/// Session expires after configured duration if not completed.
/// </summary>
public class RegistrationSession : AuditableEntity
{
    /// <summary>
    /// Mobile number being registered (verified via OTP).
    /// </summary>
    public string MobileNumber { get; set; } = default!;

    /// <summary>
    /// Current status of the registration workflow.
    /// </summary>
    public RegistrationStatus Status { get; set; } = RegistrationStatus.AwaitingOtpVerification;

    /// <summary>
    /// Timestamp when this session must expire.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Number of OTP verification attempts made.
    /// </summary>
    public int OtpAttempts { get; set; } = 0;

    /// <summary>
    /// Last timestamp when OTP was sent.
    /// </summary>
    public DateTime? LastOtpSentAtUtc { get; set; }

    /// <summary>
    /// Tracks OTP resend count to enforce rate limiting.
    /// </summary>
    public int OtpResendCount { get; set; } = 0;

    /// <summary>
    /// Temporary storage of employee details during registration (JSON).
    /// </summary>
    public string? EmployeeDetailsJson { get; set; }

    /// <summary>
    /// Related identity verification record.
    /// </summary>
    public IdentityVerification? IdentityVerification { get; set; }

    /// <summary>
    /// Related biometric enrollment record.
    /// </summary>
    public BiometricEnrollment? BiometricEnrollment { get; set; }

    /// <summary>
    /// Final employee ID after successful registration completion.
    /// </summary>
    public Guid? CreatedEmployeeId { get; set; }

    /// <summary>
    /// Verify if session has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;

    /// <summary>
    /// Verify if OTP verification attempts exceed limit.
    /// </summary>
    public bool IsOtpAttemptsExhausted(int maxAttempts) => OtpAttempts >= maxAttempts;

    /// <summary>
    /// Verify if OTP resend limit is reached.
    /// </summary>
    public bool IsOtpResendLimitReached(int maxResends) => OtpResendCount >= maxResends;
}
