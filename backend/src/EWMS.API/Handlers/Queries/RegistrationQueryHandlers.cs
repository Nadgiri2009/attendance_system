using EWMS.Application.Common.Models;
using EWMS.Application.EmployeeRegistration.Queries;
using EWMS.Domain.Enums;
using EWMS.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EWMS.API.Handlers.Queries;

public class RegistrationStatusQueryHandler : IRequestHandler<GetRegistrationStatusQuery, Result<RegistrationStatusResponse>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RegistrationStatusQueryHandler> _logger;

    public RegistrationStatusQueryHandler(ApplicationDbContext dbContext, ILogger<RegistrationStatusQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<RegistrationStatusResponse>> Handle(GetRegistrationStatusQuery request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.RegistrationSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);
        if (session == null) return Result<RegistrationStatusResponse>.Failure("Session not found.");

        var step = GetCurrentStep(session.Status);
        var response = new RegistrationStatusResponse
        {
            SessionId = session.Id,
            Status = session.Status.ToString(),
            ExpiresAt = session.ExpiresAtUtc,
            IsExpired = session.IsExpired,
            CurrentStep = step,
            Message = GetStepMessage(step, session.Status)
        };

        _logger.LogInformation($"Retrieved status for session {request.SessionId}: Step {step}, Status {session.Status}");
        return Result<RegistrationStatusResponse>.Success(response);
    }

    private int GetCurrentStep(RegistrationStatus status) =>
        status switch
        {
            RegistrationStatus.AwaitingOtpVerification => 1,
            RegistrationStatus.OtpVerified => 2,
            RegistrationStatus.AwaitingIdentityVerification => 3,
            RegistrationStatus.IdentityVerified => 4,
            RegistrationStatus.BiometricEnrolled => 5,
            RegistrationStatus.BiometricEnrollmentStarted => 5,
            RegistrationStatus.BiometricEnrollmentCompleted => 5,
            RegistrationStatus.BiometricVerified => 5,
            RegistrationStatus.Completed => 6,
            _ => 0
        };

    private string GetStepMessage(int step, RegistrationStatus status) =>
        step switch
        {
            1 => "Awaiting OTP verification",
            2 => "OTP verified - Ready for employee details",
            3 => "Awaiting identity verification",
            4 => "Ready for biometric enrollment",
            5 => "Biometric enrollment in progress",
            6 => "Registration completed successfully",
            _ => "Unknown status"
        };
}

public class BiometricStatusQueryHandler : IRequestHandler<GetBiometricStatusQuery, Result<BiometricStatusResponse>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BiometricStatusQueryHandler> _logger;

    public BiometricStatusQueryHandler(ApplicationDbContext dbContext, ILogger<BiometricStatusQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<BiometricStatusResponse>> Handle(GetBiometricStatusQuery request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.RegistrationSessions
            .Include(s => s.BiometricEnrollment)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);

        if (session == null) return Result<BiometricStatusResponse>.Failure("Session not found.");
        if (session.BiometricEnrollment == null)
            return Result<BiometricStatusResponse>.Success(new BiometricStatusResponse
            {
                SessionId = session.Id,
                Status = "NotStarted",
                RequiredFingers = 8,
                EnrolledFingers = 0,
                EnrolledFingerNumbers = new List<int>(),
                ProgressPercentage = 0,
                ErrorMessage = null
            });

        var enrolled = session.BiometricEnrollment.GetEnrolledFingers();
        var progress = (enrolled.Count * 100) / session.BiometricEnrollment.RequiredFingerCount;

        var response = new BiometricStatusResponse
        {
            SessionId = session.Id,
            Status = session.BiometricEnrollment.Status.ToString(),
            RequiredFingers = session.BiometricEnrollment.RequiredFingerCount,
            EnrolledFingers = enrolled.Count,
            EnrolledFingerNumbers = enrolled,
            ProgressPercentage = progress,
            ErrorMessage = session.BiometricEnrollment.FailureReason
        };

        _logger.LogInformation($"Biometric status for session {request.SessionId}: {enrolled.Count}/{session.BiometricEnrollment.RequiredFingerCount} fingers");
        return Result<BiometricStatusResponse>.Success(response);
    }
}
