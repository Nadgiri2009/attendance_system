using EWMS.Application.Common.Interfaces;

namespace EWMS.Infrastructure.Services;

/// <summary>
/// Explicit adapter used when no approved biometric SDK/API is configured.
/// It prevents the application from claiming that a biometric scan succeeded.
/// </summary>
public sealed class UnavailableBiometricProvider : IBiometricProvider
{
    public string ProviderName => "UnconfiguredBiometricProvider";

    public Task<BiometricEnrollmentResult> StartEnrollmentAsync(Guid employeeId, int requiredFingers = 8, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BiometricEnrollmentResult { IsSuccess = false, Message = "No approved biometric provider is configured." });

    public Task<BiometricFingerEnrollResult> EnrollFingerAsync(string enrollmentReference, int fingerNumber, byte[] templateData, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BiometricFingerEnrollResult { IsSuccess = false, Message = "No approved biometric provider is configured." });

    public Task<BiometricEnrollmentCompleteResult> CompleteEnrollmentAsync(string enrollmentReference, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BiometricEnrollmentCompleteResult { IsSuccess = false, Message = "No approved biometric provider is configured." });

    public Task<BiometricVerificationResult> VerifyBiometricAsync(string enrollmentReference, byte[] templateData, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BiometricVerificationResult { IsSuccess = false, Message = "No approved biometric provider is configured." });
}
