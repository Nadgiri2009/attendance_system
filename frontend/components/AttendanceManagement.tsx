"use client";

import { useEffect, useState } from "react";
import { api, getErrorMessage } from "@/lib/api";
import { locationTracker } from "@/lib/locationTracker";
import { AttendanceDto, EmployeeDto, PaginatedList } from "@/lib/types";
import DataTable, { Column } from "@/components/DataTable";
import Pagination from "@/components/Pagination";
import AttendanceForm, { AttendanceFormValues } from "@/components/AttendanceForm";
import TrackingHistoryPanel from "@/components/TrackingHistoryPanel";

const PAGE_SIZE = 10;

const EMPTY_FORM: AttendanceFormValues = {
  employeeId: "",
  checkInAtLocal: "",
  checkOutAtLocal: "",
  status: "Present",
  remarks: ""
};

// datetime-local (local time, no timezone) <-> ISO UTC string helpers.
function localToIso(local: string): string {
  return new Date(local).toISOString();
}
function isoToLocal(iso?: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}

// BUG FIX: previously there was no Admin/HR view over attendance at all —
// only the self-service "check in for myself, today" widget above this
// component. This panel is the missing Create/Update/Delete/List/Search/
// Sort/Pagination surface for Attendance, mirroring the Employees list page.
export default function AttendanceManagement() {
  const [result, setResult] = useState<PaginatedList<AttendanceDto> | null>(null);
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [sortBy, setSortBy] = useState("attendancedate");
  const [sortDescending, setSortDescending] = useState(true);
  const [isLoading, setIsLoading] = useState(true);
  const [panel, setPanel] = useState<{ mode: "create" | "edit"; values: AttendanceFormValues; id?: string } | null>(null);
  const [listError, setListError] = useState<string | null>(null);
  const [infoMessage, setInfoMessage] = useState<string | null>(null);
  const [routePanel, setRoutePanel] = useState<{ attendanceId: string } | null>(null);

  useEffect(() => {
    api.get<{ data: PaginatedList<EmployeeDto> }>("/employees", { params: { pageSize: 200, isActive: true } })
      .then((res) => setEmployees(res.data.data.items))
      .catch((err) => {
        console.warn("[AttendanceManagement] Failed to fetch employees:", err);
      });
  }, []);

  useEffect(() => {
    setPageNumber(1);
  }, [search, statusFilter, fromDate, toDate]);

  function loadList() {
    setIsLoading(true);
    setListError(null);
    api
      .get<{ data: PaginatedList<AttendanceDto> }>("/attendance/history", {
        params: {
          search: search || undefined,
          status: statusFilter || undefined,
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
          sortBy,
          sortDescending,
          pageNumber,
          pageSize: PAGE_SIZE
        }
      })
      .then((res) => setResult(res.data.data))
      .catch(() => setListError("Could not load attendance records."))
      .finally(() => setIsLoading(false));
  }

  useEffect(() => {
    const timeout = setTimeout(loadList, 300);
    const interval = setInterval(loadList, 10000);
    return () => {
      clearTimeout(timeout);
      clearInterval(interval);
    };
  }, [search, statusFilter, fromDate, toDate, sortBy, sortDescending, pageNumber]);

  function handleSortChange(key: string) {
    if (key === sortBy) setSortDescending((prev) => !prev);
    else {
      setSortBy(key);
      setSortDescending(true);
    }
  }

  function openCreate() {
    setInfoMessage(null);
    setPanel({ mode: "create", values: EMPTY_FORM });
  }

  function openEdit(a: AttendanceDto) {
    setInfoMessage(null);
    setPanel({
      mode: "edit",
      id: a.id,
      values: {
        employeeId: a.employeeId,
        checkInAtLocal: isoToLocal(a.checkInAtUtc),
        checkOutAtLocal: isoToLocal(a.checkOutAtUtc),
        status: a.status,
        remarks: a.remarks ?? ""
      }
    });
  }

  async function handleFormSubmit(values: AttendanceFormValues) {
    const checkInIso = localToIso(values.checkInAtLocal);
    const payload = {
      employeeId: values.employeeId,
      attendanceDate: values.checkInAtLocal.slice(0, 10),
      checkInAtUtc: checkInIso,
      checkOutAtUtc: values.checkOutAtLocal ? localToIso(values.checkOutAtLocal) : null,
      status: values.status,
      remarks: values.remarks || null
    };

    let createdAttendanceId: string | null = null;

    if (panel?.mode === "create") {
      const res = await api.post<{ success: boolean; data: { id: string } }>("/attendance", payload);
      createdAttendanceId = res.data.data.id;
    } else if (panel?.mode === "edit" && panel.id) {
      await api.put(`/attendance/${panel.id}`, {
        id: panel.id,
        checkInAtUtc: payload.checkInAtUtc,
        checkOutAtUtc: payload.checkOutAtUtc,
        status: payload.status,
        remarks: payload.remarks
      });
    }

    if (panel?.mode === "create" && createdAttendanceId && payload.checkInAtUtc) {
      try {
        const storedUser = window.localStorage.getItem("ewms_user");
        const parsedUser = storedUser ? JSON.parse(storedUser) as { employeeId?: string | null } : null;
        // Critical fix: the tracking session must be started for the same employee
        // attached to the attendance record, not whichever user is currently logged in.
        // This prevents validation failures when an admin creates attendance for another employee.
        const employeeId = values.employeeId || parsedUser?.employeeId;

        if (employeeId) {
          await locationTracker.start(employeeId, createdAttendanceId);
          setInfoMessage("Attendance saved and live GPS tracking started.");
        }
      } catch (trackingErr) {
        console.warn("Manual attendance tracking could not start automatically:", trackingErr);
        setInfoMessage("Attendance saved. GPS tracking could not start automatically; you can start it manually.");
      }
    }

    setPanel(null);
    loadList();
  }

  async function handleDelete(a: AttendanceDto) {
    if (!confirm(`Delete the attendance record for ${a.employeeName} on ${a.attendanceDate}?`)) return;
    await api.delete(`/attendance/${a.id}`);
    loadList();
  }

  const columns: Column<AttendanceDto>[] = [
    { header: "Employee", sortKey: "employeename", render: (a) => a.employeeName },
    { header: "Date", sortKey: "attendancedate", render: (a) => a.attendanceDate },
    {
      header: "Check-in",
      sortKey: "checkinatutc",
      render: (a) => (a.checkInAtUtc ? new Date(a.checkInAtUtc).toLocaleString() : "—")
    },
    { header: "Check-out", render: (a) => (a.checkOutAtUtc ? new Date(a.checkOutAtUtc).toLocaleString() : "—") },
    { header: "Hours", sortKey: "totalhours", render: (a) => a.totalHours ?? "—" },
    { header: "Status", sortKey: "status", render: (a) => a.status },
    { header: "Remarks", render: (a) => a.remarks || "—" },
    {
      header: "",
      render: (a) => (
        <div className="flex gap-3">
          <button onClick={() => setRoutePanel({ attendanceId: a.id })} className="text-blue-600 hover:text-blue-700 text-sm font-medium">
            View route
          </button>
          <button onClick={() => openEdit(a)} className="text-primary-600 hover:text-primary-700 text-sm font-medium">
            Edit
          </button>
          <button onClick={() => handleDelete(a)} className="text-danger hover:underline text-sm font-medium">
            Delete
          </button>
        </div>
      )
    }
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-medium text-ink">Manage attendance records</h2>
        <button
          onClick={openCreate}
          className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700"
        >
          + Add record
        </button>
      </div>

      {panel && (
        <AttendanceForm
          mode={panel.mode}
          initialValues={panel.values}
          employees={employees}
          onSubmit={handleFormSubmit}
          onCancel={() => setPanel(null)}
        />
      )}

      {routePanel && (
        <TrackingHistoryPanel
          attendanceId={routePanel.attendanceId}
          onClose={() => setRoutePanel(null)}
        />
      )}

      <div className="flex flex-wrap gap-3">
        <input
          type="text"
          placeholder="Search by employee name or code…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="flex-1 min-w-[220px] rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
        />
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
        >
          <option value="">All statuses</option>
          <option value="Present">Present</option>
          <option value="Absent">Absent</option>
          <option value="HalfDay">Half day</option>
          <option value="OnLeave">On leave</option>
          <option value="MissedCheckOut">Missed check-out</option>
          <option value="PendingApproval">Pending approval</option>
        </select>
        <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} aria-label="From date" className="rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400" />
        <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} aria-label="To date" className="rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400" />
      </div>

      {listError && <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-sm text-danger">{listError}</div>}
      {infoMessage && <div className="rounded-md border border-success/20 bg-success/5 px-3 py-2 text-sm text-success">{infoMessage}</div>}

      {isLoading || !result ? (
        <div className="text-sm text-slate-400 py-10 text-center">Loading attendance records…</div>
      ) : (
        <>
          <DataTable
            columns={columns}
            rows={result.items}
            emptyMessage="No attendance records match your filters."
            sortState={{ sortBy, sortDescending }}
            onSortChange={handleSortChange}
          />
          <Pagination
            pageNumber={result.pageNumber}
            totalPages={result.totalPages}
            totalCount={result.totalCount}
            hasPreviousPage={result.hasPreviousPage}
            hasNextPage={result.hasNextPage}
            onPageChange={setPageNumber}
          />
        </>
      )}
    </div>
  );
}
