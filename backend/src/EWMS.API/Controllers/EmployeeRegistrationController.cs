using EWMS.Application.Common.Models;
using EWMS.Application.EmployeeRegistration.Commands;
using EWMS.Application.EmployeeRegistration.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWMS.API.Controllers;

/// <summary>
/// Employee Registration Controller - handles the multi-step employee registration workflow.
/// This endpoint is restricted to administrators.
/// </summary>
[Authorize(Roles = "Admin")]
public class EmployeeRegistrationController : BaseApiController
{
    /// <summary>
    /// Step 1: Initiate registration by providing mobile number.
    /// </summary>
    /// <param name="request">Request containing mobile number</param>
    /// <returns>Registration session ID if successful</returns>
    [HttpPost("start")]
    public async Task<IActionResult> StartRegistration([FromBody] StartRegistrationRequest request)
    {
        var command = new StartRegistrationCommand(request.MobileNumber);
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { sessionId = result.Data } });
    }

    /// <summary>Resume a registration session at its persisted workflow step.</summary>
    [HttpGet("status/{sessionId:guid}")]
    public async Task<IActionResult> GetRegistrationStatus(Guid sessionId)
    {
        var result = await Mediator.Send(new GetRegistrationStatusQuery { SessionId = sessionId });
        if (!result.Succeeded || result.Data is null)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = result.Data });
    }

    /// <summary>
    /// Step 1: Send OTP to the mobile number.
    /// </summary>
    /// <param name="request">Request containing session ID</param>
    /// <returns>Success status</returns>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        var command = new SendOtpCommand(request.SessionId);
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { sent = true } });
    }

    /// <summary>
    /// Step 1: Verify OTP entered by the user.
    /// </summary>
    /// <param name="request">Request containing session ID and OTP</param>
    /// <returns>Success status</returns>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var command = new VerifyOtpCommand(request.SessionId, request.Otp);
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { verified = true } });
    }

    /// <summary>
    /// Step 2: Submit employee details (name, email, department, etc.).
    /// </summary>
    /// <param name="request">Request containing employee details</param>
    /// <returns>Success status</returns>
    [HttpPost("details")]
    public async Task<IActionResult> SubmitEmployeeDetails([FromForm] SubmitEmployeeDetailsRequest request)
    {
        if (request.Photo is null || request.Photo.Length == 0)
            return BadRequest(new { success = false, errors = new[] { "Employee photo is required." } });

        if (request.Photo.Length > 5 * 1024 * 1024)
            return BadRequest(new { success = false, errors = new[] { "Employee photo must be 5 MB or smaller." } });

        if (!new[] { "image/jpeg", "image/png", "image/webp" }.Contains(request.Photo.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { success = false, errors = new[] { "Employee photo must be a JPEG, PNG, or WebP image." } });

        await using var photoStream = new MemoryStream();
        await request.Photo.CopyToAsync(photoStream);
        var photoBytes = photoStream.ToArray();
        if (!IsSupportedImageSignature(photoBytes))
            return BadRequest(new { success = false, errors = new[] { "The uploaded file is not a valid image." } });

        var command = new SubmitEmployeeDetailsCommand
        {
            SessionId = request.SessionId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address ?? string.Empty,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            DateOfJoining = request.DateOfJoining,
            EmploymentType = request.EmploymentType,
            AadhaarNumber = request.AadhaarNumber,
            PhotoBytes = photoBytes,
            PhotoContentType = request.Photo.ContentType
        };
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { submitted = true } });
    }

    /// <summary>
    /// Step 3: Verify identity against external provider.
    /// </summary>
    /// <param name="request">Request containing session ID and identity verification input</param>
    /// <returns>Success status</returns>
    [HttpPost("identity/verify")]
    public async Task<IActionResult> VerifyIdentity([FromBody] VerifyIdentityRequest request)
    {
        var command = new VerifyIdentityCommand
        {
            SessionId = request.SessionId,
            IdentityInput = request.IdentityInput
        };
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { verified = true } });
    }

    /// <summary>
    /// Step 4: Start biometric enrollment process.
    /// </summary>
    /// <param name="request">Request containing session ID</param>
    /// <returns>Biometric enrollment reference</returns>
    [HttpPost("biometric/start")]
    public async Task<IActionResult> StartBiometricEnrollment([FromBody] StartBiometricEnrollmentRequest request)
    {
        var command = new StartBiometricEnrollmentCommand
        {
            SessionId = request.SessionId,
            RequiredFingers = request.RequiredFingers
        };
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { started = true } });
    }

    /// <summary>
    /// Step 4: Enroll a single fingerprint.
    /// </summary>
    /// <param name="request">Request containing session ID, finger number, and template data</param>
    /// <returns>Success status with finger count</returns>
    [HttpPost("biometric/finger")]
    public async Task<IActionResult> EnrollFinger([FromBody] EnrollFingerRequest request)
    {
        var command = new EnrollFingerCommand
        {
            SessionId = request.SessionId,
            FingerNumber = request.FingerNumber,
            TemplateDataBase64 = request.TemplateDataBase64
        };
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { enrolled = true } });
    }

    /// <summary>
    /// Complete eight-finger enrollment after all required fingers are enrolled.
    /// </summary>
    [HttpPost("biometric/complete")]
    public async Task<IActionResult> CompleteBiometricEnrollment([FromBody] CompleteBiometricEnrollmentRequest request)
    {
        var command = new CompleteBiometricEnrollmentCommand
        {
            SessionId = request.SessionId
        };
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { completed = true } });
    }

    /// <summary>
    /// Step 4: Get current biometric enrollment status and progress.
    /// </summary>
    /// <param name="sessionId">Registration session ID</param>
    /// <returns>Biometric status including enrolled fingers and progress percentage</returns>
    [HttpGet("biometric/status")]
    public async Task<IActionResult> GetBiometricStatus([FromQuery] Guid sessionId)
    {
        var query = new GetBiometricStatusQuery { SessionId = sessionId };
        var result = await Mediator.Send(query);

        if (!result.Succeeded || result.Data is null)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new
        {
            success = true,
            data = new
            {
                result.Data.SessionId,
                result.Data.Status,
                result.Data.RequiredFingers,
                result.Data.EnrolledFingers,
                result.Data.EnrolledFingerNumbers,
                result.Data.ProgressPercentage,
                result.Data.ErrorMessage
            }
        });
    }

    /// <summary>
    /// Step 5: Complete registration and create employee record.
    /// </summary>
    /// <param name="request">Request containing session ID for completion</param>
    /// <returns>Employee details with confirmation</returns>
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
    {
        var command = new CompleteRegistrationCommand { SessionId = request.SessionId };
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { completed = true, employeeId = result.Data } });
    }

    private static bool IsSupportedImageSignature(byte[] bytes) =>
        (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) ||
        (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) ||
        (bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8));
}

// ===================== REQUEST DTOs =====================

/// <summary>Request to start registration with mobile number.</summary>
public class StartRegistrationRequest
{
    public string MobileNumber { get; set; } = string.Empty;
}

/// <summary>Request to send OTP.</summary>
public class SendOtpRequest
{
    public Guid SessionId { get; set; }
}

/// <summary>Request to verify OTP.</summary>
public class VerifyOtpRequest
{
    public Guid SessionId { get; set; }
    public string Otp { get; set; } = string.Empty;
}

/// <summary>Request to submit employee details.</summary>
public class SubmitEmployeeDetailsRequest
{
    public Guid SessionId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Address { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid DesignationId { get; set; }
    public DateOnly DateOfJoining { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string AadhaarNumber { get; set; } = string.Empty;
    public IFormFile? Photo { get; set; }
}

/// <summary>Request to verify identity.</summary>
public class VerifyIdentityRequest
{
    public Guid SessionId { get; set; }
    public string IdentityInput { get; set; } = string.Empty;
}

/// <summary>Request to start biometric enrollment.</summary>
public class StartBiometricEnrollmentRequest
{
    public Guid SessionId { get; set; }
    public int RequiredFingers { get; set; } = 8;
}

/// <summary>Request to enroll a finger.</summary>
public class EnrollFingerRequest
{
    public Guid SessionId { get; set; }
    public int FingerNumber { get; set; }
    public string TemplateDataBase64 { get; set; } = string.Empty;
}

/// <summary>Request to complete biometric enrollment.</summary>
public class CompleteBiometricEnrollmentRequest
{
    public Guid SessionId { get; set; }
}

/// <summary>Request to complete registration.</summary>
public class CompleteRegistrationRequest
{
    public Guid SessionId { get; set; }
}
