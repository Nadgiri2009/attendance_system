using EWMS.Application.Common.Interfaces;
using EWMS.Infrastructure.Identity;
using EWMS.Persistence.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EWMS.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set it in appsettings.json or via the ConnectionStrings__DefaultConnection environment variable.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // BUG FIX: this method previously (1) printed the raw connection
        // string — including the DB password when SQL auth is used — to
        // stdout, a credential leak into logs/console output that's
        // unacceptable in any environment, let alone production; and
        // (2) called `services.BuildServiceProvider()` here to
        // synchronously test connectivity during DI *registration*, before
        // the real app has even finished starting. Building a second,
        // throwaway service provider is a well-known ASP.NET Core
        // anti-pattern, and because the connectivity check `throw`s on
        // failure, it meant the entire API would refuse to start any time
        // SQL Server wasn't already reachable at that exact moment — e.g.
        // any normal container/orchestration startup race. Connectivity
        // is already correctly handled by `EnableRetryOnFailure` above
        // (query-time transient retry) and the `/health` endpoint
        // (`AddHealthChecks().AddSqlServer(...)` in Program.cs) — both
        // check the real, fully-built application, not a throwaway one.

        // Employee Code Generator
        services.AddScoped<IEmployeeCodeGenerator, EmployeeCodeGenerator>();

        return services;
    }
}