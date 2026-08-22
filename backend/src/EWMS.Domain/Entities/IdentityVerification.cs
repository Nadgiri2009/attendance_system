using EWMS.Domain.Common;
using EWMS.Domain.Enums;

namespace EWMS.Domain.Entities;

/// <summary>
/// Records identity verification performed during employee registration.
/// Stores only minimum required protected reference, not raw biometric data.
/// </summary>
public class IdentityVerification : AuditableEntity
{
    /// <summary>
    /// Associated registration session.
    /// </summary>
    public Guid RegistrationSessionId { get; set; }
    public RegistrationSession RegistrationSession { get; set; } = default!;

    /// <summary>
    /// Verification provider name (e.g., "Aadhaar", "MockProvider").
    /// </summary>
    public string Provider { get; set; } = default!;

    /// <summary>
    /// Reference/transaction ID from the verification provider.
    /// Used to look up verification records without storing raw biometric data.
    /// </summary>
    public string VerificationReference { get; set; } = default!;

    /// <summary>
    /// Current status of identity verification.
    /// </summary>
    public IdentityVerificationStatus Status { get; set; } = IdentityVerificationStatus.Pending;

    /// <summary>
    /// Timestamp when verification was completed.
    /// </summary>
    public DateTime? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Additional metadata or details about the verification (JSON).
    /// </summary>
    public string? VerificationMetadataJson { get; set; }

    /// <summary>
    /// Reason for failure if verification failed.
    /// </summary>
    public string? FailureReason { get; set; }
}
