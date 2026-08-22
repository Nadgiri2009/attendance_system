using EWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWMS.Persistence.Configurations;

public class IdentityVerificationConfiguration : IEntityTypeConfiguration<IdentityVerification>
{
    public void Configure(EntityTypeBuilder<IdentityVerification> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RegistrationSessionId)
            .IsRequired();

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.VerificationReference)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.VerifiedAtUtc)
            .IsRequired(false);

        builder.Property(e => e.VerificationMetadataJson)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(e => e.FailureReason)
            .IsRequired(false)
            .HasMaxLength(500);

        // Relationship to RegistrationSession
        builder.HasOne(e => e.RegistrationSession)
            .WithOne(rs => rs.IdentityVerification)
            .HasForeignKey<IdentityVerification>(e => e.RegistrationSessionId)
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
