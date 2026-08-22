using System.Security.Cryptography;
using System.Text;
using EWMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace EWMS.Infrastructure.Services;

/// <summary>
/// Default OTP provider implementation using cryptographic hash.
/// </summary>
public class OtpProvider : IOtpProvider
{
    private readonly IPasswordHasher<object> _hasher;

    public OtpProvider()
    {
        _hasher = new PasswordHasher<object>();
    }

    /// <summary>
    /// Generate a random 6-digit numeric OTP.
    /// </summary>
    public string GenerateOtp()
    {
        const int length = 6;
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] randomBytes = new byte[length];
            rng.GetBytes(randomBytes);

            // Convert to numeric string
            var otp = string.Empty;
            for (int i = 0; i < length; i++)
            {
                otp += (randomBytes[i] % 10).ToString();
            }

            return otp;
        }
    }

    /// <summary>
    /// Hash OTP using ASP.NET Core Identity's password hasher.
    /// </summary>
    public string HashOtp(string otp)
    {
        return _hasher.HashPassword(null, otp);
    }

    /// <summary>
    /// Verify OTP against stored hash.
    /// </summary>
    public bool VerifyOtp(string plainOtp, string hashedOtp)
    {
        if (string.IsNullOrEmpty(plainOtp) || string.IsNullOrEmpty(hashedOtp))
            return false;

        var result = _hasher.VerifyHashedPassword(null, hashedOtp, plainOtp);
        return result == PasswordVerificationResult.Success;
    }
}
