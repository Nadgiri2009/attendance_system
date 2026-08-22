using EWMS.Domain.Common;

namespace EWMS.Domain.Entities;

/// <summary>
/// Audit log for tracking important application events.
/// Used to record registration steps, attendance changes, failed verifications, etc.
/// </summary>
public class AuditLog : AuditableEntity
{
    /// <summary>
    /// Action being audited (e.g., "RegistrationStarted", "BiometricVerificationFailed").
    /// </summary>
    public string Action { get; set; } = default!;

    /// <summary>
    /// Entity type being acted upon (e.g., "Employee", "RegistrationSession").
    /// </summary>
    public string EntityType { get; set; } = default!;

    /// <summary>
    /// ID of the entity being acted upon (if applicable).
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// ID of the user performing the action (if authenticated).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Username of the user performing the action.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Status of the action result (e.g., "Success", "Failed").
    /// </summary>
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Detailed information about the action (JSON serialized).
    /// Sensitive data must NOT be stored (no OTP, Aadhaar, biometric templates).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Provider transaction/reference ID if applicable.
    /// </summary>
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Error message if the action failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// IP address from which the action was initiated.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Timestamp of the audit event.
    /// </summary>
    public DateTime EventAtUtc { get; set; } = DateTime.UtcNow;
}
