using EWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWMS.Persistence.Configurations;

public class BiometricEnrollmentConfiguration : IEntityTypeConfiguration<BiometricEnrollment>
{
    public void Configure(EntityTypeBuilder<BiometricEnrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RegistrationSessionId)
            .IsRequired();

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.EnrollmentReference)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(e => e.EnrollmentReference)
            .IsUnique()
            .HasDatabaseName("IX_BiometricEnrollments_EnrollmentReference_Unique");

        builder.Property(e => e.RequiredFingerCount)
            .IsRequired()
            .HasDefaultValue(8);

        builder.Property(e => e.EnrolledFingerCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.EnrolledFingers)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.EnrollmentStartedAtUtc)
            .IsRequired(false);

        builder.Property(e => e.EnrollmentCompletedAtUtc)
            .IsRequired(false);

        builder.Property(e => e.VerificationAttemptedAtUtc)
            .IsRequired(false);

        builder.Property(e => e.VerificationResult)
            .IsRequired(false);

        builder.Property(e => e.EnrollmentMetadataJson)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(e => e.FailureReason)
            .IsRequired(false)
            .HasMaxLength(500);

        // Relationship to RegistrationSession
        builder.HasOne(e => e.RegistrationSession)
            .WithOne(rs => rs.BiometricEnrollment)
            .HasForeignKey<BiometricEnrollment>(e => e.RegistrationSessionId)
            .OnDelete(DeleteBehavior.Cascade);

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
