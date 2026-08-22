using System.Text;
using System.Reflection;
using System.Text.Json.Serialization;
using EWMS.API.Middleware;
using EWMS.Application;
using EWMS.Infrastructure;
using EWMS.Persistence;
using EWMS.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/ewms-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ---------- Application layers ----------
builder.Services.AddApplication(Assembly.GetExecutingAssembly());
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

// ---------- Controllers ----------
// BUG FIX: enums (Gender, AttendanceStatus) are sent by the frontend as JSON
// strings (e.g. "Male"), but System.Text.Json binds enums as numbers by
// default. Without this converter, every request containing an enum value
// fails ASP.NET Core's automatic [ApiController] model validation with a 400
// *before* the request ever reaches MediatR/the handler — which is exactly
// why Employee/Attendance "submits successfully" client-side (the fetch call
// completes) but nothing is ever saved (the server rejected it up front).
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// ---------- CORS ----------
// Restored to read from configuration (Cors:AllowedOrigins in appsettings.json)
// instead of a hardcoded origin, so dev/staging/production can each allow the
// right frontend origin without a code change.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[]
                      {
                          "http://localhost:3000",
                          "http://127.0.0.1:3000"
                      };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ---------- Swagger ----------
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "EWMS API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter a valid JWT token"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---------- JWT Authentication ----------
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        ),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization();

// ---------- Health Checks ----------
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// ---------- Database Seed ----------
if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Database:AutoMigrateAndSeed"))
{
    await DbSeeder.SeedAsync(app.Services);
}

// ---------- Middleware Pipeline ----------

// Replace ExceptionHandlingMiddleware with your actual middleware class name if different
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHsts();
}

// Only redirect to HTTPS when an HTTPS endpoint is actually configured
// (avoids the "Failed to determine the https port for redirect" warning
// and dropped requests when running plain HTTP locally via launchSettings.json).
if (!string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_HTTPS_PORT"]) || app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseStaticFiles();

app.UseCors("AllowReact");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Exposed for integration tests
public partial class Program
{
}