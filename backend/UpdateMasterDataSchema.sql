SET XACT_ABORT ON;
GO

IF SCHEMA_ID(N'Organization') IS NULL EXEC(N'CREATE SCHEMA [Organization]');
IF SCHEMA_ID(N'Attendance') IS NULL EXEC(N'CREATE SCHEMA [Attendance]');
GO

IF OBJECT_ID(N'[Organization].[Departments]', N'U') IS NULL
BEGIN
    CREATE TABLE [Organization].[Departments]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Organization_Departments] PRIMARY KEY,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [ParentDepartmentId] uniqueidentifier NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_MasterDepartments_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_MasterDepartments_IsDeleted] DEFAULT 0,
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
        [Level] int NOT NULL CONSTRAINT [DF_MasterDesignations_Level] DEFAULT 1,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_MasterDesignations_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_MasterDesignations_IsDeleted] DEFAULT 0,
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

IF OBJECT_ID(N'[Attendance].[BiometricDevices]', N'U') IS NULL
BEGIN
    CREATE TABLE [Attendance].[BiometricDevices]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Attendance_BiometricDevices] PRIMARY KEY,
        [DeviceId] nvarchar(200) NOT NULL,
        [Provider] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NULL,
        [ApiUrl] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_MasterBiometricDevices_IsActive] DEFAULT 1,
        [LastStatus] nvarchar(50) NULL,
        [LastStatusAtUtc] datetime2(7) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL CONSTRAINT [DF_MasterBiometricDevices_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [CreatedBy] nvarchar(256) NULL,
        [ModifiedAtUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_MasterBiometricDevices_IsDeleted] DEFAULT 0,
        [DeletedAtUtc] datetime2(7) NULL,
        [DeletedBy] nvarchar(256) NULL
    );
END;
GO

/* Add fields when an older installation already has one of the tables. */
IF COL_LENGTH(N'[Organization].[Departments]', N'ParentDepartmentId') IS NULL ALTER TABLE [Organization].[Departments] ADD [ParentDepartmentId] uniqueidentifier NULL;
IF COL_LENGTH(N'[Organization].[Departments]', N'IsDeleted') IS NULL ALTER TABLE [Organization].[Departments] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_MasterDepartments_IsDeleted_2] DEFAULT 0;
IF COL_LENGTH(N'[Organization].[Designations]', N'Level') IS NULL ALTER TABLE [Organization].[Designations] ADD [Level] int NOT NULL CONSTRAINT [DF_MasterDesignations_Level_2] DEFAULT 1;
IF COL_LENGTH(N'[Organization].[Designations]', N'IsDeleted') IS NULL ALTER TABLE [Organization].[Designations] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_MasterDesignations_IsDeleted_2] DEFAULT 0;
GO

/* Master records are intentionally independent; application validation handles relationships. */
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' + CHAR(10)
FROM sys.foreign_keys
WHERE parent_object_id IN (OBJECT_ID(N'[Organization].[Departments]'), OBJECT_ID(N'[Organization].[Designations]'), OBJECT_ID(N'[Attendance].[BiometricDevices]'));
IF @sql <> N'' EXEC sys.sp_executesql @sql;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MasterDepartments_Code' AND object_id = OBJECT_ID(N'[Organization].[Departments]'))
    CREATE UNIQUE INDEX [IX_MasterDepartments_Code] ON [Organization].[Departments]([Code]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MasterBiometricDevices_DeviceId' AND object_id = OBJECT_ID(N'[Attendance].[BiometricDevices]'))
    CREATE UNIQUE INDEX [IX_MasterBiometricDevices_DeviceId] ON [Attendance].[BiometricDevices]([DeviceId]);
GO