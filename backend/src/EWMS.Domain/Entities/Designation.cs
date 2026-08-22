using EWMS.Domain.Common;

namespace EWMS.Domain.Entities;

public class Designation : AuditableEntity
{
    public string Title { get; set; } = default!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
    public int Level { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
