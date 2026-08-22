using EWMS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EWMS.Infrastructure.Services;

/// <summary>
/// Mock identity verification provider for development/testing.
/// In production, replace with actual Aadhaar or third-party verification service.
/// Simulates successful identity verification for testing purposes.
/// </summary>
public class MockIdentityVerificationProvider : IIdentityVerificationProvider
{
    public string ProviderName => "MockProvider";

    private readonly ILogger<MockIdentityVerificationProvider> _logger;

    public MockIdentityVerificationProvider(ILogger<MockIdentityVerificationProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Mock identity verification - simulates successful verification.
    /// In production, this would call a real Aadhaar or identity verification service.
    /// </summary>
    public Task<IdentityVerificationResult> VerifyIdentityAsync(
        string identityInput,
        CancellationToken cancellationToken = default
    )
    {
        // Validate input format
        if (string.IsNullOrWhiteSpace(identityInput))
        {
            _logger.LogWarning("Identity verification failed: empty input");
            return Task.FromResult(new IdentityVerificationResult
            {
                IsSuccess = false,
                Message = "Identity input is required"
            });
        }

        // Simulate verification - in production, call real provider
        // For demo, always succeed if input is provided
        var verificationReference = $"MOCK-{Guid.NewGuid():N}";

        _logger.LogInformation($"Mock identity verification succeeded. Reference: {verificationReference}");

        return Task.FromResult(new IdentityVerificationResult
        {
            IsSuccess = true,
            VerificationReference = verificationReference,
            MetadataJson = "{\"verified_at\": \"" + DateTime.UtcNow.ToString("o") + "\"}"
        });
    }
}
