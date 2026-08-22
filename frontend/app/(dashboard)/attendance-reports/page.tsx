"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { api, getErrorMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { DepartmentDto, DesignationDto, AttendanceReportResult, AttendanceAuditRow, AttendanceDashboardSummary, PaginatedList } from "@/lib/types";
import Pagination from "@/components/Pagination";
import { CircleCheck, CircleOff, Clock3, LogIn, LogOut, Percent, UsersRound, type LucideIcon } from "lucide-react";

type ReportFilters = {
  reportType: string; employeeSearch: string; departmentId: string; subDepartmentId: string; designationId: string;
  dateFrom: string; dateTo: string; month: string; year: string; status: string;
  inDepartment: string; outDepartment: string; inLocation: string; outLocation: string;
  biometricDevice: string; timeFrom: string; timeTo: string; pageNumber: number;
};

const initialFilters: ReportFilters = {
  reportType: "daily", employeeSearch: "", departmentId: "", subDepartmentId: "", designationId: "", dateFrom: new Date().toISOString().slice(0, 10), dateTo: new Date().toISOString().slice(0, 10), month: "", year: "", status: "",
  inDepartment: "", outDepartment: "", inLocation: "", outLocation: "", biometricDevice: "", timeFrom: "", timeTo: "", pageNumber: 1
};

const MONTHS = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
const CURRENT_YEAR = new Date().getFullYear();
const YEARS = Array.from({ length: 11 }, (_, index) => CURRENT_YEAR - 5 + index);

function paramsFor(filters: ReportFilters) {
  return Object.fromEntries(Object.entries(filters).map(([key, value]) => [key, value || undefined]));
}

function displayTime(value?: string | null) { return value ? new Date(value).toLocaleTimeString() : "-"; }

function AttendanceReportsPage() {
  const { user } = useAuth();
  const [filters, setFilters] = useState(initialFilters);
  const [result, setResult] = useState<AttendanceReportResult | null>(null);
  const [audit, setAudit] = useState<PaginatedList<AttendanceAuditRow> | null>(null);
  const [dashboard, setDashboard] = useState<AttendanceDashboardSummary | null>(null);
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [designations, setDesignations] = useState<DesignationDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const appliedFilters = useRef<ReportFilters>(initialFilters);

  useEffect(() => {
    api.get<{ data: DepartmentDto[] }>("/departments").then((response) => setDepartments(response.data.data)).catch(() => setDepartments([]));
  }, []);

  useEffect(() => {
    const query = filters.departmentId ? { departmentId: filters.departmentId } : undefined;
    api.get<{ data: DesignationDto[] }>("/designations", { params: query }).then((response) => setDesignations(response.data.data)).catch(() => setDesignations([]));
  }, [filters.departmentId]);

  function update(key: keyof ReportFilters, value: string | number) { setFilters((current) => ({ ...current, [key]: value })); }

  async function loadData(requestFilters: ReportFilters) {
    setLoading(true); setError(null);
    try {
      const [response, auditResponse, summaryResponse] = await Promise.all([
        api.get<{ data: AttendanceReportResult }>("/attendance/reports", { params: paramsFor(requestFilters) }),
        api.get<{ data: PaginatedList<AttendanceAuditRow> }>("/attendance/reports/audit", { params: paramsFor(requestFilters) }),
        api.get<{ data: AttendanceDashboardSummary }>("/attendance/dashboard-summary", { params: paramsFor(requestFilters) })
      ]);
      setResult(response.data.data); setAudit(auditResponse.data.data); setDashboard(summaryResponse.data.data);
    } catch (err) { setError(getErrorMessage(err, "Could not load attendance report.")); }
    finally { setLoading(false); }
  }

  async function apply(event?: FormEvent) {
    event?.preventDefault();
    appliedFilters.current = filters;
    await loadData(filters);
  }

  useEffect(() => {
    void loadData(initialFilters);
    const refresh = window.setInterval(() => void loadData(appliedFilters.current), 30000);
    return () => window.clearInterval(refresh);
  }, []);

  if (!user?.roles.includes("Admin")) {
    return <div className="rounded-lg border border-border bg-white p-8 text-center text-sm text-slate-500">Admin access is required.</div>;
  }

  function reset() { appliedFilters.current = initialFilters; setFilters(initialFilters); setResult(null); setAudit(null); setDashboard(null); setError(null); }

  async function exportReport(format: "excel" | "pdf") {
    try {
      const response = await api.get(`/attendance/reports/export/${format}`, { params: paramsFor(filters), responseType: "blob" });
      const url = URL.createObjectURL(response.data); const link = document.createElement("a"); link.href = url;
      link.download = `attendance-report.${format === "excel" ? "xlsx" : "pdf"}`; link.click(); URL.revokeObjectURL(url);
    } catch (err) { setError(getErrorMessage(err, `Could not export ${format} report.`)); }
  }

  const field = (label: string, key: keyof ReportFilters, type = "text") => (
    <label className="text-xs text-slate-500">{label}<input type={type} value={filters[key] as string} onChange={(event) => update(key, event.target.value)} className="mt-1 w-full rounded-md border border-border bg-white px-2.5 py-2 text-sm text-ink" /></label>
  );

  return <div className="space-y-6">
    <div><h1 className="font-display text-2xl text-ink">Attendance Reports</h1><p className="mt-1 text-sm text-slate-500">Filter attendance on the server and export the complete result.</p></div>
    <form onSubmit={apply} className="space-y-4 rounded-lg border border-border bg-white p-4 shadow-card">
      <div className="grid gap-3 md:grid-cols-3 lg:grid-cols-5">
        <label className="text-xs text-slate-500">Report<select value={filters.reportType} onChange={(event) => update("reportType", event.target.value)} className="mt-1 w-full rounded-md border border-border bg-white px-2.5 py-2 text-sm"><option value="daily">Daily Attendance</option><option value="employee">Employee-wise Attendance</option><option value="department">Department-wise Attendance</option><option value="monthly">Monthly Attendance</option><option value="status">Present / Absent</option><option value="late">Late Attendance</option><option value="location">IN/OUT Location</option><option value="device">Biometric Device-wise</option><option value="summary">Attendance Summary</option><option value="audit">Biometric Transaction/Audit</option></select></label>
        {field("Employee name / ID", "employeeSearch")}
        <label className="text-xs text-slate-500">Department<select value={filters.departmentId} onChange={(event) => update("departmentId", event.target.value)} className="mt-1 w-full rounded-md border border-border bg-white px-2.5 py-2 text-sm"><option value="">All departments</option>{departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        {field("Sub-department ID", "subDepartmentId")}
        <label className="text-xs text-slate-500">Designation<select value={filters.designationId} onChange={(event) => update("designationId", event.target.value)} className="mt-1 w-full rounded-md border border-border bg-white px-2.5 py-2 text-sm"><option value="">All designations</option>{designations.map((item) => <option key={item.id} value={item.id}>{item.title}</option>)}</select></label>
        <label className="text-xs text-slate-500">Status<select value={filters.status} onChange={(event) => update("status", event.target.value)} className="mt-1 w-full rounded-md border border-border bg-white px-2.5 py-2 text-sm"><option value="">All statuses</option><option>Present</option><option>Absent</option><option>Leave</option><option>HalfDay</option><option>Late</option></select></label>
        {field("Date from", "dateFrom", "date")}{field("Date to", "dateTo", "date")}
        <label className="text-xs text-slate-500">Month<select value={filters.month} onChange={(event) => update("month", event.target.value)} className="mt-1 w-full rounded-md border border-border bg-white px-2.5 py-2 text-sm"><option value="">All months</option>{MONTHS.map((month, index) => <option key={month} value={index + 1}>{month}</option>)}</select></label>
        <label className="text-xs text-slate-500">Year<select value={filters.year} onChange={(event) => update("year", event.target.value)} className="mt-1 w-full rounded-md border border-border bg-white px-2.5 py-2 text-sm"><option value="">All years</option>{YEARS.map((year) => <option key={year} value={year}>{year}</option>)}</select></label>
        {field("Time from", "timeFrom", "time")}{field("Time to", "timeTo", "time")}
        {field("IN department", "inDepartment")}{field("OUT department", "outDepartment")}{field("IN location", "inLocation")}{field("OUT location", "outLocation")}{field("Biometric device", "biometricDevice")}
      </div>
      <div className="flex flex-wrap gap-2 border-t border-border pt-4"><button type="submit" className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white">{loading ? "Applying..." : "Apply Filters"}</button><button type="button" onClick={reset} className="rounded-md border border-border px-4 py-2 text-sm text-slate-600">Reset</button><button type="button" disabled={!result} onClick={() => exportReport("excel")} className="rounded-md border border-success px-4 py-2 text-sm text-success disabled:opacity-40">Export Excel</button><button type="button" disabled={!result} onClick={() => exportReport("pdf")} className="rounded-md border border-danger px-4 py-2 text-sm text-danger disabled:opacity-40">Export PDF</button></div>
    </form>
    {error && <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-sm text-danger">{error}</div>}
    {loading && !dashboard && <div className="rounded-lg border border-border bg-white p-8 text-center text-sm text-slate-500 shadow-card">Loading current attendance statistics...</div>}
    {dashboard && <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-5">{([
      ["Total Employees", dashboard.totalEmployees, UsersRound, "border-blue-200 bg-blue-50 text-blue-700"],
      ["Present", dashboard.present, CircleCheck, "border-emerald-200 bg-emerald-50 text-emerald-700"],
      ["Absent", dashboard.absent, CircleOff, "border-rose-200 bg-rose-50 text-rose-700"],
      ["Checked In", dashboard.checkedIn, LogIn, "border-cyan-200 bg-cyan-50 text-cyan-700"],
      ["Checked Out", dashboard.checkedOut, LogOut, "border-violet-200 bg-violet-50 text-violet-700"],
      ["Attendance %", `${dashboard.attendancePercentage}%`, Percent, "border-indigo-200 bg-indigo-50 text-indigo-700"],
      ["Currently Working", dashboard.currentlyWorking, Clock3, "border-teal-200 bg-teal-50 text-teal-700"]
    ] as Array<[string, string | number, LucideIcon, string]>).map(([label, value, Icon, color]) => <button type="button" key={label} onClick={() => { const next = { ...filters, status: label === "Present" ? "Present" : "" }; appliedFilters.current = next; setFilters(next); void loadData(next); }} className={`rounded-lg border p-3 text-left shadow-card transition hover:-translate-y-0.5 ${color}`}><div className="flex items-center justify-between"><span className="text-[11px] font-medium uppercase tracking-wide">{label}</span><Icon size={16} aria-hidden="true" /></div><div className="mt-2 text-xl font-semibold text-ink">{value}</div></button>)}</div>}
    {result && <>
      <div className="overflow-x-auto rounded-lg border border-border bg-white shadow-card"><table className="min-w-[1400px] w-full text-left text-xs"><thead className="border-b border-border bg-slate-50 text-slate-500"><tr>{["Employee", "Employee Department", "Sub-Department", "Designation", "Date", "IN Time", "IN Department / Location", "IN Device", "OUT Time", "OUT Department", "OUT Location", "OUT Device", "Hours", "Status"].map((header) => <th key={header} className="whitespace-nowrap px-3 py-3 font-medium">{header}</th>)}</tr></thead><tbody className="divide-y divide-border">{result.items.map((row) => <tr key={row.attendanceId}><td className="px-3 py-3"><div className="flex items-center gap-2">{row.employeePhoto ? <img src={row.employeePhoto} alt="" className="h-8 w-8 rounded-full object-cover" /> : <div className="h-8 w-8 rounded-full bg-slate-100" />}<div><div className="font-medium text-ink">{row.employeeName}</div><div className="text-slate-400">{row.employeeCode}</div></div></div></td><td className="px-3 py-3">{row.employeeDepartment}</td><td className="px-3 py-3">{row.subDepartment || "-"}</td><td className="px-3 py-3">{row.employeeDesignation}</td><td className="px-3 py-3">{row.attendanceDate}</td><td className="px-3 py-3">{displayTime(row.inTimeUtc)}</td><td className="max-w-[220px] px-3 py-3">{row.inDepartment || row.inLocation ? <div><div>{row.inDepartment || "-"}</div><div className="mt-1 text-slate-500">{row.inLocation || "-"}</div></div> : "-"}</td><td className="px-3 py-3">{row.inBiometricDevice || "-"}</td><td className="px-3 py-3">{displayTime(row.outTimeUtc)}</td><td className="px-3 py-3">{row.outDepartment || "-"}</td><td className="px-3 py-3">{row.outLocation || "-"}</td><td className="px-3 py-3">{row.outBiometricDevice || "-"}</td><td className="px-3 py-3">{row.totalWorkingHours ?? "-"}</td><td className="px-3 py-3 font-medium">{row.attendanceStatus}</td></tr>)}</tbody></table></div>
      <Pagination pageNumber={result.pageNumber} totalPages={result.totalPages} totalCount={result.totalCount} hasPreviousPage={result.hasPreviousPage} hasNextPage={result.hasNextPage} onPageChange={(pageNumber) => { const nextFilters = { ...filters, pageNumber }; setFilters(nextFilters); void (async () => { setLoading(true); try { const [response, auditResponse] = await Promise.all([api.get<{ data: AttendanceReportResult }>("/attendance/reports", { params: paramsFor(nextFilters) }), api.get<{ data: PaginatedList<AttendanceAuditRow> }>("/attendance/reports/audit", { params: paramsFor(nextFilters) })]); setResult(response.data.data); setAudit(auditResponse.data.data); } catch (err) { setError(getErrorMessage(err, "Could not load attendance report.")); } finally { setLoading(false); } })(); }} />
      {audit && <section className="space-y-3"><h2 className="text-lg font-medium text-ink">Biometric Transaction Audit</h2><div className="overflow-x-auto rounded-lg border border-border bg-white shadow-card"><table className="min-w-[1100px] w-full text-left text-xs"><thead className="border-b border-border bg-slate-50 text-slate-500"><tr>{["Employee ID", "Employee Name", "Date", "Time", "IN/OUT", "Device ID/Name", "Device Location", "Department at Device", "Authentication Status", "Created Date/Time"].map((header) => <th key={header} className="whitespace-nowrap px-3 py-3 font-medium">{header}</th>)}</tr></thead><tbody className="divide-y divide-border">{audit.items.map((row) => <tr key={row.id}><td className="px-3 py-3">{row.employeeId}</td><td className="px-3 py-3">{row.employeeName}</td><td className="px-3 py-3">{new Date(row.dateTimeUtc).toLocaleDateString()}</td><td className="px-3 py-3">{new Date(row.dateTimeUtc).toLocaleTimeString()}</td><td className="px-3 py-3">{row.transactionType}</td><td className="px-3 py-3">{row.deviceId || row.deviceName || "-"}</td><td className="px-3 py-3">{row.deviceLocation || "-"}</td><td className="px-3 py-3">{row.departmentAtDevice || "-"}</td><td className="px-3 py-3">{row.verificationStatus}</td><td className="px-3 py-3">{new Date(row.createdAtUtc).toLocaleString()}</td></tr>)}</tbody></table></div></section>}
    </>}
  </div>;
}

export default AttendanceReportsPage;