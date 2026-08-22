using System.Reflection;
using EWMS.Application.Common.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using EWMS.Application.Attendance.Services;

namespace EWMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IAttendanceReportService, AttendanceReportService>();

        // Collect all assemblies for handler registration
        var assembliesToScan = new[] { assembly }.Concat(additionalAssemblies).Distinct().ToArray();

        services.AddMediatR(cfg =>
        {
            // Register handlers from Application assembly
            cfg.RegisterServicesFromAssembly(assembly);
            
            // Register handlers from additional assemblies (e.g., API layer)
            foreach (var additionalAssembly in additionalAssemblies)
            {
                cfg.RegisterServicesFromAssembly(additionalAssembly);
            }
            
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
        });

        return services;
    }
}
