using EWMS.Domain.Common;
using EWMS.Domain.Enums;

namespace EWMS.Domain.Entities;

public class Employee : AuditableEntity
{
    public string EmployeeCode { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public Gender Gender { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateOnly DateOfJoining { get; set; }
    public string? PhotoUrl { get; set; }
    public string AadhaarNumber { get; set; } = default!;
    public byte[]? AadhaarLast8Hash { get; set; }

    /// <summary>
    /// Employee's address (added for registration module).
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Employment type (Permanent, Contract, Temporary, etc).
    /// </summary>
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;

    /// <summary>
    /// Whether employee's mobile number has been verified via OTP.
    /// </summary>
    public bool MobileVerified { get; set; } = false;

    /// <summary>
    /// Whether employee's identity has been verified.
    /// </summary>
    public bool IdentityVerified { get; set; } = false;

    /// <summary>
    /// Whether employee's biometric (8 fingers) has been successfully enrolled.
    /// </summary>
    public bool BiometricEnrolled { get; set; } = false;

    /// <summary>
    /// Reference to the identity verification record (provider + reference).
    /// </summary>
    public string? IdentityVerificationReference { get; set; }

    /// <summary>
    /// Reference to the biometric enrollment record (provider + reference).
    /// </summary>
    public string? BiometricEnrollmentReference { get; set; }

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public Guid DesignationId { get; set; }
    public Designation Designation { get; set; } = default!;

    public Guid? ReportingManagerId { get; set; }
    public Employee? ReportingManager { get; set; }

    public Guid? ApplicationUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
