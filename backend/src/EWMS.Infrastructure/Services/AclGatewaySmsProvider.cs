using EWMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EWMS.Infrastructure.Services;

public sealed class AclGatewaySmsProvider : ISmsProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AclGatewaySmsProvider> _logger;

    public AclGatewaySmsProvider(HttpClient httpClient, IConfiguration configuration, ILogger<AclGatewaySmsProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AclSms:BaseUrl"];
        var appId = _configuration["AclSms:AppId"];
        var userId = _configuration["AclSms:UserId"];
        var password = _configuration["AclSms:Password"];
        var sender = _configuration["AclSms:Sender"];
        var dltTemplateId = _configuration["AclSms:DltTemplateId"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(dltTemplateId))
        {
            _logger.LogError("ACL Gateway SMS configuration is incomplete. Configure AclSms settings.");
            return null;
        }

        var recipient = NormalizeIndianNumber(phoneNumber);
        if (recipient is null)
        {
            _logger.LogWarning("ACL Gateway SMS was not sent because the destination phone number is invalid.");
            return null;
        }

        var query = string.Join("&", new Dictionary<string, string>
        {
            ["appid"] = appId,
            ["userId"] = userId,
            ["pass"] = password,
            ["contenttype"] = "1",
            ["from"] = sender,
            ["to"] = recipient,
            ["text"] = message,
            ["alert"] = "1",
            ["selfid"] = "true",
            ["dlrreq"] = "true",
            ["dtm"] = dltTemplateId
        }.Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));

        try
        {
            using var response = await _httpClient.GetAsync($"{baseUrl.TrimEnd('?') }?{query}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("ACL Gateway rejected SMS. HTTP status: {StatusCode}, response: {Response}", response.StatusCode, body);
                return null;
            }

            _logger.LogInformation("SMS sent successfully through ACL Gateway. HTTP status: {StatusCode}", response.StatusCode);
            return body.Trim();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Error sending SMS through ACL Gateway.");
            return null;
        }
    }

    public Task<string?> SendOtpAsync(string phoneNumber, string otp, int expiryMinutes, CancellationToken cancellationToken = default) =>
        SendSmsAsync(phoneNumber, $"Your OTP is: {otp}. This is valid for {expiryMinutes} minutes. Do not share this with anyone.", cancellationToken);

    private static string? NormalizeIndianNumber(string phoneNumber)
    {
        var value = new string((phoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (value.StartsWith("0091", StringComparison.Ordinal)) value = value[4..];
        if (value.StartsWith("91", StringComparison.Ordinal) && value.Length == 12) value = value[2..];
        return value.Length == 10 && value[0] is >= '6' and <= '9' ? $"91{value}" : null;
    }
}