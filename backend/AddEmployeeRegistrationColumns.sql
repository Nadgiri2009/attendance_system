-- Add registration fields without inserting seed data or creating foreign keys.
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

-- Verify columns were added
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'Employee' AND TABLE_NAME = 'Employees'
ORDER BY ORDINAL_POSITION;
