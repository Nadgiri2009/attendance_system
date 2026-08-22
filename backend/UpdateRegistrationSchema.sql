-- Apply registration schema updates to an existing EWMS database.
-- This script changes schema only: it inserts no departments, designations,
-- employees, biometric templates, or other hardcoded business data.

IF OBJECT_ID(N'dbo.IdentityVerifications', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = N'FK_IdentityVerifications_RegistrationSessions_RegistrationSessionId'
         AND parent_object_id = OBJECT_ID(N'dbo.IdentityVerifications')
   )
    ALTER TABLE [dbo].[IdentityVerifications]
        DROP CONSTRAINT [FK_IdentityVerifications_RegistrationSessions_RegistrationSessionId];

IF OBJECT_ID(N'dbo.BiometricEnrollments', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = N'FK_BiometricEnrollments_RegistrationSessions_RegistrationSessionId'
         AND parent_object_id = OBJECT_ID(N'dbo.BiometricEnrollments')
   )
    ALTER TABLE [dbo].[BiometricEnrollments]
        DROP CONSTRAINT [FK_BiometricEnrollments_RegistrationSessions_RegistrationSessionId];

IF COL_LENGTH(N'Employee.Employees', N'Address') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [Address] NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'Employee.Employees', N'EmploymentType') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [EmploymentType] INT NOT NULL CONSTRAINT [DF_Employees_EmploymentType] DEFAULT 0;
IF COL_LENGTH(N'Employee.Employees', N'MobileVerified') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [MobileVerified] BIT NOT NULL CONSTRAINT [DF_Employees_MobileVerified] DEFAULT 0;
IF COL_LENGTH(N'Employee.Employees', N'IdentityVerified') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [IdentityVerified] BIT NOT NULL CONSTRAINT [DF_Employees_IdentityVerified] DEFAULT 0;
IF COL_LENGTH(N'Employee.Employees', N'BiometricEnrolled') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [BiometricEnrolled] BIT NOT NULL CONSTRAINT [DF_Employees_BiometricEnrolled] DEFAULT 0;
IF COL_LENGTH(N'Employee.Employees', N'AadhaarNumber') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [AadhaarNumber] NVARCHAR(12) NOT NULL CONSTRAINT [DF_Employees_AadhaarNumber] DEFAULT N'';
IF COL_LENGTH(N'Employee.Employees', N'IdentityVerificationReference') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [IdentityVerificationReference] NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'Employee.Employees', N'BiometricEnrollmentReference') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [BiometricEnrollmentReference] NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'Employee.Employees', N'AadhaarLast8Hash') IS NULL
    ALTER TABLE [Employee].[Employees] ADD [AadhaarLast8Hash] VARBINARY(32) NULL;
EXEC(N'UPDATE [Employee].[Employees]
    SET [AadhaarLast8Hash] = HASHBYTES(''SHA2_256'', CONVERT(VARCHAR(8), RIGHT([AadhaarNumber], 8)))
    WHERE [AadhaarLast8Hash] IS NULL AND LEN([AadhaarNumber]) >= 8');

-- Keep the existing database aligned with the AttendanceRecord entity.
IF COL_LENGTH(N'Attendance.AttendanceRecords', N'CheckInVerificationReference') IS NULL
    ALTER TABLE [Attendance].[AttendanceRecords] ADD [CheckInVerificationReference] NVARCHAR(500) NULL;
IF COL_LENGTH(N'Attendance.AttendanceRecords', N'CheckInDeviceId') IS NULL
    ALTER TABLE [Attendance].[AttendanceRecords] ADD [CheckInDeviceId] NVARCHAR(200) NULL;
IF COL_LENGTH(N'Attendance.AttendanceRecords', N'CheckOutVerificationReference') IS NULL
    ALTER TABLE [Attendance].[AttendanceRecords] ADD [CheckOutVerificationReference] NVARCHAR(500) NULL;
IF COL_LENGTH(N'Attendance.AttendanceRecords', N'CheckOutDeviceId') IS NULL
    ALTER TABLE [Attendance].[AttendanceRecords] ADD [CheckOutDeviceId] NVARCHAR(200) NULL;

SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (TABLE_SCHEMA = N'Employee' AND TABLE_NAME = N'Employees')
   OR TABLE_NAME IN (N'RegistrationSessions', N'IdentityVerifications', N'BiometricEnrollments')
ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION;

SELECT name AS ForeignKeyName, OBJECT_SCHEMA_NAME(parent_object_id) AS TableSchema,
       OBJECT_NAME(parent_object_id) AS TableName
FROM sys.foreign_keys
WHERE name IN (
    N'FK_IdentityVerifications_RegistrationSessions_RegistrationSessionId',
    N'FK_BiometricEnrollments_RegistrationSessions_RegistrationSessionId'
);
