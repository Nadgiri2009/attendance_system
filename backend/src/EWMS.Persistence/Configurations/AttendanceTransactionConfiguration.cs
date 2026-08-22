using EWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWMS.Persistence.Configurations;

public class AttendanceTransactionConfiguration : IEntityTypeConfiguration<AttendanceTransaction>
{
    public void Configure(EntityTypeBuilder<AttendanceTransaction> builder)
    {
        builder.ToTable("AttendanceTransactions", "Attendance");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.DeviceId).HasMaxLength(200);
        builder.Property(t => t.DeviceName).HasMaxLength(200);
        builder.Property(t => t.DeviceLocation).HasMaxLength(500);
        builder.Property(t => t.DepartmentAtDevice).HasMaxLength(200);
        builder.Property(t => t.TransactionType).IsRequired().HasMaxLength(10);
        builder.Property(t => t.VerificationStatus).IsRequired().HasMaxLength(20);
        builder.Property(t => t.VerificationMethod).HasMaxLength(100);
        builder.Property(t => t.VerificationReference).HasMaxLength(500);
        builder.Property(t => t.FailureReason).HasMaxLength(500);
        builder.HasIndex(t => new { t.EmployeeId, t.TransactionTimeUtc });
        builder.HasOne(t => t.Employee).WithMany().HasForeignKey(t => t.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.AttendanceRecord).WithMany().HasForeignKey(t => t.AttendanceRecordId).OnDelete(DeleteBehavior.SetNull);
    }
}