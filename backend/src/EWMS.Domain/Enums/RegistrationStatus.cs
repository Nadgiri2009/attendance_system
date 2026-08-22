namespace EWMS.Domain.Enums;

/// <summary>
/// Registration workflow status for temporary registration sessions.
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// Registration session initiated, awaiting OTP verification.
    /// </summary>
    AwaitingOtpVerification = 1,

    /// <summary>
    /// OTP verified, employee details being collected.
    /// </summary>
    OtpVerified = 2,

    /// <summary>
    /// Employee details submitted, awaiting identity verification.
    /// </summary>
    AwaitingIdentityVerification = 3,

    /// <summary>
    /// Identity verified, biometric enrollment in progress.
    /// </summary>
    IdentityVerified = 4,

    /// <summary>
    /// Biometric enrollment complete, awaiting final verification.
    /// </summary>
    BiometricEnrolled = 5,

    /// <summary>
    /// Biometric enrollment has been started and is accepting fingers.
    /// </summary>
    BiometricEnrollmentStarted = 9,

    /// <summary>
    /// All required fingers have been enrolled and final verification is pending or complete.
    /// </summary>
    BiometricEnrollmentCompleted = 10,

    /// <summary>
    /// Final biometric verification passed.
    /// </summary>
    BiometricVerified = 11,

    /// <summary>
    /// All steps complete, permanent employee record created.
    /// </summary>
    Completed = 6,

    /// <summary>
    /// Registration session expired or cancelled.
    /// </summary>
    Expired = 7,

    /// <summary>
    /// Registration failed at some step.
    /// </summary>
    Failed = 8
}
