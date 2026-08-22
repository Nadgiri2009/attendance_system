using EWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWMS.Persistence.Configurations;

public class RegistrationSessionConfiguration : IEntityTypeConfiguration<RegistrationSession>
{
    public void Configure(EntityTypeBuilder<RegistrationSession> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MobileNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.ExpiresAtUtc)
            .IsRequired();

        builder.Property(e => e.OtpAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.OtpResendCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.EmployeeDetailsJson)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedEmployeeId)
            .IsRequired(false);

        // Navigation properties are configured on the child entities
        // IdentityVerification and BiometricEnrollment configure their relationships

        // Index for faster lookups
        builder.HasIndex(e => new { e.MobileNumber, e.Status })
            .HasDatabaseName("IX_RegistrationSessions_MobileNumber_Status");

        // Audit fields
        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.ModifiedAtUtc)
            .IsRequired(false);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAtUtc)
            .IsRequired(false);
    }
}
