using System.Net;
using System.Text.Json;
using EWMS.Application.Common.Exceptions;
using ValidationException = EWMS.Application.Common.Exceptions.ValidationException;

namespace EWMS.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // BUG FIX: this catch block had been rewritten to short-circuit
            // straight to a generic 500 for every exception — including
            // ValidationException — instead of calling HandleExceptionAsync
            // below. That meant every FluentValidation failure (all of the
            // Employee/Attendance/Tracking validation this project relies
            // on) came back as an opaque 500 with no field-level messages,
            // and NotFoundException also came back as 500 instead of 404.
            // It also serialized ex.StackTrace directly into the HTTP
            // response, which leaks internal implementation details to
            // any API client and should never happen, in development or
            // production. Restored to delegate to HandleExceptionAsync,
            // which already has the correct status-code mapping.
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (HttpStatusCode.BadRequest, "Validation failed.", (object?)validationEx.Errors),
            Microsoft.EntityFrameworkCore.DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                && sqlEx.Message.Contains("IX_AttendanceRecords_EmployeeId_AttendanceDate", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.BadRequest, "You have already checked in today.", null),
            Microsoft.EntityFrameworkCore.DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                && sqlEx.Message.Contains("IX_Employees_Email", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.BadRequest, "An employee with this email already exists.", null),
            Microsoft.EntityFrameworkCore.DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                && sqlEx.Message.Contains("IX_Employees_EmployeeCode", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.BadRequest, "An employee with this code already exists.", null),
            Microsoft.EntityFrameworkCore.DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                && sqlEx.Message.Contains("Departments", StringComparison.OrdinalIgnoreCase)
                && sqlEx.Message.Contains("Code", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.BadRequest, "A department with this code already exists.", null),
            Microsoft.EntityFrameworkCore.DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                && sqlEx.Message.Contains("BiometricDevices", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.BadRequest, "A biometric device with this ID already exists.", null),
            NotFoundException notFoundEx => (HttpStatusCode.NotFound, notFoundEx.Message, null),
            UnauthorizedException unauthorizedEx => (HttpStatusCode.Unauthorized, unauthorizedEx.Message, null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            success = false,
            message,
            errors
        });

        await context.Response.WriteAsync(payload);
    }
}
