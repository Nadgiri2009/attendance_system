using EWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWMS.Persistence.Configurations;

public class GpsLogConfiguration : IEntityTypeConfiguration<GpsLog>
{
    public void Configure(EntityTypeBuilder<GpsLog> builder)
    {
        builder.ToTable("GpsLogs", "GPS");
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => new { g.EmployeeId, g.RecordedAtUtc });

        // Powers GetTrackingHistoryQuery: "all points for this session, in order".
        builder.HasIndex(g => new { g.TrackingSessionId, g.RecordedAtUtc });

        // Prevent a cascade cycle: AttendanceRecord can also delete or null out
        // the same GpsLog rows through its own relationship, so this FK must not
        // cascade on deletion of the tracking session.
        builder.HasOne(g => g.TrackingSession)
            .WithMany(t => t.LocationPoints)
            .HasForeignKey(g => g.TrackingSessionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
