using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EWMS.Application.Attendance.Commands.MarkAttendance;

public record MarkAttendanceCommand(
    string AadhaarLastEight,
    string TemplateDataBase64,
    string? DeviceId = null,
    string? DeviceName = null,
    string? DeviceLocation = null,
    string? DepartmentAtDevice = null) : IRequest<Result<Guid>>;

public class MarkAttendanceCommandHandler : IRequestHandler<MarkAttendanceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IBiometricProvider _biometricProvider;
    private readonly IDateTimeService _dateTime;

    public MarkAttendanceCommandHandler(
        IApplicationDbContext context,
        IBiometricProvider biometricProvider,
        IDateTimeService dateTime)
    {
        _context = context;
        _biometricProvider = biometricProvider;
        _dateTime = dateTime;
    }

    public async Task<Result<Guid>> Handle(MarkAttendanceCommand request, CancellationToken cancellationToken)
    {
        var lastEight = request.AadhaarLastEight?.Trim() ?? string.Empty;
        if (lastEight.Length != 8 || !lastEight.All(char.IsDigit))
            return Result<Guid>.Failure("Enter the last 8 digits of your Aadhaar number.");

        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(lastEight));
        var employees = await _context.Employees
            .Where(e => e.IsActive && !e.IsDeleted)
            .ToListAsync(cancellationToken);
        var employee = employees.FirstOrDefault(e =>
            (e.AadhaarLast8Hash is not null && CryptographicOperations.FixedTimeEquals(e.AadhaarLast8Hash, suppliedHash)) ||
            (e.AadhaarLast8Hash is null && e.AadhaarNumber.EndsWith(lastEight, StringComparison.Ordinal)));
        if (employee is null)
            return Result<Guid>.Failure("The Aadhaar digits do not match your employee record.");

        if (string.IsNullOrWhiteSpace(employee.BiometricEnrollmentReference))
            return Result<Guid>.Failure("Biometric enrollment is not available for this employee.");

        byte[] templateData;
        try { templateData = Convert.FromBase64String(request.TemplateDataBase64); }
        catch { return Result<Guid>.Failure("A valid fingerprint scan is required."); }
        if (templateData.Length == 0)
            return Result<Guid>.Failure("A valid fingerprint scan is required.");

        var today = _dateTime.TodayUtc;
        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == today && !a.IsDeleted, cancellationToken);
        var transactionType = existing?.CheckInAtUtc is not null && existing.CheckOutAtUtc is null ? "OUT" : "IN";
        var verification = await _biometricProvider.VerifyBiometricAsync(employee.BiometricEnrollmentReference, templateData, cancellationToken);
        if (!verification.IsSuccess)
        {
            _context.AttendanceTransactions.Add(CreateTransaction(employee.Id, transactionType, request, verification.Message, "Failed", null));
            await _context.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Failure("Fingerprint verification failed. Attendance was not marked.");
        }

        var now = _dateTime.UtcNow;
        var record = existing ?? new AttendanceRecord
        {
            EmployeeId = employee.Id,
            AttendanceDate = today,
            CheckInAtUtc = now,
            Status = AttendanceStatus.Present
        };

        if (existing is not null)
        {
            if (existing.CheckInAtUtc is null)
                existing.CheckInAtUtc = now;
            else if (existing.CheckOutAtUtc is null)
            {
                existing.CheckOutAtUtc = now;
                existing.TotalHours = Math.Round((decimal)(now - existing.CheckInAtUtc.Value).TotalHours, 2);
            }
            else
                return Result<Guid>.Failure("Attendance for today has already been completed.");
        }
        else
        {
            _context.AttendanceRecords.Add(record);
        }

        _context.AttendanceTransactions.Add(CreateTransaction(employee.Id, transactionType, request, null, "Success", record.Id, employee.BiometricEnrollmentReference));
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(record.Id);
    }

    private AttendanceTransaction CreateTransaction(Guid employeeId, string type, MarkAttendanceCommand request, string? failureReason, string status, Guid? attendanceRecordId, string? verificationReference = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = employeeId,
        AttendanceRecordId = attendanceRecordId,
        DeviceId = request.DeviceId,
        DeviceName = request.DeviceName,
        DeviceLocation = request.DeviceLocation,
        DepartmentAtDevice = request.DepartmentAtDevice,
        TransactionTimeUtc = _dateTime.UtcNow,
        TransactionType = type,
        VerificationStatus = status,
        VerificationMethod = _biometricProvider.ProviderName,
        VerificationReference = verificationReference,
        FailureReason = failureReason,
        CreatedAtUtc = _dateTime.UtcNow
    };
}