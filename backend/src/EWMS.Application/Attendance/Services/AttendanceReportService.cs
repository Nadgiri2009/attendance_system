using EWMS.Application.Common.Interfaces;
using EWMS.Application.Common.Models;
using EWMS.Domain.Entities;
using EWMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Attendance.Services;

public sealed class AttendanceReportFilter
{
    public string? ReportType { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? EmployeeSearch { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? SubDepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public string? Status { get; set; }
    public string? InDepartment { get; set; }
    public string? OutDepartment { get; set; }
    public string? InLocation { get; set; }
    public string? OutLocation { get; set; }
    public string? BiometricDevice { get; set; }
    public TimeSpan? TimeFrom { get; set; }
    public TimeSpan? TimeTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class AttendanceReportRow
{
    public Guid AttendanceId { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeePhoto { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public string EmployeeCode { get; init; } = string.Empty;
    public string EmployeeDepartment { get; init; } = string.Empty;
    public string? SubDepartment { get; init; }
    public string EmployeeDesignation { get; init; } = string.Empty;
    public DateOnly AttendanceDate { get; init; }
    public DateTime? InTimeUtc { get; init; }
    public string? InDepartment { get; init; }
    public string? InLocation { get; init; }
    public string? InBiometricDevice { get; init; }
    public DateTime? OutTimeUtc { get; init; }
    public string? OutDepartment { get; init; }
    public string? OutLocation { get; init; }
    public string? OutBiometricDevice { get; init; }
    public decimal? TotalWorkingHours { get; init; }
    public string AttendanceStatus { get; init; } = string.Empty;
}

public sealed record AttendanceReportSummary(
    int WorkingDays,
    int TotalEmployees,
    int Present,
    int Absent,
    int Leave,
    int HalfDay,
    int Late,
    decimal TotalHours,
    decimal AttendancePercentage);

public sealed record AttendanceReportResult(
    IReadOnlyList<AttendanceReportRow> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    AttendanceReportSummary Summary)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;
}

public sealed record AttendanceDashboardSummary(
    int TotalEmployees,
    int Present,
    int Absent,
    int OnLeave,
    int HalfDay,
    int Late,
    int CheckedIn,
    int CheckedOut,
    decimal AttendancePercentage,
    int CurrentlyWorking);

public sealed class AttendanceAuditRow
{
    public Guid Id { get; init; }
    public string EmployeeId { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime DateTimeUtc { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string? DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceLocation { get; init; }
    public string? DepartmentAtDevice { get; init; }
    public string VerificationStatus { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}

public interface IAttendanceReportService
{
    Task<AttendanceReportResult> QueryAsync(AttendanceReportFilter filter, bool paginate, CancellationToken cancellationToken = default);
    Task<PaginatedList<AttendanceAuditRow>> QueryAuditAsync(AttendanceReportFilter filter, CancellationToken cancellationToken = default);
    Task<AttendanceDashboardSummary> GetDashboardSummaryAsync(AttendanceReportFilter filter, CancellationToken cancellationToken = default);
}

public sealed class AttendanceReportService : IAttendanceReportService
{
    private readonly IApplicationDbContext _context;

    public AttendanceReportService(IApplicationDbContext context) => _context = context;

    public async Task<AttendanceReportResult> QueryAsync(AttendanceReportFilter filter, bool paginate, CancellationToken cancellationToken = default)
    {
        NormalizeAndValidate(filter);

        var records = _context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee).ThenInclude(e => e.Department).ThenInclude(d => d.ParentDepartment)
            .Include(a => a.Employee).ThenInclude(e => e.Designation)
            .AsQueryable();

        if (filter.EmployeeId.HasValue)
            records = records.Where(a => a.EmployeeId == filter.EmployeeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.EmployeeSearch))
        {
            var search = filter.EmployeeSearch.Trim();
            records = records.Where(a => a.Employee.EmployeeCode.Contains(search) ||
                (a.Employee.FirstName + " " + a.Employee.LastName).Contains(search));
        }
        if (filter.DepartmentId.HasValue)
            records = records.Where(a => a.Employee.DepartmentId == filter.DepartmentId.Value);
        if (filter.SubDepartmentId.HasValue)
            records = records.Where(a => a.Employee.Department.ParentDepartmentId == filter.SubDepartmentId.Value);
        if (filter.DesignationId.HasValue)
            records = records.Where(a => a.Employee.DesignationId == filter.DesignationId.Value);
        if (filter.DateFrom.HasValue)
            records = records.Where(a => a.AttendanceDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            records = records.Where(a => a.AttendanceDate <= filter.DateTo.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            records = records.Where(a => a.Status.ToString() == filter.Status ||
                (filter.Status == "Leave" && a.Status.ToString() == "OnLeave") ||
                (filter.Status == "Late" && a.Remarks != null && a.Remarks.Contains("Late")));
        if (filter.TimeFrom.HasValue)
            records = records.Where(a => a.CheckInAtUtc.HasValue && a.CheckInAtUtc.Value.TimeOfDay >= filter.TimeFrom.Value);
        if (filter.TimeTo.HasValue)
            records = records.Where(a => a.CheckInAtUtc.HasValue && a.CheckInAtUtc.Value.TimeOfDay <= filter.TimeTo.Value);

        var transactions = _context.AttendanceTransactions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.InDepartment))
            records = records.Where(a => transactions.Any(t => t.AttendanceRecordId == a.Id && t.TransactionType == "IN" && t.DepartmentAtDevice != null && t.DepartmentAtDevice.Contains(filter.InDepartment)));
        if (!string.IsNullOrWhiteSpace(filter.OutDepartment))
            records = records.Where(a => transactions.Any(t => t.AttendanceRecordId == a.Id && t.TransactionType == "OUT" && t.DepartmentAtDevice != null && t.DepartmentAtDevice.Contains(filter.OutDepartment)));
        if (!string.IsNullOrWhiteSpace(filter.InLocation))
            records = records.Where(a => (a.CheckInAddress != null && a.CheckInAddress.Contains(filter.InLocation)) || transactions.Any(t => t.AttendanceRecordId == a.Id && t.TransactionType == "IN" && t.DeviceLocation != null && t.DeviceLocation.Contains(filter.InLocation)));
        if (!string.IsNullOrWhiteSpace(filter.OutLocation))
            records = records.Where(a => (a.CheckOutAddress != null && a.CheckOutAddress.Contains(filter.OutLocation)) || transactions.Any(t => t.AttendanceRecordId == a.Id && t.TransactionType == "OUT" && t.DeviceLocation != null && t.DeviceLocation.Contains(filter.OutLocation)));
        if (!string.IsNullOrWhiteSpace(filter.BiometricDevice))
            records = records.Where(a => (a.CheckInDeviceId != null && a.CheckInDeviceId.Contains(filter.BiometricDevice)) ||
                (a.CheckOutDeviceId != null && a.CheckOutDeviceId.Contains(filter.BiometricDevice)) ||
                transactions.Any(t => t.AttendanceRecordId == a.Id && ((t.DeviceId != null && t.DeviceId.Contains(filter.BiometricDevice)) || (t.DeviceName != null && t.DeviceName.Contains(filter.BiometricDevice)))));

        var totalCount = await records.CountAsync(cancellationToken);
        var summaryRows = await records.Select(a => new { a.EmployeeId, a.Status, a.TotalHours, a.Remarks }).ToListAsync(cancellationToken);
        var pageNumber = Math.Max(filter.PageNumber, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 500);
        IQueryable<AttendanceRecord> page = records.OrderByDescending(a => a.AttendanceDate).ThenBy(a => a.Employee.EmployeeCode);
        if (paginate)
            page = page.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var selected = await page.ToListAsync(cancellationToken);
        var ids = selected.Select(a => a.Id).ToList();
        var transactionRows = await transactions.Where(t => t.AttendanceRecordId.HasValue && ids.Contains(t.AttendanceRecordId.Value))
            .OrderBy(t => t.TransactionTimeUtc).ToListAsync(cancellationToken);

        var items = selected.Select(record => CreateRow(record, transactionRows.Where(t => t.AttendanceRecordId == record.Id))).ToList();
        var workingDays = CalculateWorkingDays(filter.DateFrom, filter.DateTo);
        var present = summaryRows.Count(r => r.Status.ToString() == "Present");
        var absent = summaryRows.Count(r => r.Status.ToString() == "Absent");
        var leave = summaryRows.Count(r => r.Status.ToString() == "OnLeave");
        var halfDay = summaryRows.Count(r => r.Status.ToString() == "HalfDay");
        var late = summaryRows.Count(r => r.Remarks?.Contains("Late", StringComparison.OrdinalIgnoreCase) == true);
        var totalHours = summaryRows.Sum(r => r.TotalHours ?? 0);
        var denominator = summaryRows.Count == 0 ? 0 : summaryRows.Count;
        var summary = new AttendanceReportSummary(workingDays, summaryRows.Select(r => r.EmployeeId).Distinct().Count(), present, absent, leave, halfDay, late, totalHours, denominator == 0 ? 0 : Math.Round(present * 100m / denominator, 2));

        return new AttendanceReportResult(items, totalCount, pageNumber, pageSize, summary);
    }

    public async Task<PaginatedList<AttendanceAuditRow>> QueryAuditAsync(AttendanceReportFilter filter, CancellationToken cancellationToken = default)
    {
        NormalizeAndValidate(filter);
        var transactions = _context.AttendanceTransactions.AsNoTracking().Include(t => t.Employee).AsQueryable();
        if (filter.EmployeeId.HasValue) transactions = transactions.Where(t => t.EmployeeId == filter.EmployeeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.EmployeeSearch)) transactions = transactions.Where(t => t.Employee.EmployeeCode.Contains(filter.EmployeeSearch) || (t.Employee.FirstName + " " + t.Employee.LastName).Contains(filter.EmployeeSearch));
        if (filter.DateFrom.HasValue) transactions = transactions.Where(t => t.TransactionTimeUtc >= filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (filter.DateTo.HasValue) transactions = transactions.Where(t => t.TransactionTimeUtc < filter.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (filter.Status is "Success" or "Failed") transactions = transactions.Where(t => t.VerificationStatus == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.BiometricDevice)) transactions = transactions.Where(t => (t.DeviceId != null && t.DeviceId.Contains(filter.BiometricDevice)) || (t.DeviceName != null && t.DeviceName.Contains(filter.BiometricDevice)));

        var totalCount = await transactions.CountAsync(cancellationToken);
        var pageNumber = Math.Max(filter.PageNumber, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 500);
        var items = await transactions.OrderByDescending(t => t.TransactionTimeUtc)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(t => new AttendanceAuditRow
            {
                Id = t.Id,
                EmployeeId = t.Employee.EmployeeCode,
                EmployeeName = t.Employee.FirstName + " " + t.Employee.LastName,
                DateTimeUtc = t.TransactionTimeUtc,
                TransactionType = t.TransactionType,
                DeviceId = t.DeviceId,
                DeviceName = t.DeviceName,
                DeviceLocation = t.DeviceLocation,
                DepartmentAtDevice = t.DepartmentAtDevice,
                VerificationStatus = t.VerificationStatus,
                CreatedAtUtc = t.CreatedAtUtc
            }).ToListAsync(cancellationToken);
        return new PaginatedList<AttendanceAuditRow>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<AttendanceDashboardSummary> GetDashboardSummaryAsync(AttendanceReportFilter filter, CancellationToken cancellationToken = default)
    {
        NormalizeAndValidate(filter);
        var dateFrom = filter.DateFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dateTo = filter.DateTo ?? dateFrom;

        var employees = _context.Employees.AsNoTracking().Where(e => e.IsActive);
        if (filter.EmployeeId.HasValue) employees = employees.Where(e => e.Id == filter.EmployeeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.EmployeeSearch)) employees = employees.Where(e => e.EmployeeCode.Contains(filter.EmployeeSearch) || (e.FirstName + " " + e.LastName).Contains(filter.EmployeeSearch));
        if (filter.DepartmentId.HasValue) employees = employees.Where(e => e.DepartmentId == filter.DepartmentId.Value);
        if (filter.SubDepartmentId.HasValue) employees = employees.Where(e => e.Department.ParentDepartmentId == filter.SubDepartmentId.Value);
        if (filter.DesignationId.HasValue) employees = employees.Where(e => e.DesignationId == filter.DesignationId.Value);

        var employeeIds = await employees.Select(e => e.Id).ToListAsync(cancellationToken);
        var records = await _context.AttendanceRecords.AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.AttendanceDate >= dateFrom && a.AttendanceDate <= dateTo)
            .Select(a => new { a.EmployeeId, a.Status, a.CheckInAtUtc, a.CheckOutAtUtc, a.Remarks })
            .ToListAsync(cancellationToken);
        var transactions = await _context.AttendanceTransactions.AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeId) && t.TransactionTimeUtc >= dateFrom.ToDateTime(TimeOnly.MinValue) && t.TransactionTimeUtc < dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .Select(t => new { t.EmployeeId, t.TransactionType, t.TransactionTimeUtc })
            .ToListAsync(cancellationToken);

        var latestRecords = records.GroupBy(r => r.EmployeeId).Select(g => g.OrderByDescending(r => r.CheckInAtUtc ?? DateTime.MinValue).First()).ToList();
        var present = latestRecords.Count(r => r.Status == AttendanceStatus.Present);
        var onLeave = latestRecords.Count(r => r.Status == AttendanceStatus.OnLeave);
        var halfDay = latestRecords.Count(r => r.Status == AttendanceStatus.HalfDay);
        var late = latestRecords.Count(r => r.Remarks?.Contains("Late", StringComparison.OrdinalIgnoreCase) == true);
        var transactionStates = transactions.GroupBy(t => t.EmployeeId).Select(group =>
        {
            var latestIn = group.Where(t => t.TransactionType == "IN").OrderByDescending(t => t.TransactionTimeUtc).FirstOrDefault();
            var latestOut = group.Where(t => t.TransactionType == "OUT").OrderByDescending(t => t.TransactionTimeUtc).FirstOrDefault();
            return new { LatestIn = latestIn, LatestOut = latestOut };
        }).ToList();
        var checkedIn = transactionStates.Count(s => s.LatestIn != null && (s.LatestOut == null || s.LatestIn.TransactionTimeUtc > s.LatestOut.TransactionTimeUtc));
        var checkedOut = transactionStates.Count(s => s.LatestIn != null && s.LatestOut != null && s.LatestOut.TransactionTimeUtc >= s.LatestIn.TransactionTimeUtc);
        var absent = Math.Max(employeeIds.Count - present - onLeave - halfDay, 0);
        var applicable = Math.Max(employeeIds.Count - onLeave, 0);

        return new AttendanceDashboardSummary(
            employeeIds.Count, present, absent, onLeave, halfDay, late, checkedIn, checkedOut,
            applicable == 0 ? 0 : Math.Round(present * 100m / applicable, 2), checkedIn);
    }

    private static AttendanceReportRow CreateRow(AttendanceRecord record, IEnumerable<AttendanceTransaction> source)
    {
        var transactions = source.ToList();
        var input = transactions.FirstOrDefault(t => t.TransactionType.Equals("IN", StringComparison.OrdinalIgnoreCase));
        var output = transactions.FirstOrDefault(t => t.TransactionType.Equals("OUT", StringComparison.OrdinalIgnoreCase));
        return new AttendanceReportRow
        {
            AttendanceId = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeePhoto = record.Employee.PhotoUrl ?? string.Empty,
            EmployeeName = record.Employee.FirstName + " " + record.Employee.LastName,
            EmployeeCode = record.Employee.EmployeeCode,
            EmployeeDepartment = record.Employee.Department.Name,
            SubDepartment = record.Employee.Department.ParentDepartment?.Name,
            EmployeeDesignation = record.Employee.Designation.Title,
            AttendanceDate = record.AttendanceDate,
            InTimeUtc = input?.TransactionTimeUtc ?? record.CheckInAtUtc,
            InDepartment = input?.DepartmentAtDevice,
            InLocation = input?.DeviceLocation ?? record.CheckInAddress,
            InBiometricDevice = input?.DeviceName ?? input?.DeviceId ?? record.CheckInDeviceId,
            OutTimeUtc = output?.TransactionTimeUtc ?? record.CheckOutAtUtc,
            OutDepartment = output?.DepartmentAtDevice,
            OutLocation = output?.DeviceLocation ?? record.CheckOutAddress,
            OutBiometricDevice = output?.DeviceName ?? output?.DeviceId ?? record.CheckOutDeviceId,
            TotalWorkingHours = record.TotalHours,
            AttendanceStatus = record.Status.ToString()
        };
    }

    private static void NormalizeAndValidate(AttendanceReportFilter filter)
    {
        if (filter.Month is < 1 or > 12) throw new ArgumentException("Month must be between 1 and 12.");
        if (filter.Year is < 2000 or > 2100) throw new ArgumentException("Year must be between 2000 and 2100.");
        if (filter.DateFrom.HasValue && filter.DateTo.HasValue && filter.DateFrom > filter.DateTo) throw new ArgumentException("Date From cannot be after Date To.");
        if (filter.Month.HasValue && filter.Year.HasValue)
        {
            var first = new DateOnly(filter.Year.Value, filter.Month.Value, 1);
            var last = new DateOnly(filter.Year.Value, filter.Month.Value, DateTime.DaysInMonth(filter.Year.Value, filter.Month.Value));
            filter.DateFrom = filter.DateFrom.HasValue && filter.DateFrom > first ? filter.DateFrom : first;
            filter.DateTo = filter.DateTo.HasValue && filter.DateTo < last ? filter.DateTo : last;
        }
        filter.PageNumber = Math.Max(filter.PageNumber, 1);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 500);
    }

    private static int CalculateWorkingDays(DateOnly? from, DateOnly? to)
    {
        if (!from.HasValue || !to.HasValue) return 0;
        var count = 0;
        for (var date = from.Value; date <= to.Value; date = date.AddDays(1))
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) count++;
        return count;
    }
}