using EWMS.Domain.Common;
using EWMS.Domain.Enums;

namespace EWMS.Domain.Entities;

/// <summary>
/// Records biometric enrollment during employee registration.
/// Stores enrollment metadata and reference, not raw fingerprint templates or images.
/// </summary>
public class BiometricEnrollment : AuditableEntity
{
    /// <summary>
    /// Associated registration session.
    /// </summary>
    public Guid RegistrationSessionId { get; set; }
    public RegistrationSession RegistrationSession { get; set; } = default!;

    /// <summary>
    /// Biometric provider name (e.g., "MockProvider", vendor SDK name).
    /// </summary>
    public string Provider { get; set; } = default!;

    /// <summary>
    /// Enrollment reference/transaction ID from the provider.
    /// Used to look up enrollment records without storing raw templates.
    /// </summary>
    public string EnrollmentReference { get; set; } = default!;

    /// <summary>
    /// Number of fingers required for successful enrollment (typically 8).
    /// </summary>
    public int RequiredFingerCount { get; set; } = 8;

    /// <summary>
    /// Number of fingers successfully enrolled.
    /// </summary>
    public int EnrolledFingerCount { get; set; } = 0;

    /// <summary>
    /// Comma-separated list of successfully enrolled finger positions.
    /// Example: "1,2,3,4,5,6,7,8"
    /// </summary>
    public string? EnrolledFingers { get; set; }

    /// <summary>
    /// Current status of biometric enrollment.
    /// </summary>
    public BiometricStatus Status { get; set; } = BiometricStatus.NotStarted;

    /// <summary>
    /// Timestamp when enrollment was started.
    /// </summary>
    public DateTime? EnrollmentStartedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when enrollment was completed.
    /// </summary>
    public DateTime? EnrollmentCompletedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when final verification was performed.
    /// </summary>
    public DateTime? VerificationAttemptedAtUtc { get; set; }

    /// <summary>
    /// Result of final biometric verification (true = passed).
    /// </summary>
    public bool? VerificationResult { get; set; }

    /// <summary>
    /// Additional metadata or details about enrollment (JSON).
    /// </summary>
    public string? EnrollmentMetadataJson { get; set; }

    /// <summary>
    /// Reason for enrollment or verification failure.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Check if all required fingers are enrolled.
    /// </summary>
    public bool IsFullyEnrolled => EnrolledFingerCount >= RequiredFingerCount;

    /// <summary>
    /// Parse enrolled fingers from comma-separated list.
    /// </summary>
    public List<int> GetEnrolledFingers()
    {
        if (string.IsNullOrEmpty(EnrolledFingers))
            return new List<int>();

        return EnrolledFingers
            .Split(',')
            .Select(f => int.TryParse(f.Trim(), out var finger) ? finger : -1)
            .Where(f => f > 0)
            .ToList();
    }
}
