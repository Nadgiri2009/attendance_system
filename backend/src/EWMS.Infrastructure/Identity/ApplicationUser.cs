using Microsoft.AspNetCore.Identity;

namespace EWMS.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}
