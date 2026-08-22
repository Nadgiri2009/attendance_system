using System.Linq;
using EWMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EWMS.Persistence.Seed;

public static class DbSeeder
{
    private static readonly string[] Roles = { "Admin", "HR", "Manager", "Employee" };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Skip migrations - using manual schema file
        // if (context.Database.GetMigrations().Any())
        // {
        //     await context.Database.MigrateAsync();
        // }
        // else
        // {
        //     await context.Database.EnsureCreatedAsync();
        // }

        await EnsureAuditLogTableAsync(context);
        await EnsureEmployeeRegistrationColumnsAsync(context);
        await EnsureTrackingSessionRequiredColumnsAsync(context);
        await EnsureAttendanceRecordRequiredColumnsAsync(context);
        await EnsureTotalHoursColumnTypeAsync(context);

        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName, Description = roleName + " role" });
        }

        var adminUserName = configuration["Admin:Username"] ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME");
        var adminEmail = configuration["Admin:Email"] ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = configuration["Admin:Password"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var existingAdminUser = await userManager.FindByNameAsync(adminUserName) ?? await userManager.FindByEmailAsync(adminEmail);
        if (existingAdminUser == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminUserName,
                NormalizedUserName = adminUserName.ToUpperInvariant(),
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                EmployeeId = null
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(existingAdminUser.NormalizedUserName))
                existingAdminUser.NormalizedUserName = existingAdminUser.UserName?.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(existingAdminUser.NormalizedEmail))
                existingAdminUser.NormalizedEmail = existingAdminUser.Email?.ToUpperInvariant();
            await userManager.UpdateAsync(existingAdminUser);
        }
    }

    private static async Task EnsureEmployeeRegistrationColumnsAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'Employee.Employees', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'Employee'
                    AND TABLE_NAME = 'Employees'
                    AND COLUMN_NAME = 'AadhaarNumber'
                )
                BEGIN
                    ALTER TABLE [Employee].[Employees]
                        ADD [AadhaarNumber] NVARCHAR(12) NOT NULL CONSTRAINT [DF_Employees_AadhaarNumber] DEFAULT (N'');
                END;
                IF COL_LENGTH(N'Employee.Employees', N'AadhaarLast8Hash') IS NULL
                    ALTER TABLE [Employee].[Employees] ADD [AadhaarLast8Hash] VARBINARY(32) NULL;
                EXEC(N'UPDATE [Employee].[Employees]
                    SET [AadhaarLast8Hash] = HASHBYTES(''SHA2_256'', CONVERT(VARCHAR(8), RIGHT([AadhaarNumber], 8)))
                    WHERE [AadhaarLast8Hash] IS NULL AND LEN([AadhaarNumber]) >= 8');
            END");
    }

    private static async Task EnsureAuditLogTableAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[AuditLogs]
                (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Audit_AuditLogs] PRIMARY KEY,
                    [Action] nvarchar(150) NOT NULL,
                    [EntityType] nvarchar(150) NOT NULL,
                    [EntityId] uniqueidentifier NULL,
                    [UserId] uniqueidentifier NULL,
                    [UserName] nvarchar(256) NULL,
                    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_AuditLogs_Status] DEFAULT (N'Success'),
                    [Details] nvarchar(max) NULL,
                    [TransactionReference] nvarchar(500) NULL,
                    [ErrorMessage] nvarchar(1000) NULL,
                    [IpAddress] nvarchar(45) NULL,
                    [EventAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_AuditLogs_EventAtUtc] DEFAULT (SYSUTCDATETIME()),
                    [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_AuditLogs_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
                    [CreatedBy] nvarchar(256) NULL,
                    [ModifiedAtUtc] datetime2(7) NULL,
                    [ModifiedBy] nvarchar(256) NULL,
                    [IsDeleted] bit NOT NULL CONSTRAINT [DF_AuditLogs_IsDeleted] DEFAULT (0),
                    [DeletedAtUtc] datetime2(7) NULL,
                    [DeletedBy] nvarchar(256) NULL
                );
            END");
    }

    private static async Task EnsureTrackingSessionRequiredColumnsAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'GPS.TrackingSessions', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'GPS' 
                    AND TABLE_NAME = 'TrackingSessions' 
                    AND COLUMN_NAME = 'StartAccuracyMeters'
                )
                BEGIN
                    ALTER TABLE GPS.TrackingSessions
                        ADD StartAccuracyMeters FLOAT NULL;
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'GPS' 
                    AND TABLE_NAME = 'TrackingSessions' 
                    AND COLUMN_NAME = 'StartBatteryPercent'
                )
                BEGIN
                    ALTER TABLE GPS.TrackingSessions
                        ADD StartBatteryPercent FLOAT NULL;
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'GPS' 
                    AND TABLE_NAME = 'TrackingSessions' 
                    AND COLUMN_NAME = 'DeviceIp'
                )
                BEGIN
                    ALTER TABLE GPS.TrackingSessions
                        ADD DeviceIp NVARCHAR(45) NULL;
                END;
            END");
    }

    private static async Task EnsureAttendanceRecordRequiredColumnsAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'Attendance.AttendanceRecords', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'Attendance' 
                    AND TABLE_NAME = 'AttendanceRecords' 
                    AND COLUMN_NAME = 'CheckInAddress'
                )
                BEGIN
                    ALTER TABLE Attendance.AttendanceRecords
                        ADD CheckInAddress NVARCHAR(500) NULL;
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'Attendance' 
                    AND TABLE_NAME = 'AttendanceRecords' 
                    AND COLUMN_NAME = 'CheckOutAddress'
                )
                BEGIN
                    ALTER TABLE Attendance.AttendanceRecords
                        ADD CheckOutAddress NVARCHAR(500) NULL;
                END;
            END");
    }

    private static async Task EnsureTotalHoursColumnTypeAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            @"IF OBJECT_ID(N'Attendance.AttendanceRecords', N'U') IS NOT NULL
            BEGIN
                -- Fix TotalHours column type from FLOAT to DECIMAL(10,2)
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = 'Attendance' 
                    AND TABLE_NAME = 'AttendanceRecords' 
                    AND COLUMN_NAME = 'TotalHours'
                    AND DATA_TYPE = 'real' OR DATA_TYPE = 'float'
                )
                BEGIN
                    -- Check if this is actually needed - only modify if it's float/real
                    BEGIN TRY
                        ALTER TABLE Attendance.AttendanceRecords
                            ALTER COLUMN TotalHours DECIMAL(10,2) NULL;
                    END TRY
                    BEGIN CATCH
                        -- If conversion fails, it's likely already fixed or has incompatible data
                    END CATCH;
                END;
            END");
    }
}
