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

public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, Result<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOtpProvider _otpProvider;
    private readonly IOtpCache _otpCache;
    private readonly ISmsProvider _smsProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendOtpCommandHandler> _logger;

    public SendOtpCommandHandler(
        ApplicationDbContext dbContext,
        IOtpProvider otpProvider,
        IOtpCache otpCache,
        ISmsProvider smsProvider,
        IConfiguration configuration,
        ILogger<SendOtpCommandHandler> logger
    )
    {
        _dbContext = dbContext;
        _otpProvider = otpProvider;
        _otpCache = otpCache;
        _smsProvider = smsProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        SendOtpCommand request,
        CancellationToken cancellationToken
    )
    {
        // Get registration session
        var session = await _dbContext.RegistrationSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);

        if (session == null)
            return Result<bool>.Failure("Registration session not found.");

        // Check if session has expired
        if (session.IsExpired)
            return Result<bool>.Failure("Registration session has expired. Please start a new registration.");

        // Check OTP resend limit
        var maxResends = int.TryParse(_configuration["OTP:MaxResendCount"], out var max) ? max : 3;
        if (session.IsOtpResendLimitReached(maxResends))
            return Result<bool>.Failure($"OTP resend limit reached. Please try again later.");

        // Generate OTP
        var otp = _otpProvider.GenerateOtp();
        var hashedOtp = _otpProvider.HashOtp(otp);
        var otpExpiryMinutes = int.TryParse(_configuration["OTP:ExpiryMinutes"], out var expiry) ? expiry : 5;

        // Send SMS
        var smsResult = await _smsProvider.SendOtpAsync(
            session.MobileNumber,
            otp,
            otpExpiryMinutes,
            cancellationToken
        );

        if (string.IsNullOrEmpty(smsResult))
        {
            _logger.LogError($"Failed to send OTP for session {request.SessionId}");
            return Result<bool>.Failure("ACL Gateway could not deliver the OTP. Check the ACL SMS settings and phone number.");
        }

        // Store the hash only after the provider confirms delivery.
        await _otpCache.SetOtpAsync(request.SessionId, hashedOtp, otpExpiryMinutes);

        // Update session tracking
        session.LastOtpSentAtUtc = DateTime.UtcNow;
        session.OtpResendCount++;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"OTP sent successfully for session {request.SessionId}");

        // Log the OTP temporarily (ONLY for development! Remove in production!)
        // In production, OTP would only be sent via SMS
        if (_configuration.GetValue<bool>("Development:ShowOtpInLogs"))
        {
            _logger.LogInformation($"[DEV ONLY] OTP for testing: {otp}");
        }

        return Result<bool>.Success(true);
    }
}
