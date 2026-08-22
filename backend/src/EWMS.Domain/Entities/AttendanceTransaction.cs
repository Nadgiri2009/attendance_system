namespace EWMS.Domain.Entities;

public class AttendanceTransaction
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public Guid? AttendanceRecordId { get; set; }
    public AttendanceRecord? AttendanceRecord { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceLocation { get; set; }
    public string? DepartmentAtDevice { get; set; }
    public DateTime TransactionTimeUtc { get; set; }
    public string TransactionType { get; set; } = default!;
    public string VerificationStatus { get; set; } = default!;
    public string? VerificationMethod { get; set; }
    public string? VerificationReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}