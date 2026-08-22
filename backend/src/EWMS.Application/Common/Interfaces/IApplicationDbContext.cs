using EWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Department> Departments { get; }
    DbSet<Designation> Designations { get; }
    DbSet<BiometricDevice> BiometricDevices { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<AttendanceTransaction> AttendanceTransactions { get; }
    DbSet<GpsLog> GpsLogs { get; }
    DbSet<TrackingSession> TrackingSessions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
