using System.Text;
using EWMS.Domain.Entities;
using EWMS.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWMS.API.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class AdminController : BaseApiController
{
    private readonly ApplicationDbContext _dbContext;

    public AdminController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        var logs = BuildAuditQuery(query);
        var totalCount = await logs.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var pageNumber = Math.Max(query.PageNumber, 1);
        var items = await logs
            .OrderByDescending(log => log.EventAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AuditLogDto(
                log.Id,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.UserName,
                log.Status,
                log.Details,
                log.ErrorMessage,
                log.IpAddress,
                log.EventAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new
            {
                items,
                pageNumber,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                totalCount,
                hasPreviousPage = pageNumber > 1,
                hasNextPage = pageNumber * pageSize < totalCount
            }
        });
    }

    [HttpGet("audit-logs/export")]
    public async Task<IActionResult> ExportAuditLogs([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        var logs = await BuildAuditQuery(query)
            .OrderByDescending(log => log.EventAtUtc)
            .Take(10_000)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("EventAtUtc,Action,EntityType,EntityId,UserName,Status,Details,ErrorMessage,IpAddress");
        foreach (var log in logs)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                log.EventAtUtc.ToString("O"),
                log.Action,
                log.EntityType,
                log.EntityId?.ToString() ?? string.Empty,
                log.UserName ?? string.Empty,
                log.Status,
                log.Details ?? string.Empty,
                log.ErrorMessage ?? string.Empty,
                log.IpAddress ?? string.Empty
            }.Select(EscapeCsv)));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"ewms-audit-log-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private IQueryable<AuditLog> BuildAuditQuery(AuditLogQuery query)
    {
        var logs = _dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
            logs = logs.Where(log => log.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            logs = logs.Where(log => log.EntityType == query.EntityType);
        if (!string.IsNullOrWhiteSpace(query.Status))
            logs = logs.Where(log => log.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.UserName))
            logs = logs.Where(log => log.UserName != null && log.UserName.Contains(query.UserName));
        if (query.FromUtc.HasValue)
            logs = logs.Where(log => log.EventAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue)
            logs = logs.Where(log => log.EventAtUtc < query.ToUtc.Value.AddDays(1));

        return logs;
    }

    private static string EscapeCsv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}

public sealed class AuditLogQuery
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? Status { get; set; }
    public string? UserName { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? UserName,
    string Status,
    string? Details,
    string? ErrorMessage,
    string? IpAddress,
    DateTime EventAtUtc);
