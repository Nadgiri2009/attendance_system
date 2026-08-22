using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
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

namespace EWMS.API.Handlers.Commands;

public class BiometricHandlers
{
    public class StartBiometricHandler : IRequestHandler<StartBiometricEnrollmentCommand, Result<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IBiometricProvider _bioProvider;
        private readonly ILogger<StartBiometricHandler> _logger;
        private readonly FeatureOptions _featureOptions;

        public StartBiometricHandler(ApplicationDbContext dbContext, IBiometricProvider bioProvider, IOptions<FeatureOptions> featureOptions, ILogger<StartBiometricHandler> logger)
        {
            _dbContext = dbContext;
            _bioProvider = bioProvider;
            _featureOptions = featureOptions.Value;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(StartBiometricEnrollmentCommand request, CancellationToken cancellationToken)
        {
            var session = await _dbContext.RegistrationSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);
            if (session == null || session.IsExpired) return Result<bool>.Failure("Session not found or expired.");
            var identityReady = session.Status == RegistrationStatus.IdentityVerified;
            var detailsReady = session.Status == RegistrationStatus.AwaitingIdentityVerification;
            if (_featureOptions.AadhaarVerificationRequired ? !identityReady : !detailsReady && !identityReady)
                return Result<bool>.Failure(_featureOptions.AadhaarVerificationRequired
                    ? "Identity must be verified first."
                    : "Employee details must be submitted first.");

            var requiredFingers = request.RequiredFingers <= 0 ? 8 : request.RequiredFingers;
            if (requiredFingers != 8)
                return Result<bool>.Failure("Exactly 8 fingers are required.");

            var enrollResult = await _bioProvider.StartEnrollmentAsync(session.Id, requiredFingers, cancellationToken);
            if (!enrollResult.IsSuccess) return Result<bool>.Failure($"Enrollment failed: {enrollResult.Message}");

            var bioEnroll = new BiometricEnrollment
            {
                RegistrationSessionId = session.Id,
                Provider = _bioProvider.ProviderName,
                EnrollmentReference = enrollResult.EnrollmentReference ?? string.Empty,
                RequiredFingerCount = 8,
                Status = BiometricStatus.InProgress,
                EnrollmentStartedAtUtc = DateTime.UtcNow
            };

            await _dbContext.BiometricEnrollments.AddAsync(bioEnroll, cancellationToken);
            session.BiometricEnrollment = bioEnroll;
            session.Status = RegistrationStatus.BiometricEnrollmentStarted;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Biometric enrollment started for session {request.SessionId}");
            return Result<bool>.Success(true);
        }
    }

    public class EnrollFingerHandler : IRequestHandler<EnrollFingerCommand, Result<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IBiometricProvider _bioProvider;
        private readonly ILogger<EnrollFingerHandler> _logger;

        public EnrollFingerHandler(ApplicationDbContext dbContext, IBiometricProvider bioProvider, ILogger<EnrollFingerHandler> logger)
        {
            _dbContext = dbContext;
            _bioProvider = bioProvider;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(EnrollFingerCommand request, CancellationToken cancellationToken)
        {
            var session = await _dbContext.RegistrationSessions.Include(s => s.BiometricEnrollment).FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);
            if (session == null || session.IsExpired) return Result<bool>.Failure("Session not found or expired.");
            if (session?.BiometricEnrollment == null) return Result<bool>.Failure("Biometric enrollment not found.");
            if (session.BiometricEnrollment.Status != BiometricStatus.InProgress) return Result<bool>.Failure("Enrollment not in progress.");
            if (request.FingerNumber < 1 || request.FingerNumber > session.BiometricEnrollment.RequiredFingerCount)
                return Result<bool>.Failure($"Finger number must be between 1 and {session.BiometricEnrollment.RequiredFingerCount}.");

            var enrolledFingers = session.BiometricEnrollment.GetEnrolledFingers();
            if (enrolledFingers.Contains(request.FingerNumber))
                return Result<bool>.Failure("This finger has already been enrolled.");

            byte[] templateData;
            try { templateData = Convert.FromBase64String(request.TemplateDataBase64); }
            catch { return Result<bool>.Failure("Invalid template data format."); }

            if (templateData.Length == 0)
                return Result<bool>.Failure("A biometric provider scan is required.");

            var fingerResult = await _bioProvider.EnrollFingerAsync(session.BiometricEnrollment.EnrollmentReference, request.FingerNumber, templateData, cancellationToken);
            if (!fingerResult.IsSuccess) return Result<bool>.Failure($"Finger enrollment failed: {fingerResult.Message}");

            enrolledFingers.Add(request.FingerNumber);

            session.BiometricEnrollment.EnrolledFingers = string.Join(",", enrolledFingers.OrderBy(f => f));
            session.BiometricEnrollment.EnrolledFingerCount = enrolledFingers.Count;

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Finger {request.FingerNumber} enrolled. Progress: {enrolledFingers.Count}/{session.BiometricEnrollment.RequiredFingerCount}");

            return Result<bool>.Success(true);
        }
    }

    public class CompleteBiometricHandler : IRequestHandler<CompleteBiometricEnrollmentCommand, Result<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IBiometricProvider _bioProvider;
        private readonly ILogger<CompleteBiometricHandler> _logger;

        public CompleteBiometricHandler(ApplicationDbContext dbContext, IBiometricProvider bioProvider, ILogger<CompleteBiometricHandler> logger)
        {
            _dbContext = dbContext;
            _bioProvider = bioProvider;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(CompleteBiometricEnrollmentCommand request, CancellationToken cancellationToken)
        {
            var session = await _dbContext.RegistrationSessions.Include(s => s.BiometricEnrollment).FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);
            if (session?.BiometricEnrollment == null) return Result<bool>.Failure("Biometric enrollment not found.");
            if (session.BiometricEnrollment.Status != BiometricStatus.InProgress) return Result<bool>.Failure("Enrollment not in progress.");
            if (session.BiometricEnrollment.EnrolledFingerCount < session.BiometricEnrollment.RequiredFingerCount)
                return Result<bool>.Failure($"Not all fingers enrolled: {session.BiometricEnrollment.EnrolledFingerCount}/{session.BiometricEnrollment.RequiredFingerCount}");

            var completeResult = await _bioProvider.CompleteEnrollmentAsync(session.BiometricEnrollment.EnrollmentReference, cancellationToken);
            if (!completeResult.IsSuccess)
            {
                session.BiometricEnrollment.Status = BiometricStatus.Failed;
                session.BiometricEnrollment.FailureReason = completeResult.Message;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result<bool>.Failure($"Completion failed: {completeResult.Message}");
            }

            session.BiometricEnrollment.Status = BiometricStatus.Completed;
            session.BiometricEnrollment.EnrollmentCompletedAtUtc = DateTime.UtcNow;
            session.Status = RegistrationStatus.BiometricEnrollmentCompleted;
            session.BiometricEnrollment.VerificationResult = true;

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Biometric enrollment completed for session {request.SessionId}");

            return Result<bool>.Success(true);
        }
    }
}

public class CompleteRegistrationCommandHandler : IRequestHandler<CompleteRegistrationCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmployeeCodeGenerator _codeGen;
    private readonly FeatureOptions _featureOptions;
    private readonly ILogger<CompleteRegistrationCommandHandler> _logger;

    public CompleteRegistrationCommandHandler(ApplicationDbContext dbContext, IEmployeeCodeGenerator codeGen, IOptions<FeatureOptions> featureOptions, ILogger<CompleteRegistrationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _codeGen = codeGen;
        _featureOptions = featureOptions.Value;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CompleteRegistrationCommand request, CancellationToken cancellationToken)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var trans = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var session = await _dbContext.RegistrationSessions
                    .Include(s => s.IdentityVerification).Include(s => s.BiometricEnrollment)
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId && !s.IsDeleted, cancellationToken);

            if (session == null || session.IsExpired) return Result<Guid>.Failure("Session not found or expired.");
            if (_featureOptions.AadhaarVerificationRequired && session.IdentityVerification?.Status != IdentityVerificationStatus.Verified)
                return Result<Guid>.Failure("Identity verification required.");

            if (session.BiometricEnrollment?.Status is not (BiometricStatus.Completed or BiometricStatus.VerificationPassed))
                return Result<Guid>.Failure("Eight-finger biometric enrollment is required.");

            if (string.IsNullOrEmpty(session.EmployeeDetailsJson)) return Result<Guid>.Failure("Employee details missing.");

            using var detailsDocument = JsonDocument.Parse(session.EmployeeDetailsJson);
            var details = detailsDocument.RootElement;
            if (details.ValueKind != JsonValueKind.Object)
                return Result<Guid>.Failure("Invalid employee details.");

            var existing = await _dbContext.Employees.FirstOrDefaultAsync(e => e.PhoneNumber == session.MobileNumber && !e.IsDeleted, cancellationToken);
            if (existing != null)
            {
                await trans.RollbackAsync(cancellationToken);
                return Result<Guid>.Failure("Employee with this mobile already exists.");
            }

            var empCode = await _codeGen.GenerateEmployeeCodeAsync(cancellationToken);
            if (string.IsNullOrEmpty(empCode))
            {
                await trans.RollbackAsync(cancellationToken);
                return Result<Guid>.Failure("Failed to generate employee code.");
            }

            var emp = new Employee
            {
                EmployeeCode = empCode,
                FirstName = details.GetProperty("FirstName").GetString() ?? string.Empty,
                LastName = details.GetProperty("LastName").GetString() ?? string.Empty,
                Email = details.GetProperty("Email").GetString() ?? string.Empty,
                PhoneNumber = session.MobileNumber,
                Address = details.GetProperty("Address").GetString() ?? string.Empty,
                DateOfBirth = JsonSerializer.Deserialize<DateOnly>(details.GetProperty("DateOfBirth").GetRawText()),
                Gender = Enum.Parse<Gender>(details.GetProperty("Gender").GetString() ?? string.Empty, ignoreCase: true),
                DepartmentId = Guid.Parse(details.GetProperty("DepartmentId").GetString() ?? Guid.Empty.ToString()),
                DesignationId = Guid.Parse(details.GetProperty("DesignationId").GetString() ?? Guid.Empty.ToString()),
                DateOfJoining = JsonSerializer.Deserialize<DateOnly>(details.GetProperty("DateOfJoining").GetRawText()),
                EmploymentType = Enum.Parse<EmploymentType>(details.GetProperty("EmploymentType").GetString() ?? "Permanent", ignoreCase: true),
                IsActive = true,
                MobileVerified = true,
                IdentityVerified = session.IdentityVerification?.Status == IdentityVerificationStatus.Verified,
                BiometricEnrolled = true,
                IdentityVerificationReference = session.IdentityVerification?.VerificationReference,
                BiometricEnrollmentReference = session.BiometricEnrollment.EnrollmentReference,
                AadhaarNumber = details.GetProperty("AadhaarNumber").GetString() ?? string.Empty,
                AadhaarLast8Hash = SHA256.HashData(Encoding.UTF8.GetBytes((details.GetProperty("AadhaarNumber").GetString() ?? string.Empty)[^8..])),
                PhotoUrl = details.GetProperty("PhotoUrl").GetString() ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = "system"
            };

            await _dbContext.Employees.AddAsync(emp, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            session.CreatedEmployeeId = emp.Id;
            session.Status = RegistrationStatus.Completed;
            session.EmployeeDetailsJson = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await trans.CommitAsync(cancellationToken);

            _logger.LogInformation($"Registration completed. Employee: {empCode} (ID: {emp.Id})");
                return Result<Guid>.Success(emp.Id);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync(cancellationToken);
                _logger.LogError(ex, $"Error completing registration for session {request.SessionId}");
                return Result<Guid>.Failure($"Failed to complete registration: {ex.Message}");
            }
        });
    }
}
