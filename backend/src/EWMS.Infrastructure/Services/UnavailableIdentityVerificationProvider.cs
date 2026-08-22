using EWMS.Application.Common.Interfaces;

namespace EWMS.Infrastructure.Services;

/// <summary>
/// Explicit adapter used when no authorized identity provider is configured.
/// </summary>
public sealed class UnavailableIdentityVerificationProvider : IIdentityVerificationProvider
{
    public string ProviderName => "UnconfiguredIdentityProvider";

    public Task<IdentityVerificationResult> VerifyIdentityAsync(string identityInput, CancellationToken cancellationToken = default) =>
        Task.FromResult(new IdentityVerificationResult
        {
            IsSuccess = false,
            Message = "No authorized Aadhaar identity provider is configured."
        });
}
