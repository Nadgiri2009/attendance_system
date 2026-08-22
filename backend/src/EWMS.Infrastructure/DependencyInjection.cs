using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Configuration;
using EWMS.Infrastructure.Identity;
using EWMS.Infrastructure.Services;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EWMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();

        // OTP and SMS Services
        services.AddSingleton<IOtpProvider, OtpProvider>();
        services.AddSingleton<IOtpCache, InMemoryOtpCache>();
        services.AddHttpClient<ISmsProvider, AclGatewaySmsProvider>();
        services.Configure<FeatureOptions>(configuration.GetSection("Features"));

        if (configuration.GetValue<bool>("Identity:AllowMockProvider"))
            services.AddScoped<IIdentityVerificationProvider, MockIdentityVerificationProvider>();
        else
            services.AddScoped<IIdentityVerificationProvider, UnavailableIdentityVerificationProvider>();

        if (configuration.GetValue<bool>("Biometric:AllowMockProvider"))
            services.AddSingleton<IBiometricProvider, MockBiometricProvider>();
        else if (string.Equals(configuration["Biometric:Provider"], "Http", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<HttpBiometricOptions>(configuration.GetSection("Biometric"));
            services.AddHttpClient<IBiometricProvider, HttpBiometricProvider>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<HttpBiometricOptions>>().Value;
                if (string.IsNullOrWhiteSpace(options.BaseUrl))
                    throw new InvalidOperationException("Biometric:BaseUrl must be configured when Biometric:Provider is Http.");

                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            });
        }
        else
            services.AddScoped<IBiometricProvider, UnavailableBiometricProvider>();

        return services;
    }
}
