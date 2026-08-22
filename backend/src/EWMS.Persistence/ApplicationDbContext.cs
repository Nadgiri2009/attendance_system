using EWMS.Application.Common.Interfaces;
using EWMS.Domain.Common;
using EWMS.Domain.Entities;
using EWMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EWMS.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<BiometricDevice> BiometricDevices => Set<BiometricDevice>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceTransaction> AttendanceTransactions => Set<AttendanceTransaction>();
    public DbSet<GpsLog> GpsLogs => Set<GpsLog>();
    public DbSet<TrackingSession> TrackingSessions => Set<TrackingSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RegistrationSession> RegistrationSessions => Set<RegistrationSession>();
    public DbSet<IdentityVerification> IdentityVerifications => Set<IdentityVerification>();
    public DbSet<BiometricEnrollment> BiometricEnrollments => Set<BiometricEnrollment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Map Identity tables into a dedicated "Security" schema
        builder.Entity<ApplicationUser>().ToTable("Users", "Security");
        builder.Entity<ApplicationRole>().ToTable("Roles", "Security");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "Security");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "Security");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "Security");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "Security");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "Security");

        // Global query filter for soft-deletable entities
        builder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Designation>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<BiometricDevice>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AttendanceRecord>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<TrackingSession>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<RegistrationSession>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<IdentityVerification>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<BiometricEnrollment>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AuditLog>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = ChangeTracker.Entries<AuditableEntity>()
            .Where(entry => entry.Entity is not AuditLog && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => new
            {
                Entry = entry,
                Action = entry.State == EntityState.Added ? "Created" : entry.Entity.IsDeleted ? "Deleted" : "Updated"
            })
            .ToList();

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    entry.Entity.CreatedBy = _currentUserService?.UserName;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAtUtc = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = _currentUserService?.UserName;
                    break;
            }
        }

        foreach (var auditEntry in auditEntries)
        {
            AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = $"{auditEntry.Action}{auditEntry.Entry.Metadata.ClrType.Name}",
                EntityType = auditEntry.Entry.Metadata.ClrType.Name,
                EntityId = auditEntry.Entry.Entity.Id,
                Status = "Success",
                Details = JsonSerializer.Serialize(new
                {
                    ChangedProperties = auditEntry.Entry.Properties
                        .Where(property => property.IsModified)
                        .Select(property => property.Metadata.Name)
                        .ToArray()
                }),
                EventAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUserService?.UserName
            });
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
