namespace EWMS.Application.EmployeeRegistration;

/// <summary>
/// DTO for starting the employee registration flow.
/// </summary>
public class StartRegistrationRequest
{
    /// <summary>
    /// Mobile number to register (with country code).
    /// </summary>
    public string MobileNumber { get; set; } = default!;
}

/// <summary>
/// DTO for sending OTP.
/// </summary>
public class SendOtpRequest
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }
}

/// <summary>
/// DTO for verifying OTP.
/// </summary>
public class VerifyOtpRequest
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// OTP code entered by user.
    /// </summary>
    public string Otp { get; set; } = default!;
}

/// <summary>
/// DTO for submitting employee details.
/// </summary>
public class SubmitEmployeeDetailsRequest
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Employee's full first name.
    /// </summary>
    public string FirstName { get; set; } = default!;

    /// <summary>
    /// Employee's full last name.
    /// </summary>
    public string LastName { get; set; } = default!;

    /// <summary>
    /// Date of birth (YYYY-MM-DD format).
    /// </summary>
    public DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Gender (Male/Female/Other).
    /// </summary>
    public string Gender { get; set; } = default!;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = default!;

    /// <summary>
    /// Residential address.
    /// </summary>
    public string Address { get; set; } = default!;

    /// <summary>
    /// Department ID (from master data).
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Designation ID (from master data).
    /// </summary>
    public Guid DesignationId { get; set; }

    /// <summary>
    /// Employment type (Permanent/Contract/etc).
    /// </summary>
    public string EmploymentType { get; set; } = "Permanent";

    /// <summary>
    /// Date of joining (YYYY-MM-DD format).
    /// </summary>
    public DateOnly DateOfJoining { get; set; }
}

/// <summary>
/// DTO for identity verification step.
/// </summary>
public class VerifyIdentityRequest
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Identity input (e.g., Aadhaar number, passport, etc).
    /// Implementation-specific based on provider.
    /// </summary>
    public string IdentityInput { get; set; } = default!;
}

/// <summary>
/// DTO for starting biometric enrollment.
/// </summary>
public class StartBiometricEnrollmentRequest
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Number of required fingers (typically 8).
    /// </summary>
    public int RequiredFingers { get; set; } = 8;
}

/// <summary>
/// DTO for enrolling a single finger.
/// </summary>
public class EnrollFingerRequest
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Finger position (1-8).
    /// 1=Thumb, 2=Index, 3=Middle, 4=Ring, 5=Pinky (right hand)
    /// 6=Thumb, 7=Index, 8=Middle (left hand) - example mapping
    /// </summary>
    public int FingerNumber { get; set; }

    /// <summary>
    /// Biometric template data from device (byte array, base64 encoded in JSON).
    /// </summary>
    public string TemplateDataBase64 { get; set; } = default!;
}

/// <summary>
/// DTO for completing biometric enrollment.
/// </summary>
public class CompleteBiometricEnrollmentRequest
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Final verification template captured by the biometric device.
    /// </summary>
    public string TemplateDataBase64 { get; set; } = default!;
}

/// <summary>
/// DTO for completing the entire registration.
/// </summary>
public class CompleteRegistrationRequest
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }
}

/// <summary>
/// DTO for registration status response.
/// </summary>
public class RegistrationStatusResponse
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Current status (AwaitingOtpVerification, OtpVerified, etc).
    /// </summary>
    public string Status { get; set; } = default!;

    /// <summary>
    /// When the session expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether session has expired.
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Current step in registration flow (1-6).
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Message or error description.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// DTO for biometric enrollment status response.
/// </summary>
public class BiometricStatusResponse
{
    /// <summary>
    /// Registration session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Current biometric status.
    /// </summary>
    public string Status { get; set; } = default!;

    /// <summary>
    /// Total required fingers (typically 8).
    /// </summary>
    public int RequiredFingers { get; set; }

    /// <summary>
    /// Number of successfully enrolled fingers.
    /// </summary>
    public int EnrolledFingers { get; set; }

    /// <summary>
    /// List of enrolled finger positions.
    /// </summary>
    public List<int> EnrolledFingerNumbers { get; set; } = new();

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage => RequiredFingers > 0 
        ? (EnrolledFingers * 100) / RequiredFingers 
        : 0;

    /// <summary>
    /// Error message if enrollment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for registration completion response.
/// </summary>
public class RegistrationCompleteResponse
{
    /// <summary>
    /// Successfully created employee ID.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Generated employee code (e.g., SMC-EMP-000001).
    /// </summary>
    public string EmployeeCode { get; set; } = default!;

    /// <summary>
    /// Employee's full name.
    /// </summary>
    public string FullName { get; set; } = default!;

    /// <summary>
    /// Department name.
    /// </summary>
    public string DepartmentName { get; set; } = default!;

    /// <summary>
    /// Designation name.
    /// </summary>
    public string DesignationName { get; set; } = default!;

    /// <summary>
    /// Message confirming successful registration.
    /// </summary>
    public string Message { get; set; } = "Registration completed successfully!";
}
