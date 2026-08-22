namespace EWMS.Domain.Enums;

/// <summary>
/// Status of biometric enrollment in the registration process.
/// </summary>
public enum BiometricStatus
{
    /// <summary>
    /// Biometric enrollment not yet started.
    /// </summary>
    NotStarted = 1,

    /// <summary>
    /// Biometric enrollment in progress.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Biometric enrollment completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Biometric verification passed.
    /// </summary>
    VerificationPassed = 4,

    /// <summary>
    /// Biometric verification failed.
    /// </summary>
    VerificationFailed = 5,

    /// <summary>
    /// Biometric enrollment failed.
    /// </summary>
    Failed = 6
}
