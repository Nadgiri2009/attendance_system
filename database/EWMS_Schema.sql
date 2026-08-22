/*
    EWMS SQL Server 2022 schema
    ----------------------------
    Run this script in the target database. It creates schemas, tables, indexes,
    defaults, and logical reference columns for the current application.

    Deliberately excluded:
      - FOREIGN KEY constraints. Relationships are validated by the application.
      - Business/master seed rows. Departments, designations, roles, employees,
        devices, Aadhaar values, biometric references, SMS recipients, and
        attendance rows must be managed dynamically by the application/provider.
      - Plaintext passwords, OTPs, Aadhaar data, or biometric templates.

    The existing ASP.NET Identity seeder must create the initial admin account
    using the administrator password supplied through secure configuration. Do
    not place a plaintext password or an unverified password hash in this schema file.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF SCHEMA_ID(N'Security') IS NULL EXEC(N'CREATE SCHEMA [Security]');
IF SCHEMA_ID(N'Organization') IS NULL EXEC(N'CREATE SCHEMA [Organization]');
IF SCHEMA_ID(N'Employee') IS NULL EXEC(N'CREATE SCHEMA [Employee]');
IF SCHEMA_ID(N'Attendance') IS NULL EXEC(N'CREATE SCHEMA [Attendance]');
IF SCHEMA_ID(N'GPS') IS NULL EXEC(N'CREATE SCHEMA [GPS]');
GO

/* Organization and dropdown master data. */
IF OBJECT_ID(N'[Organization].[Departments]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[Departments]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Organization_Departments] PRIMARY KEY,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [ParentDepartmentId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_Departments_IsActive] DEFAULT (1),
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_Departments_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_Departments_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

IF OBJECT_ID(N'[Organization].[Designations]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[Designations]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Organization_Designations] PRIMARY KEY,
        [Title] nvarchar(150) NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [Level] int NOT NULL CONSTRAINT [DF_Designations_Level] DEFAULT (0),
        [IsActive] bit NOT NULL CONSTRAINT [DF_Designations_IsActive] DEFAULT (1),
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_Designations_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_Designations_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

/* Employee registration and employee identity metadata. */
IF OBJECT_ID(N'[Employee].[Employees]', N'U') IS NULL
BEGIN
    CREATE TABLE [Employee].[Employees]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Employee_Employees] PRIMARY KEY,
        [EmployeeCode] nvarchar(20) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Gender] nvarchar(10) NOT NULL,
        [DateOfBirth] date NOT NULL,
        [DateOfJoining] date NOT NULL,
        [PhotoUrl] nvarchar(1000) NULL,
        [Address] nvarchar(1000) NULL,
        [EmploymentType] int NOT NULL CONSTRAINT [DF_Employees_EmploymentType] DEFAULT (0),
        [MobileVerified] bit NOT NULL CONSTRAINT [DF_Employees_MobileVerified] DEFAULT (0),
        [IdentityVerified] bit NOT NULL CONSTRAINT [DF_Employees_IdentityVerified] DEFAULT (0),
        [BiometricEnrolled] bit NOT NULL CONSTRAINT [DF_Employees_BiometricEnrolled] DEFAULT (0),
        [AadhaarNumber] nvarchar(12) NOT NULL CONSTRAINT [DF_Employees_AadhaarNumber] DEFAULT (N''),
        [IdentityVerificationReference] nvarchar(500) NULL,
        [BiometricEnrollmentReference] nvarchar(500) NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [DesignationId] uniqueidentifier NOT NULL,
        [ReportingManagerId] uniqueidentifier NULL,
        [ApplicationUserId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_Employees_IsActive] DEFAULT (0),
        [AadhaarEncrypted] varbinary(max) NULL,
        [AadhaarLast4] char(4) NULL,
        [AadhaarLast8Hash] varbinary(32) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_Employees_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_Employees_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

/* ASP.NET Core Identity tables. IDs are application-generated GUIDs. */
IF OBJECT_ID(N'[Security].[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE [Security].[Users]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Security_Users] PRIMARY KEY,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_Users_EmailConfirmed] DEFAULT (0),
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL CONSTRAINT [DF_Users_PhoneNumberConfirmed] DEFAULT (0),
        [TwoFactorEnabled] bit NOT NULL CONSTRAINT [DF_Users_TwoFactorEnabled] DEFAULT (0),
        [LockoutEnd] datetimeoffset(7) NULL,
        [LockoutEnabled] bit NOT NULL CONSTRAINT [DF_Users_LockoutEnabled] DEFAULT (0),
        [AccessFailedCount] int NOT NULL CONSTRAINT [DF_Users_AccessFailedCount] DEFAULT (0),
        [EmployeeId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
        [LastLoginAtUtc] datetime2(7) NULL
    );
END;
GO

IF OBJECT_ID(N'[Security].[Roles]', N'U') IS NULL
BEGIN
    CREATE TABLE [Security].[Roles]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Security_Roles] PRIMARY KEY,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [Description] nvarchar(1000) NULL
    );
END;
GO

IF OBJECT_ID(N'[Security].[UserRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [Security].[UserRoles]
    (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Security_UserRoles] PRIMARY KEY ([UserId], [RoleId])
    );
END;
GO

IF OBJECT_ID(N'[Security].[UserClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [Security].[UserClaims]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Security_UserClaims] PRIMARY KEY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL
    );
END;
GO

IF OBJECT_ID(N'[Security].[UserLogins]', N'U') IS NULL
BEGIN
    CREATE TABLE [Security].[UserLogins]
    (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(256) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Security_UserLogins] PRIMARY KEY NONCLUSTERED ([LoginProvider], [ProviderKey])
    );
END;
GO

IF OBJECT_ID(N'[Security].[RoleClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [Security].[RoleClaims]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Security_RoleClaims] PRIMARY KEY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL
    );
END;
GO

IF OBJECT_ID(N'[Security].[UserTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [Security].[UserTokens]
    (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_Security_UserTokens] PRIMARY KEY NONCLUSTERED ([UserId], [LoginProvider], [Name])
    );
END;
GO

IF OBJECT_ID(N'[Security].[RefreshTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [Security].[RefreshTokens]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Security_RefreshTokens] PRIMARY KEY,
        [ApplicationUserId] uniqueidentifier NOT NULL,
        [Token] nvarchar(500) NOT NULL,
        [ExpiresAtUtc] datetime2(7) NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [RevokedAtUtc] datetime2(7) NULL,
        [ReplacedByToken] nvarchar(500) NULL
    );
END;
GO

/* Public registration, OTP rate-limit state, identity references, and biometric metadata. */
IF OBJECT_ID(N'[dbo].[RegistrationSessions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RegistrationSessions]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Registration_Sessions] PRIMARY KEY,
        [MobileNumber] nvarchar(20) NOT NULL,
        [Status] int NOT NULL CONSTRAINT [DF_RegistrationSessions_Status] DEFAULT (0),
        [ExpiresAtUtc] datetime2(7) NOT NULL,
        [OtpHash] nvarchar(500) NULL,
        [OtpAttempts] int NOT NULL CONSTRAINT [DF_RegistrationSessions_OtpAttempts] DEFAULT (0),
        [LastOtpSentAtUtc] datetime2(7) NULL,
        [OtpResendCount] int NOT NULL CONSTRAINT [DF_RegistrationSessions_OtpResendCount] DEFAULT (0),
        [EmployeeDetailsJson] nvarchar(2000) NULL,
        [CreatedEmployeeId] uniqueidentifier NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_RegistrationSessions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_RegistrationSessions_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

IF OBJECT_ID(N'[dbo].[IdentityVerifications]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IdentityVerifications]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Registration_IdentityVerifications] PRIMARY KEY,
        [RegistrationSessionId] uniqueidentifier NOT NULL,
        [Provider] nvarchar(100) NOT NULL,
        [VerificationReference] nvarchar(500) NOT NULL,
        [Status] int NOT NULL CONSTRAINT [DF_IdentityVerifications_Status] DEFAULT (0),
        [VerifiedAtUtc] datetime2(7) NULL,
        [VerificationMetadataJson] nvarchar(2000) NULL,
        [FailureReason] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_IdentityVerifications_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_IdentityVerifications_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

IF OBJECT_ID(N'[dbo].[BiometricEnrollments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BiometricEnrollments]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Registration_BiometricEnrollments] PRIMARY KEY,
        [RegistrationSessionId] uniqueidentifier NOT NULL,
        [Provider] nvarchar(100) NOT NULL,
        [EnrollmentReference] nvarchar(500) NOT NULL,
        [RequiredFingerCount] int NOT NULL CONSTRAINT [DF_BiometricEnrollments_RequiredFingerCount] DEFAULT (8),
        [EnrolledFingerCount] int NOT NULL CONSTRAINT [DF_BiometricEnrollments_EnrolledFingerCount] DEFAULT (0),
        [EnrolledFingers] nvarchar(50) NULL,
        [Status] int NOT NULL CONSTRAINT [DF_BiometricEnrollments_Status] DEFAULT (0),
        [EnrollmentStartedAtUtc] datetime2(7) NULL,
        [EnrollmentCompletedAtUtc] datetime2(7) NULL,
        [VerificationAttemptedAtUtc] datetime2(7) NULL,
        [VerificationResult] bit NULL,
        [EnrollmentMetadataJson] nvarchar(2000) NULL,
        [FailureReason] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_BiometricEnrollments_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_BiometricEnrollments_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

/* Attendance event, transaction, and exception data. */
IF OBJECT_ID(N'[Attendance].[AttendanceRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [Attendance].[AttendanceRecords]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Attendance_AttendanceRecords] PRIMARY KEY,
        [EmployeeId] uniqueidentifier NOT NULL,
        [AttendanceDate] date NOT NULL,
        [CheckInAtUtc] datetime2(7) NULL,
        [CheckInLatitude] float NULL,
        [CheckInLongitude] float NULL,
        [CheckInAddress] nvarchar(500) NULL,
        [CheckInVerificationReference] nvarchar(500) NULL,
        [CheckInDeviceId] nvarchar(200) NULL,
        [CheckOutAtUtc] datetime2(7) NULL,
        [CheckOutLatitude] float NULL,
        [CheckOutLongitude] float NULL,
        [CheckOutAddress] nvarchar(500) NULL,
        [CheckOutVerificationReference] nvarchar(500) NULL,
        [CheckOutDeviceId] nvarchar(200) NULL,
        [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_AttendanceRecords_Status] DEFAULT (N'PendingApproval'),
        [TotalHours] decimal(10,2) NULL,
        [WorkingMinutes] int NULL,
        [IsMockLocationSuspected] bit NOT NULL CONSTRAINT [DF_AttendanceRecords_IsMockLocationSuspected] DEFAULT (0),
        [Remarks] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_AttendanceRecords_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AttendanceRecords_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

IF OBJECT_ID(N'[Attendance].[AttendanceTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [Attendance].[AttendanceTransactions]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Attendance_AttendanceTransactions] PRIMARY KEY,
        [EmployeeId] uniqueidentifier NOT NULL,
        [AttendanceRecordId] uniqueidentifier NULL,
        [DeviceId] nvarchar(200) NULL,
        [DeviceName] nvarchar(200) NULL,
        [DeviceLocation] nvarchar(500) NULL,
        [DepartmentAtDevice] nvarchar(200) NULL,
        [TransactionTimeUtc] datetime2(7) NOT NULL CONSTRAINT [DF_AttendanceTransactions_TransactionTimeUtc] DEFAULT (SYSUTCDATETIME()),
        [TransactionType] nvarchar(10) NOT NULL,
        [VerificationStatus] nvarchar(20) NOT NULL,
        [VerificationMethod] nvarchar(100) NULL,
        [VerificationReference] nvarchar(500) NULL,
        [FailureReason] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_AttendanceTransactions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF COL_LENGTH(N'[Attendance].[AttendanceTransactions]', N'DeviceName') IS NULL
    ALTER TABLE [Attendance].[AttendanceTransactions] ADD [DeviceName] nvarchar(200) NULL;
IF COL_LENGTH(N'[Attendance].[AttendanceTransactions]', N'DeviceLocation') IS NULL
    ALTER TABLE [Attendance].[AttendanceTransactions] ADD [DeviceLocation] nvarchar(500) NULL;
IF COL_LENGTH(N'[Attendance].[AttendanceTransactions]', N'DepartmentAtDevice') IS NULL
    ALTER TABLE [Attendance].[AttendanceTransactions] ADD [DepartmentAtDevice] nvarchar(200) NULL;
GO

IF OBJECT_ID(N'[Attendance].[AttendanceExceptions]', N'U') IS NULL
BEGIN
    CREATE TABLE [Attendance].[AttendanceExceptions]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Attendance_AttendanceExceptions] PRIMARY KEY,
        [EmployeeId] uniqueidentifier NOT NULL,
        [AttendanceRecordId] uniqueidentifier NULL,
        [ExceptionType] nvarchar(100) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_AttendanceExceptions_Status] DEFAULT (N'Open'),
        [ResolvedByUserId] uniqueidentifier NULL,
        [ResolvedAtUtc] datetime2(7) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_AttendanceExceptions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL
    );
END;
GO

/* Live location and tracking. */
IF OBJECT_ID(N'[GPS].[TrackingSessions]', N'U') IS NULL
BEGIN
    CREATE TABLE [GPS].[TrackingSessions]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_GPS_TrackingSessions] PRIMARY KEY,
        [EmployeeId] uniqueidentifier NOT NULL,
        [AttendanceRecordId] uniqueidentifier NOT NULL,
        [StartedAtUtc] datetime2(7) NOT NULL,
        [StartLatitude] float NOT NULL,
        [StartLongitude] float NOT NULL,
        [StartAccuracyMeters] float NULL,
        [StartBatteryPercent] float NULL,
        [DeviceInfo] nvarchar(500) NULL,
        [DeviceIp] nvarchar(45) NULL,
        [EndedAtUtc] datetime2(7) NULL,
        [EndLatitude] float NULL,
        [EndLongitude] float NULL,
        [TotalDistanceMeters] float NULL,
        [TotalDurationSeconds] float NULL,
        [TotalPointsCaptured] int NOT NULL CONSTRAINT [DF_TrackingSessions_TotalPointsCaptured] DEFAULT (0),
        [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_TrackingSessions_Status] DEFAULT (N'Active'),
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_TrackingSessions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_TrackingSessions_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

IF OBJECT_ID(N'[GPS].[GpsLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [GPS].[GpsLogs]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_GPS_GpsLogs] PRIMARY KEY,
        [EmployeeId] uniqueidentifier NOT NULL,
        [AttendanceRecordId] uniqueidentifier NULL,
        [TrackingSessionId] uniqueidentifier NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [AccuracyMeters] float NULL,
        [SpeedKmh] float NULL,
        [Heading] float NULL,
        [BatteryPercent] float NULL,
        [IsMockLocation] bit NOT NULL CONSTRAINT [DF_GpsLogs_IsMockLocation] DEFAULT (0),
        [RecordedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_GpsLogs_RecordedAtUtc] DEFAULT (SYSUTCDATETIME())
    );
END;
GO

/* Device/provider configuration contains references only, never biometric templates. */
IF OBJECT_ID(N'[Attendance].[BiometricDevices]', N'U') IS NULL
BEGIN
    CREATE TABLE [Attendance].[BiometricDevices]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Attendance_BiometricDevices] PRIMARY KEY,
        [DeviceId] nvarchar(200) NOT NULL,
        [Provider] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NULL,
        [ApiUrl] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_BiometricDevices_IsActive] DEFAULT (1),
        [LastStatus] nvarchar(50) NULL,
        [LastStatusAtUtc] datetime2(7) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_BiometricDevices_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_BiometricDevices_IsDeleted] DEFAULT (0),
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

/* Security and operational audit trail. */
IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NULL
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
END;
GO

/* Unique constraints and lookup indexes. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Departments_Code' AND object_id = OBJECT_ID(N'[Organization].[Departments]'))
    CREATE UNIQUE INDEX [IX_Departments_Code] ON [Organization].[Departments]([Code]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Designations_Department_Title' AND object_id = OBJECT_ID(N'[Organization].[Designations]'))
    CREATE UNIQUE INDEX [IX_Designations_Department_Title] ON [Organization].[Designations]([DepartmentId], [Title]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_EmployeeCode' AND object_id = OBJECT_ID(N'[Employee].[Employees]'))
    CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employee].[Employees]([EmployeeCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_Email' AND object_id = OBJECT_ID(N'[Employee].[Employees]'))
    CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employee].[Employees]([Email]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_PhoneNumber' AND object_id = OBJECT_ID(N'[Employee].[Employees]'))
    CREATE UNIQUE INDEX [IX_Employees_PhoneNumber] ON [Employee].[Employees]([PhoneNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_NormalizedUserName' AND object_id = OBJECT_ID(N'[Security].[Users]'))
    CREATE UNIQUE INDEX [IX_Users_NormalizedUserName] ON [Security].[Users]([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_NormalizedEmail' AND object_id = OBJECT_ID(N'[Security].[Users]'))
    CREATE INDEX [IX_Users_NormalizedEmail] ON [Security].[Users]([NormalizedEmail]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Roles_NormalizedName' AND object_id = OBJECT_ID(N'[Security].[Roles]'))
    CREATE UNIQUE INDEX [IX_Roles_NormalizedName] ON [Security].[Roles]([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_Token' AND object_id = OBJECT_ID(N'[Security].[RefreshTokens]'))
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [Security].[RefreshTokens]([Token]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RegistrationSessions_MobileNumber_Status' AND object_id = OBJECT_ID(N'[dbo].[RegistrationSessions]'))
    CREATE INDEX [IX_RegistrationSessions_MobileNumber_Status] ON [dbo].[RegistrationSessions]([MobileNumber], [Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IdentityVerifications_RegistrationSessionId' AND object_id = OBJECT_ID(N'[dbo].[IdentityVerifications]'))
    CREATE UNIQUE INDEX [IX_IdentityVerifications_RegistrationSessionId] ON [dbo].[IdentityVerifications]([RegistrationSessionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BiometricEnrollments_EnrollmentReference_Unique' AND object_id = OBJECT_ID(N'[dbo].[BiometricEnrollments]'))
    CREATE UNIQUE INDEX [IX_BiometricEnrollments_EnrollmentReference_Unique] ON [dbo].[BiometricEnrollments]([EnrollmentReference]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BiometricEnrollments_RegistrationSessionId' AND object_id = OBJECT_ID(N'[dbo].[BiometricEnrollments]'))
    CREATE UNIQUE INDEX [IX_BiometricEnrollments_RegistrationSessionId] ON [dbo].[BiometricEnrollments]([RegistrationSessionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceRecords_EmployeeId_AttendanceDate' AND object_id = OBJECT_ID(N'[Attendance].[AttendanceRecords]'))
    CREATE UNIQUE INDEX [IX_AttendanceRecords_EmployeeId_AttendanceDate] ON [Attendance].[AttendanceRecords]([EmployeeId], [AttendanceDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AttendanceTransactions_Employee_Time' AND object_id = OBJECT_ID(N'[Attendance].[AttendanceTransactions]'))
    CREATE INDEX [IX_AttendanceTransactions_Employee_Time] ON [Attendance].[AttendanceTransactions]([EmployeeId], [TransactionTimeUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TrackingSessions_AttendanceRecordId' AND object_id = OBJECT_ID(N'[GPS].[TrackingSessions]'))
    CREATE UNIQUE INDEX [IX_TrackingSessions_AttendanceRecordId] ON [GPS].[TrackingSessions]([AttendanceRecordId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TrackingSessions_OneActivePerEmployee' AND object_id = OBJECT_ID(N'[GPS].[TrackingSessions]'))
    CREATE UNIQUE INDEX [IX_TrackingSessions_OneActivePerEmployee] ON [GPS].[TrackingSessions]([EmployeeId]) WHERE [Status] = N'Active';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GpsLogs_Employee_RecordedAt' AND object_id = OBJECT_ID(N'[GPS].[GpsLogs]'))
    CREATE INDEX [IX_GpsLogs_Employee_RecordedAt] ON [GPS].[GpsLogs]([EmployeeId], [RecordedAtUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GpsLogs_TrackingSession_RecordedAt' AND object_id = OBJECT_ID(N'[GPS].[GpsLogs]'))
    CREATE INDEX [IX_GpsLogs_TrackingSession_RecordedAt] ON [GPS].[GpsLogs]([TrackingSessionId], [RecordedAtUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BiometricDevices_DeviceId' AND object_id = OBJECT_ID(N'[Attendance].[BiometricDevices]'))
    CREATE UNIQUE INDEX [IX_BiometricDevices_DeviceId] ON [Attendance].[BiometricDevices]([DeviceId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_Entity_EventAt' AND object_id = OBJECT_ID(N'[dbo].[AuditLogs]'))
    CREATE INDEX [IX_AuditLogs_Entity_EventAt] ON [dbo].[AuditLogs]([EntityType], [EntityId], [EventAtUtc]);
GO

/* Validation: this schema intentionally contains zero FK constraints. */
IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id IN
    (
        OBJECT_ID(N'[Security].[Users]'), OBJECT_ID(N'[Security].[Roles]'), OBJECT_ID(N'[Security].[UserRoles]'),
        OBJECT_ID(N'[Security].[UserClaims]'), OBJECT_ID(N'[Security].[UserLogins]'), OBJECT_ID(N'[Security].[RoleClaims]'),
        OBJECT_ID(N'[Security].[UserTokens]'), OBJECT_ID(N'[Security].[RefreshTokens]'), OBJECT_ID(N'[Organization].[Departments]'),
        OBJECT_ID(N'[Organization].[Designations]'), OBJECT_ID(N'[Employee].[Employees]'), OBJECT_ID(N'[dbo].[RegistrationSessions]'),
        OBJECT_ID(N'[dbo].[IdentityVerifications]'), OBJECT_ID(N'[dbo].[BiometricEnrollments]'), OBJECT_ID(N'[Attendance].[AttendanceRecords]'),
        OBJECT_ID(N'[Attendance].[AttendanceTransactions]'), OBJECT_ID(N'[Attendance].[AttendanceExceptions]'), OBJECT_ID(N'[GPS].[TrackingSessions]'),
        OBJECT_ID(N'[GPS].[GpsLogs]'), OBJECT_ID(N'[Attendance].[BiometricDevices]'), OBJECT_ID(N'[dbo].[AuditLogs]')
    )
)
    THROW 51000, 'Schema validation failed: foreign key constraints are not allowed in EWMS schema.', 1;
GO
