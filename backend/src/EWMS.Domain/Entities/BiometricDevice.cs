using EWMS.Domain.Common;

namespace EWMS.Domain.Entities;

public class BiometricDevice : AuditableEntity
{
    public string DeviceId { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? ApiUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LastStatus { get; set; }
    public DateTime? LastStatusAtUtc { get; set; }
}