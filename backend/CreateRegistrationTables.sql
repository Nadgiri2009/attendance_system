-- Create RegistrationSessions table
CREATE TABLE [RegistrationSessions] (
    [Id] uniqueidentifier NOT NULL,
    [MobileNumber] nvarchar(20) NOT NULL,
    [Status] int NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [OtpAttempts] int NOT NULL DEFAULT 0,
    [LastOtpSentAtUtc] datetime2 NULL,
    [OtpResendCount] int NOT NULL DEFAULT 0,
    [EmployeeDetailsJson] nvarchar(2000) NULL,
    [CreatedEmployeeId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedAtUtc] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT 0,
    [DeletedAtUtc] datetime2 NULL,
    [DeletedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_RegistrationSessions] PRIMARY KEY ([Id])
);

-- Create IdentityVerifications table
CREATE TABLE [IdentityVerifications] (
    [Id] uniqueidentifier NOT NULL,
    [RegistrationSessionId] uniqueidentifier NOT NULL,
    [Provider] nvarchar(100) NOT NULL,
    [VerificationReference] nvarchar(500) NOT NULL,
    [Status] int NOT NULL,
    [VerifiedAtUtc] datetime2 NULL,
    [VerificationMetadataJson] nvarchar(2000) NULL,
    [FailureReason] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedAtUtc] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT 0,
    [DeletedAtUtc] datetime2 NULL,
    [DeletedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_IdentityVerifications] PRIMARY KEY ([Id])
);

-- Create BiometricEnrollments table
CREATE TABLE [BiometricEnrollments] (
    [Id] uniqueidentifier NOT NULL,
    [RegistrationSessionId] uniqueidentifier NOT NULL,
    [Provider] nvarchar(100) NOT NULL,
    [EnrollmentReference] nvarchar(500) NOT NULL,
    [RequiredFingerCount] int NOT NULL DEFAULT 8,
    [EnrolledFingerCount] int NOT NULL DEFAULT 0,
    [EnrolledFingers] nvarchar(50) NULL,
    [Status] int NOT NULL,
    [EnrollmentStartedAtUtc] datetime2 NULL,
    [EnrollmentCompletedAtUtc] datetime2 NULL,
    [VerificationAttemptedAtUtc] datetime2 NULL,
    [VerificationResult] bit NULL,
    [EnrollmentMetadataJson] nvarchar(2000) NULL,
    [FailureReason] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedAtUtc] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL DEFAULT 0,
    [DeletedAtUtc] datetime2 NULL,
    [DeletedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_BiometricEnrollments] PRIMARY KEY ([Id]),
    UNIQUE([EnrollmentReference])
);

-- Create indexes
CREATE INDEX [IX_RegistrationSessions_MobileNumber_Status] ON [RegistrationSessions] ([MobileNumber], [Status]);
CREATE UNIQUE INDEX [IX_IdentityVerifications_RegistrationSessionId] ON [IdentityVerifications] ([RegistrationSessionId]);
CREATE UNIQUE INDEX [IX_BiometricEnrollments_RegistrationSessionId] ON [BiometricEnrollments] ([RegistrationSessionId]);
CREATE UNIQUE INDEX [IX_BiometricEnrollments_EnrollmentReference_Unique] ON [BiometricEnrollments] ([EnrollmentReference]);
