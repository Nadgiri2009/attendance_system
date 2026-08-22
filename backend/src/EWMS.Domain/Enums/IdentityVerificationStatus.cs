namespace EWMS.Domain.Enums;

/// <summary>
/// Status of identity verification in the registration process.
/// </summary>
public enum IdentityVerificationStatus
{
    /// <summary>
    /// Identity verification is pending.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Identity has been successfully verified.
    /// </summary>
    Verified = 2,

    /// <summary>
    /// Identity verification failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Identity verification was rejected.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Identity verification expired without completion.
    /// </summary>
    Expired = 5
}
