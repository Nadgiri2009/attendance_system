using System.Text.Json;
using System.Text.RegularExpressions;
using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Application.EmployeeRegistration.Commands;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using EWMS.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EWMS.Application.Common.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace EWMS.API.Handlers.Commands;

public class SubmitEmployeeDetailsCommandHandler : IRequestHandler<SubmitEmployeeDetailsCommand, Result<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SubmitEmployeeDetailsCommandHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public SubmitEmployeeDetailsCommandHandler(ApplicationDbContext dbContext, IWebHostEnvironment environment, ILogger<SubmitEmployeeDetailsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _environment = environment;
    }

    public async Task<Result<bool>> Handle(SubmitEmployeeDetailsCommand request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.RegistrationSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);

        if (session == null || session.IsExpired)
            return Result<bool>.Failure("Session not found or expired.");

        if (session.Status != RegistrationStatus.OtpVerified)
            return Result<bool>.Failure("Session must complete OTP verification first.");

        if (!request.Email.Contains("@"))
            return Result<bool>.Failure("Invalid email format.");

        var aadhaar = request.AadhaarNumber?.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (string.IsNullOrWhiteSpace(aadhaar) || !Regex.IsMatch(aadhaar, "^[2-9][0-9]{11}$"))
            return Result<bool>.Failure("Enter a valid 12-digit Aadhaar number.");

        var dept = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId && !d.IsDeleted, cancellationToken);
        if (dept == null) return Result<bool>.Failure("Department not found.");

        var desig = await _dbContext.Designations.FirstOrDefaultAsync(d => d.Id == request.DesignationId && !d.IsDeleted, cancellationToken);
        if (desig == null) return Result<bool>.Failure("Designation not found.");

        if (request.PhotoBytes.Length == 0)
            return Result<bool>.Failure("Employee photo is required.");

        var photoExtension = request.PhotoContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(photoExtension))
            return Result<bool>.Failure("Employee photo must be a JPEG, PNG, or WebP image.");

        var photoDirectory = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "employees");
        Directory.CreateDirectory(photoDirectory);
        var photoFileName = $"{Guid.NewGuid():N}{photoExtension}";
        await File.WriteAllBytesAsync(Path.Combine(photoDirectory, photoFileName), request.PhotoBytes, cancellationToken);

        var details = new { request.FirstName, request.LastName, request.DateOfBirth, request.Gender, request.Email, request.Address, request.DepartmentId, request.DesignationId, request.EmploymentType, request.DateOfJoining, AadhaarNumber = aadhaar, PhotoUrl = $"/uploads/employees/{photoFileName}" };
        session.EmployeeDetailsJson = JsonSerializer.Serialize(details);
        session.Status = RegistrationStatus.AwaitingIdentityVerification;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation($"Employee details submitted for session {request.SessionId}");

        return Result<bool>.Success(true);
    }
}

public class VerifyIdentityCommandHandler : IRequestHandler<VerifyIdentityCommand, Result<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IIdentityVerificationProvider _identityProvider;
    private readonly FeatureOptions _featureOptions;
    private readonly ILogger<VerifyIdentityCommandHandler> _logger;

    public VerifyIdentityCommandHandler(ApplicationDbContext dbContext, IIdentityVerificationProvider identityProvider, IOptions<FeatureOptions> featureOptions, ILogger<VerifyIdentityCommandHandler> logger)
    {
        _dbContext = dbContext;
        _identityProvider = identityProvider;
        _featureOptions = featureOptions.Value;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(VerifyIdentityCommand request, CancellationToken cancellationToken)
    {
        var session = await _dbContext.RegistrationSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);

        if (session == null || session.IsExpired)
            return Result<bool>.Failure("Session not found or expired.");

        if (!_featureOptions.AadhaarVerificationRequired)
            return Result<bool>.Failure("Aadhaar verification is disabled for this environment.");

        if (session.Status != RegistrationStatus.AwaitingIdentityVerification)
            return Result<bool>.Failure("Session must be in identity verification step.");

        var aadhaar = request.IdentityInput?.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (string.IsNullOrWhiteSpace(aadhaar) || !Regex.IsMatch(aadhaar, "^[2-9][0-9]{11}$"))
            return Result<bool>.Failure("Enter a valid 12-digit Aadhaar number.");

        var verResult = await _identityProvider.VerifyIdentityAsync(aadhaar, cancellationToken);

        if (!verResult.IsSuccess)
        {
            _logger.LogWarning($"Identity verification failed for session {request.SessionId}: {verResult.Message}");
            return Result<bool>.Failure($"Identity verification failed: {verResult.Message}");
        }

        var idVer = new IdentityVerification
        {
            RegistrationSessionId = session.Id,
            Provider = _identityProvider.ProviderName,
            VerificationReference = verResult.VerificationReference ?? string.Empty,
            Status = IdentityVerificationStatus.Verified,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationMetadataJson = verResult.MetadataJson
        };

        await _dbContext.IdentityVerifications.AddAsync(idVer, cancellationToken);
        session.IdentityVerification = idVer;
        session.Status = RegistrationStatus.IdentityVerified;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation($"Identity verification successful for session {request.SessionId}");

        return Result<bool>.Success(true);
    }
}
