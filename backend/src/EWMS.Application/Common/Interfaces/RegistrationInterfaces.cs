namespace EWMS.Application.Common.Interfaces;

/// <summary>
/// OTP generation and verification service.
/// </summary>
public interface IOtpProvider
{
    /// <summary>
    /// Generate a 6-digit OTP.
    /// </summary>
    string GenerateOtp();

    /// <summary>
    /// Hash an OTP securely.
    /// </summary>
    string HashOtp(string otp);

    /// <summary>
    /// Verify OTP against hash (timing-safe comparison).
    /// </summary>
    bool VerifyOtp(string plainOtp, string hashedOtp);
}

/// <summary>
/// SMS delivery service.
/// </summary>
public interface ISmsProvider
{
    string ProviderName => "SMS Provider";

    /// <summary>
    /// Send generic SMS message.
    /// </summary>
    Task<string?> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send OTP via SMS with expiry notice.
    /// </summary>
    Task<string?> SendOtpAsync(string phoneNumber, string otp, int expiryMinutes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Temporary cache for OTP verification.
/// In production, should be replaced with Redis or similar distributed cache.
/// </summary>
public interface IOtpCache
{
    /// <summary>
    /// Store hashed OTP for a session.
    /// </summary>
    Task SetOtpAsync(Guid sessionId, string hashedOtp, int expiryMinutes);

    /// <summary>
    /// Retrieve stored OTP hash.
    /// </summary>
    Task<string?> GetOtpAsync(Guid sessionId);

    /// <summary>
    /// Remove OTP from cache.
    /// </summary>
    Task RemoveOtpAsync(Guid sessionId);
}

/// <summary>
/// Identity verification provider interface.
/// Implement with real Aadhaar or third-party service in production.
/// </summary>
public interface IIdentityVerificationProvider
{
    string ProviderName => "Identity Provider";

    /// <summary>
    /// Verify identity and return reference only (no raw data stored).
    /// </summary>
    Task<IdentityVerificationResult> VerifyIdentityAsync(string identityInput, CancellationToken cancellationToken = default);
}

public class IdentityVerificationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? VerificationReference { get; set; }
    public string? MetadataJson { get; set; }
}

/// <summary>
/// Biometric enrollment and verification provider.
/// Implement with real biometric device/service in production.
/// </summary>
public interface IBiometricProvider
{
    string ProviderName => "Biometric Provider";

    Task<BiometricEnrollmentResult> StartEnrollmentAsync(Guid employeeId, int requiredFingers = 8, CancellationToken cancellationToken = default);
    Task<BiometricFingerEnrollResult> EnrollFingerAsync(string enrollmentReference, int fingerNumber, byte[] templateData, CancellationToken cancellationToken = default);
    Task<BiometricEnrollmentCompleteResult> CompleteEnrollmentAsync(string enrollmentReference, CancellationToken cancellationToken = default);
    Task<BiometricVerificationResult> VerifyBiometricAsync(string enrollmentReference, byte[] templateData, CancellationToken cancellationToken = default);
}

public class BiometricEnrollmentResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? EnrollmentReference { get; set; }
}

public class BiometricFingerEnrollResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProgressCount { get; set; }
}

public class BiometricEnrollmentCompleteResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? EnrolledFingers { get; set; }
}

public class BiometricVerificationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MatchScore { get; set; }
}

/// <summary>
/// Employee code generator service.
/// Generates unique, thread-safe employee codes with configurable format.
/// </summary>
public interface IEmployeeCodeGenerator
{
    /// <summary>
    /// Generate next employee code in sequence.
    /// Format: {Prefix}-{PaddedNumber} e.g., "SMC-EMP-000001"
    /// </summary>
    Task<string> GenerateEmployeeCodeAsync(CancellationToken cancellationToken = default);
}
