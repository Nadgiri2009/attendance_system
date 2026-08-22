using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Application.EmployeeRegistration.Commands;
using EWMS.Domain.Enums;
using EWMS.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EWMS.API.Handlers.Commands;

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOtpProvider _otpProvider;
    private readonly IOtpCache _otpCache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VerifyOtpCommandHandler> _logger;

    public VerifyOtpCommandHandler(
        ApplicationDbContext dbContext,
        IOtpProvider otpProvider,
        IOtpCache otpCache,
        IConfiguration configuration,
        ILogger<VerifyOtpCommandHandler> logger
    )
    {
        _dbContext = dbContext;
        _otpProvider = otpProvider;
        _otpCache = otpCache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Otp))
            return Result<bool>.Failure("OTP is required.");

        var session = await _dbContext.RegistrationSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);

        if (session == null)
            return Result<bool>.Failure("Registration session not found.");

        if (session.IsExpired)
            return Result<bool>.Failure("Registration session has expired. Please start a new registration.");

        var maxAttempts = int.TryParse(_configuration["OTP:MaxAttempts"], out var max) ? max : 5;
        if (session.IsOtpAttemptsExhausted(maxAttempts))
        {
            session.Status = RegistrationStatus.Failed;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure("OTP verification attempts exceeded. Please start a new registration.");
        }

        var storedOtpHash = await _otpCache.GetOtpAsync(request.SessionId);

        if (string.IsNullOrEmpty(storedOtpHash))
        {
            _logger.LogWarning($"OTP not found or expired for session {request.SessionId}");
            session.OtpAttempts++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure("OTP not found or has expired. Please request a new OTP.");
        }

        if (!_otpProvider.VerifyOtp(request.Otp, storedOtpHash))
        {
            _logger.LogWarning($"OTP verification failed for session {request.SessionId}");
            session.OtpAttempts++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure("Invalid OTP. Please try again.");
        }

        session.Status = RegistrationStatus.OtpVerified;
        session.OtpAttempts = 0;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _otpCache.RemoveOtpAsync(request.SessionId);

        _logger.LogInformation($"OTP verified successfully for session {request.SessionId}");

        return Result<bool>.Success(true);
    }
}
