using EWMS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EWMS.Infrastructure.Services;

/// <summary>
/// Mock biometric provider for development/testing.
/// In production, replace with actual biometric device SDK (Neurotechnology, DigitalPersona, etc).
/// Simulates successful biometric enrollment and verification.
/// </summary>
public class MockBiometricProvider : IBiometricProvider
{
    public string ProviderName => "MockBiometricProvider";

    private readonly ILogger<MockBiometricProvider> _logger;

    // In-memory store for active enrollments (simulate device state)
    private readonly Dictionary<string, MockEnrollmentSession> _activeSessions = new();

    public MockBiometricProvider(ILogger<MockBiometricProvider> logger)
    {
        _logger = logger;
    }

    public Task<BiometricEnrollmentResult> StartEnrollmentAsync(
        Guid employeeId,
        int requiredFingers = 8,
        CancellationToken cancellationToken = default
    )
    {
        var enrollmentRef = $"MOCK-ENROLL-{Guid.NewGuid():N}";

        // Create mock enrollment session
        var session = new MockEnrollmentSession
        {
            EnrollmentReference = enrollmentRef,
            EmployeeId = employeeId,
            RequiredFingerCount = requiredFingers,
            EnrolledFingers = new List<int>()
        };

        _activeSessions[enrollmentRef] = session;

        _logger.LogInformation($"Mock biometric enrollment started. Reference: {enrollmentRef}");

        return Task.FromResult(new BiometricEnrollmentResult
        {
            IsSuccess = true,
            EnrollmentReference = enrollmentRef
        });
    }

    public Task<BiometricFingerEnrollResult> EnrollFingerAsync(
        string enrollmentReference,
        int fingerNumber,
        byte[] templateData,
        CancellationToken cancellationToken = default
    )
    {
        if (!_activeSessions.TryGetValue(enrollmentReference, out var session))
        {
            _logger.LogWarning($"Invalid enrollment reference: {enrollmentReference}");
            return Task.FromResult(new BiometricFingerEnrollResult
            {
                IsSuccess = false,
                Message = "Enrollment session not found or expired"
            });
        }

        if (fingerNumber < 1 || fingerNumber > 8)
        {
            return Task.FromResult(new BiometricFingerEnrollResult
            {
                IsSuccess = false,
                Message = "Invalid finger number (must be 1-8)"
            });
        }

        if (!session.EnrolledFingers.Contains(fingerNumber))
        {
            session.EnrolledFingers.Add(fingerNumber);
            session.EnrolledFingers.Sort();
        }

        _logger.LogInformation($"Mock finger {fingerNumber} enrolled. Progress: {session.EnrolledFingers.Count}/{session.RequiredFingerCount}");

        return Task.FromResult(new BiometricFingerEnrollResult
        {
            IsSuccess = true,
            Message = $"Finger {fingerNumber} enrolled successfully",
            ProgressCount = session.EnrolledFingers.Count
        });
    }

    public Task<BiometricEnrollmentCompleteResult> CompleteEnrollmentAsync(
        string enrollmentReference,
        CancellationToken cancellationToken = default
    )
    {
        if (!_activeSessions.TryGetValue(enrollmentReference, out var session))
        {
            _logger.LogWarning($"Invalid enrollment reference: {enrollmentReference}");
            return Task.FromResult(new BiometricEnrollmentCompleteResult
            {
                IsSuccess = false,
                Message = "Enrollment session not found or expired"
            });
        }

        // Check if all required fingers are enrolled
        if (session.EnrolledFingers.Count < session.RequiredFingerCount)
        {
            return Task.FromResult(new BiometricEnrollmentCompleteResult
            {
                IsSuccess = false,
                Message = $"Not all required fingers are enrolled. Enrolled: {session.EnrolledFingers.Count}/{session.RequiredFingerCount}"
            });
        }

        // Mark as completed and keep in memory for verification
        session.IsCompleted = true;

        var enrolledFingers = string.Join(",", session.EnrolledFingers);

        _logger.LogInformation($"Mock biometric enrollment completed. Fingers: {enrolledFingers}");

        return Task.FromResult(new BiometricEnrollmentCompleteResult
        {
            IsSuccess = true,
            EnrolledFingers = enrolledFingers
        });
    }

    public Task<BiometricVerificationResult> VerifyBiometricAsync(
        string enrollmentReference,
        byte[] templateData,
        CancellationToken cancellationToken = default
    )
    {
        if (!_activeSessions.TryGetValue(enrollmentReference, out var session))
        {
            _logger.LogWarning($"Invalid enrollment reference: {enrollmentReference}");
            return Task.FromResult(new BiometricVerificationResult
            {
                IsSuccess = false,
                Message = "Enrollment not found"
            });
        }

        if (!session.IsCompleted)
        {
            return Task.FromResult(new BiometricVerificationResult
            {
                IsSuccess = false,
                Message = "Enrollment is not complete"
            });
        }

        // Mock verification - always succeeds if template data is provided
        var matchScore = templateData != null && templateData.Length > 0 ? 95 : 0;

        _logger.LogInformation($"Mock biometric verification completed. Match score: {matchScore}");

        return Task.FromResult(new BiometricVerificationResult
        {
            IsSuccess = matchScore >= 80,
            MatchScore = matchScore
        });
    }

    /// <summary>
    /// Mock enrollment session state.
    /// </summary>
    private class MockEnrollmentSession
    {
        public string EnrollmentReference { get; set; } = default!;
        public Guid EmployeeId { get; set; }
        public int RequiredFingerCount { get; set; }
        public List<int> EnrolledFingers { get; set; } = new();
        public bool IsCompleted { get; set; }
    }
}
