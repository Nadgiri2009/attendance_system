using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Application.EmployeeRegistration.Commands;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using EWMS.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EWMS.API.Handlers.Commands;

public class StartRegistrationCommandHandler : IRequestHandler<StartRegistrationCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StartRegistrationCommandHandler> _logger;

    public StartRegistrationCommandHandler(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<StartRegistrationCommandHandler> logger
    )
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        StartRegistrationCommand request,
        CancellationToken cancellationToken
    )
    {
        // Validate mobile number format (at least 10 digits)
        if (string.IsNullOrWhiteSpace(request.MobileNumber) || request.MobileNumber.Length < 10)
            return Result<Guid>.Failure("Mobile number must be at least 10 digits.");

        // Check if employee with this phone already exists
        var existingEmployee = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.PhoneNumber == request.MobileNumber && !e.IsDeleted, cancellationToken);

        if (existingEmployee != null)
            return Result<Guid>.Failure("An employee with this mobile number already exists.");

        // Check if there's an active registration session with this mobile
        var activeSession = await _dbContext.RegistrationSessions
            .FirstOrDefaultAsync(
                s => s.MobileNumber == request.MobileNumber
                    && !s.IsDeleted
                    && s.Status != RegistrationStatus.Expired
                    && s.Status != RegistrationStatus.Failed
                    && s.Status != RegistrationStatus.Completed,
                cancellationToken
            );

        if (activeSession != null)
        {
            if (!activeSession.IsExpired)
            {
                _logger.LogInformation(
                    $"Reusing active registration session {activeSession.Id} for mobile {request.MobileNumber}."
                );
                return Result<Guid>.Success(activeSession.Id);
            }

            activeSession.Status = RegistrationStatus.Expired;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Create new registration session
        var sessionExpiryMinutes = int.TryParse(_configuration["Registration:SessionExpiryMinutes"], out var expiry) ? expiry : 60;
        var session = new RegistrationSession
        {
            Id = Guid.NewGuid(),
            MobileNumber = request.MobileNumber,
            Status = RegistrationStatus.AwaitingOtpVerification,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(sessionExpiryMinutes),
            OtpAttempts = 0,
            OtpResendCount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "system"
        };

        await _dbContext.RegistrationSessions.AddAsync(session, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"Registration session started for mobile {request.MobileNumber}. SessionId: {session.Id}");

        return Result<Guid>.Success(session.Id);
    }
}
