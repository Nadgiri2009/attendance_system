using EWMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EWMS.Persistence.Configurations;

public class BiometricDeviceConfiguration : IEntityTypeConfiguration<BiometricDevice>
{
    public void Configure(EntityTypeBuilder<BiometricDevice> builder)
    {
        builder.ToTable("BiometricDevices", "Attendance");
        builder.HasKey(device => device.Id);
        builder.Property(device => device.DeviceId).IsRequired().HasMaxLength(200);
        builder.Property(device => device.Provider).IsRequired().HasMaxLength(100);
        builder.Property(device => device.DisplayName).HasMaxLength(200);
        builder.Property(device => device.ApiUrl).HasMaxLength(1000);
        builder.HasIndex(device => device.DeviceId).IsUnique();
    }
}