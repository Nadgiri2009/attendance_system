using EWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWMS.Persistence.Configurations;

public class TrackingSessionConfiguration : IEntityTypeConfiguration<TrackingSession>
{
    public void Configure(EntityTypeBuilder<TrackingSession> builder)
    {
        builder.ToTable("TrackingSessions", "GPS");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.DeviceInfo).HasMaxLength(500);
        builder.Property(t => t.DeviceIp).HasMaxLength(45);

        builder.HasOne(t => t.Employee)
            .WithMany()
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // One tracking session per attendance record (one Check-In => one session).
        builder.HasOne(t => t.AttendanceRecord)
            .WithMany()
            .HasForeignKey(t => t.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.AttendanceRecordId).IsUnique();

        // "Prevent multiple active tracking sessions for the same employee":
        // enforced here (DB-level, survives races) in addition to the
        // application-level check in StartTrackingSessionCommandValidator.
        // Filtered unique index: only one row per employee where Status is
        // still 'Active' can exist at a time.
        builder.HasIndex(t => t.EmployeeId)
            .IsUnique()
            .HasFilter("[Status] = 'Active'")
            .HasDatabaseName("IX_TrackingSessions_OneActivePerEmployee");
    }
}
