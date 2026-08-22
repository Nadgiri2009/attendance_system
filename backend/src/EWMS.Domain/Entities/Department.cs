using EWMS.Domain.Common;

namespace EWMS.Domain.Entities;

public class Department : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Designation> Designations { get; set; } = new List<Designation>();
}
