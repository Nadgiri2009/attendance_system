using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EWMS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EWMS.Infrastructure.Services;

public sealed class HttpBiometricOptions
{
    public string ProviderName { get; set; } = "HttpBiometricProvider";
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string StartPath { get; set; } = "enrollment/start";
    public string FingerPath { get; set; } = "enrollment/finger";
    public string CompletePath { get; set; } = "enrollment/complete";
    public string VerifyPath { get; set; } = "enrollment/verify";
}

public sealed class HttpBiometricProvider : IBiometricProvider
{
    private readonly HttpClient _httpClient;
    private readonly HttpBiometricOptions _options;
    private readonly ILogger<HttpBiometricProvider> _logger;

    public HttpBiometricProvider(
        HttpClient httpClient,
        IOptions<HttpBiometricOptions> options,
        ILogger<HttpBiometricProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => _options.ProviderName;

    public Task<BiometricEnrollmentResult> StartEnrollmentAsync(
        Guid employeeId,
        int requiredFingers = 8,
        CancellationToken cancellationToken = default) =>
        SendAsync<StartEnrollmentRequest, BiometricEnrollmentResult>(
            _options.StartPath,
            new StartEnrollmentRequest(employeeId, requiredFingers),
            cancellationToken);

    public Task<BiometricFingerEnrollResult> EnrollFingerAsync(
        string enrollmentReference,
        int fingerNumber,
        byte[] templateData,
        CancellationToken cancellationToken = default) =>
        SendAsync<EnrollFingerRequest, BiometricFingerEnrollResult>(
            _options.FingerPath,
            new EnrollFingerRequest(enrollmentReference, fingerNumber, Convert.ToBase64String(templateData)),
            cancellationToken);

    public Task<BiometricEnrollmentCompleteResult> CompleteEnrollmentAsync(
        string enrollmentReference,
        CancellationToken cancellationToken = default) =>
        SendAsync<CompleteEnrollmentRequest, BiometricEnrollmentCompleteResult>(
            _options.CompletePath,
            new CompleteEnrollmentRequest(enrollmentReference),
            cancellationToken);

    public Task<BiometricVerificationResult> VerifyBiometricAsync(
        string enrollmentReference,
        byte[] templateData,
        CancellationToken cancellationToken = default) =>
        SendAsync<VerifyBiometricRequest, BiometricVerificationResult>(
            _options.VerifyPath,
            new VerifyBiometricRequest(enrollmentReference, Convert.ToBase64String(templateData)),
            cancellationToken);

    private async Task<TResult> SendAsync<TRequest, TResult>(
        string path,
        TRequest request,
        CancellationToken cancellationToken)
        where TResult : class, new()
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(path, request, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = payload switch
                {
                    BiometricEnrollmentResult result => result.Message,
                    BiometricFingerEnrollResult result => result.Message,
                    BiometricEnrollmentCompleteResult result => result.Message,
                    BiometricVerificationResult result => result.Message,
                    _ => $"Biometric device returned HTTP {(int)response.StatusCode}."
                };

                throw new HttpRequestException(message, null, response.StatusCode);
            }

            return payload ?? new TResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Biometric device request failed for {Path}", path);
            return CreateFailure<TResult>(ex.Message);
        }
    }

    private static TResult CreateFailure<TResult>(string message)
        where TResult : class, new()
    {
        return new TResult() switch
        {
            BiometricEnrollmentResult result => (TResult)(object)new BiometricEnrollmentResult { IsSuccess = false, Message = message },
            BiometricFingerEnrollResult result => (TResult)(object)new BiometricFingerEnrollResult { IsSuccess = false, Message = message },
            BiometricEnrollmentCompleteResult result => (TResult)(object)new BiometricEnrollmentCompleteResult { IsSuccess = false, Message = message },
            BiometricVerificationResult result => (TResult)(object)new BiometricVerificationResult { IsSuccess = false, Message = message },
            _ => new TResult()
        };
    }

    private sealed record StartEnrollmentRequest(Guid EmployeeId, int RequiredFingers);
    private sealed record EnrollFingerRequest(string EnrollmentReference, int FingerNumber, string TemplateDataBase64);
    private sealed record CompleteEnrollmentRequest(string EnrollmentReference);
    private sealed record VerifyBiometricRequest(string EnrollmentReference, string TemplateDataBase64);
}