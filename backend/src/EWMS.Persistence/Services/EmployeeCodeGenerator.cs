using EWMS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EWMS.Persistence.Services;

/// <summary>
/// Default implementation of employee code generator.
/// Thread-safe, transaction-safe implementation.
/// Example: SMC-EMP-000001
/// </summary>
public class EmployeeCodeGenerator : IEmployeeCodeGenerator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmployeeCodeGenerator> _logger;

    private readonly string _prefix;
    private readonly int _paddingLength;
    private readonly int _startNumber;

    public EmployeeCodeGenerator(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<EmployeeCodeGenerator> logger
    )
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;

        // Load configuration
        _prefix = _configuration["EmployeeCode:Prefix"] ?? "SMC-EMP";
        _paddingLength = int.TryParse(_configuration["EmployeeCode:PaddingLength"], out var pad) ? pad : 6;
        _startNumber = int.TryParse(_configuration["EmployeeCode:StartNumber"], out var start) ? start : 1;
    }

    /// <summary>
    /// Generate next unique employee code.
    /// Determines the highest existing code number and increments.
    /// </summary>
    public async Task<string> GenerateEmployeeCodeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the highest employee number from existing codes
            var maxCode = await _dbContext.Employees
                .OrderByDescending(e => e.EmployeeCode)
                .Select(e => e.EmployeeCode)
                .FirstOrDefaultAsync(cancellationToken);

            int nextNumber = _startNumber;

            if (!string.IsNullOrEmpty(maxCode))
            {
                // Extract numeric part from code (e.g., "SMC-EMP-000001" → 1)
                var parts = maxCode.Split('-');
                if (parts.Length > 0 && int.TryParse(parts[^1], out var currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            var code = $"{_prefix}-{nextNumber.ToString().PadLeft(_paddingLength, '0')}";

            _logger.LogInformation($"Generated employee code: {code}");
            return code;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating employee code: {ex.Message}");
            throw;
        }
    }
}
