namespace EWMS.Application.Employees;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string AadhaarNumber { get; set; } = default!;
    public string Gender { get; set; } = default!;
    public DateOnly DateOfBirth { get; set; }
    public DateOnly DateOfJoining { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = default!;
    public Guid DesignationId { get; set; }
    public string DesignationTitle { get; set; } = default!;
    public Guid? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }
    public bool IsActive { get; set; }
}
