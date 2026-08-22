using EWMS.Application.Attendance.Commands.CheckIn;
using EWMS.Application.Attendance.Commands.CheckOut;
using EWMS.Application.Attendance.Commands.CreateAttendance;
using EWMS.Application.Attendance.Commands.DeleteAttendance;
using EWMS.Application.Attendance.Commands.MarkAttendance;
using EWMS.Application.Attendance.Commands.UpdateAttendance;
using EWMS.Application.Attendance.Queries.GetAttendanceById;
using EWMS.Application.Attendance.Queries.GetAttendanceHistory;
using EWMS.Application.Attendance.Queries.GetTodayStatus;
using EWMS.Application.Attendance.Services;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace EWMS.API.Controllers;

[Authorize]
public class AttendanceController : BaseApiController
{
    private readonly IAttendanceReportService _attendanceReports;

    public AttendanceController(IAttendanceReportService attendanceReports)
    {
        _attendanceReports = attendanceReports;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [HttpPost("mark")]
    [AllowAnonymous]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request)
    {
        var result = await Mediator.Send(new MarkAttendanceCommand(request.AadhaarLastEight, request.TemplateDataBase64));
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { attendanceId = result.Data } });
    }

    // ---------- Employee self-service (unchanged) ----------

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(CheckInCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true, data = new { attendanceId = result.Data } });
    }

    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut(CheckOutCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true });
    }

    [HttpGet("today/{employeeId:guid}")]
    public async Task<IActionResult> GetToday(Guid employeeId)
    {
        var result = await Mediator.Send(new GetTodayStatusQuery(employeeId));
        return Ok(new { success = true, data = result });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] GetAttendanceHistoryQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("reports")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetReport([FromQuery] AttendanceReportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _attendanceReports.QueryAsync(filter, paginate: true, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("dashboard-summary")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetDashboardSummary([FromQuery] AttendanceReportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _attendanceReports.GetDashboardSummaryAsync(filter, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("reports/audit")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetReportAudit([FromQuery] AttendanceReportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _attendanceReports.QueryAuditAsync(filter, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("reports/export/excel")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> ExportExcel([FromQuery] AttendanceReportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _attendanceReports.QueryAsync(filter, paginate: false, cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Report");
        var headers = new[] { "Employee Photo", "Employee Name", "Employee ID", "Employee Department", "Sub-Department", "Designation", "Attendance Date", "IN Time", "IN Department / Location", "IN Biometric Device", "OUT Time", "OUT Department", "OUT Location", "OUT Biometric Device", "Total Working Hours", "Attendance Status" };
        for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
        for (var rowIndex = 0; rowIndex < result.Items.Count; rowIndex++)
        {
            var row = result.Items[rowIndex];
            var values = new object?[] { row.EmployeePhoto, row.EmployeeName, row.EmployeeCode, row.EmployeeDepartment, row.SubDepartment, row.EmployeeDesignation, row.AttendanceDate.ToString("yyyy-MM-dd"), row.InTimeUtc?.ToString("O"), $"{row.InDepartment ?? "-"} / {row.InLocation ?? "-"}", row.InBiometricDevice, row.OutTimeUtc?.ToString("O"), row.OutDepartment, row.OutLocation, row.OutBiometricDevice, row.TotalWorkingHours, row.AttendanceStatus };
            for (var column = 0; column < values.Length; column++) sheet.Cell(rowIndex + 2, column + 1).Value = values[column]?.ToString() ?? string.Empty;
        }
        var summaryRow = result.Items.Count + 4;
        sheet.Cell(summaryRow, 1).Value = "Summary";
        sheet.Cell(summaryRow + 1, 1).Value = "Working Days"; sheet.Cell(summaryRow + 1, 2).Value = result.Summary.WorkingDays;
        sheet.Cell(summaryRow + 2, 1).Value = "Present"; sheet.Cell(summaryRow + 2, 2).Value = result.Summary.Present;
        sheet.Cell(summaryRow + 3, 1).Value = "Absent"; sheet.Cell(summaryRow + 3, 2).Value = result.Summary.Absent;
        sheet.Cell(summaryRow + 4, 1).Value = "Leave"; sheet.Cell(summaryRow + 4, 2).Value = result.Summary.Leave;
        sheet.Cell(summaryRow + 5, 1).Value = "Half Day"; sheet.Cell(summaryRow + 5, 2).Value = result.Summary.HalfDay;
        sheet.Cell(summaryRow + 6, 1).Value = "Late"; sheet.Cell(summaryRow + 6, 2).Value = result.Summary.Late;
        sheet.Cell(summaryRow + 7, 1).Value = "Total Hours"; sheet.Cell(summaryRow + 7, 2).Value = result.Summary.TotalHours;
        sheet.Cell(summaryRow + 8, 1).Value = "Attendance %"; sheet.Cell(summaryRow + 8, 2).Value = result.Summary.AttendancePercentage;
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"attendance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet("reports/export/pdf")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> ExportPdf([FromQuery] AttendanceReportFilter filter, CancellationToken cancellationToken)
    {
        var result = await _attendanceReports.QueryAsync(filter, paginate: false, cancellationToken);
        var appliedFilters = string.Join(" | ", new[] { filter.DateFrom.HasValue ? $"From: {filter.DateFrom:yyyy-MM-dd}" : null, filter.DateTo.HasValue ? $"To: {filter.DateTo:yyyy-MM-dd}" : null, filter.Month.HasValue ? $"Month: {filter.Month}" : null, filter.Year.HasValue ? $"Year: {filter.Year}" : null, filter.Status, filter.EmployeeSearch }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var document = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.Header().Column(column =>
            {
                column.Item().Text("SOLAPUR MUNICIPAL CORPORATION").Bold().FontSize(16);
                column.Item().Text("Attendance Report").FontSize(12);
                column.Item().Text($"Applied filters: {appliedFilters}").FontSize(8);
                column.Item().Text($"Generated: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}").FontSize(8);
            });
            page.Content().Column(column =>
            {
                column.Spacing(8);
                column.Item().Text($"Working days: {result.Summary.WorkingDays}  Present: {result.Summary.Present}  Absent: {result.Summary.Absent}  Leave: {result.Summary.Leave}  Half Day: {result.Summary.HalfDay}  Late: {result.Summary.Late}  Total hours: {result.Summary.TotalHours:0.##}  Attendance %: {result.Summary.AttendancePercentage:0.##}%").FontSize(8);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { for (var i = 0; i < 11; i++) columns.RelativeColumn(); });
                    var headers = new[] { "Employee", "ID", "Department", "Designation", "Date", "IN", "IN Dept/Loc", "OUT", "OUT Dept/Loc", "Hours", "Status" };
                    table.Header(header => { foreach (var title in headers) header.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text(title).FontSize(7).Bold(); });
                    foreach (var row in result.Items)
                    {
                        var values = new[] { row.EmployeeName, row.EmployeeCode, row.EmployeeDepartment, row.EmployeeDesignation, row.AttendanceDate.ToString("yyyy-MM-dd"), FormatTime(row.InTimeUtc), $"{row.InDepartment ?? "-"} / {row.InLocation ?? "-"}", FormatTime(row.OutTimeUtc), $"{row.OutDepartment ?? "-"} / {row.OutLocation ?? "-"}", row.TotalWorkingHours?.ToString("0.##") ?? "-", row.AttendanceStatus };
                        foreach (var value in values) table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(value).FontSize(6);
                    }
                });
            });
            page.Footer().AlignCenter().Text(text => { text.Span("Page "); text.CurrentPageNumber(); });
        }));
        return File(document.GeneratePdf(), "application/pdf", $"attendance-report-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    private static string FormatTime(DateTime? value) => value?.ToString("HH:mm:ss") ?? "-";

    // ---------- Full CRUD (Admin/HR attendance management) ----------

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetAttendanceByIdQuery(id));
        return Ok(new { success = true, data = result });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Create(CreateAttendanceCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return CreatedAtAction(nameof(GetById), new { id = result.Data }, new { success = true, data = new { id = result.Data } });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Update(Guid id, UpdateAttendanceCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { success = false, message = "Route id and payload id do not match." });

        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteAttendanceCommand(id));
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors });

        return Ok(new { success = true });
    }
}

public sealed class MarkAttendanceRequest
{
    public string AadhaarLastEight { get; set; } = string.Empty;
    public string TemplateDataBase64 { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceLocation { get; set; }
    public string? DepartmentAtDevice { get; set; }
}

