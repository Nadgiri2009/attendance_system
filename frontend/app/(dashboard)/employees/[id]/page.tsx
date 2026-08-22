"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { api } from "@/lib/api";
import { AttendanceDto, EmployeeDto, PaginatedList } from "@/lib/types";

export default function EmployeeDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [employee, setEmployee] = useState<EmployeeDto | null>(null);
  const [attendance, setAttendance] = useState<AttendanceDto[]>([]);

  useEffect(() => {
    api.get<{ data: EmployeeDto }>(`/employees/${params.id}`)
      .then((res) => setEmployee(res.data.data))
      .catch((err) => {
        console.warn("[EmployeeDetail] Failed to fetch employee:", err);
      });
    api
      .get<{ data: PaginatedList<AttendanceDto> }>("/attendance/history", { params: { employeeId: params.id, pageSize: 10 } })
      .then((res) => setAttendance(res.data.data.items))
      .catch((err) => {
        console.warn("[EmployeeDetail] Failed to fetch attendance:", err);
      });
  }, [params.id]);

  async function handleDeactivate() {
    if (!employee) return;
    if (!confirm(`Deactivate ${employee.firstName} ${employee.lastName}?`)) return;
    await api.delete(`/employees/${employee.id}`);
    router.push("/employees");
  }

  if (!employee) return <div className="text-sm text-slate-400">Loading…</div>;

  return (
    <div className="max-w-3xl space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="font-display text-2xl text-ink">{employee.firstName} {employee.lastName}</h1>
          <p className="text-sm text-slate-500 mt-1">{employee.employeeCode} · {employee.designationTitle}, {employee.departmentName}</p>
        </div>
        <div className="flex items-center gap-4">
          <Link href={`/employees/${employee.id}/edit`} className="text-sm text-primary-600 hover:underline">
            Edit
          </Link>
          <button onClick={handleDeactivate} className="text-sm text-danger hover:underline">
            Deactivate
          </button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 bg-white border border-border rounded-lg p-6 shadow-card text-sm">
        <InfoRow label="Email" value={employee.email} />
        <InfoRow label="Phone" value={employee.phoneNumber} />
        <InfoRow label="Gender" value={employee.gender} />
        <InfoRow label="Date of joining" value={employee.dateOfJoining} />
        <InfoRow label="Reporting manager" value={employee.reportingManagerName ?? "—"} />
        <InfoRow label="Status" value={employee.isActive ? "Active" : "Inactive"} />
      </div>

      <div>
        <h2 className="text-sm font-medium text-ink mb-3">Recent attendance</h2>
        <div className="rounded-lg border border-border bg-white shadow-card divide-y divide-border">
          {attendance.length === 0 && <div className="px-4 py-8 text-center text-sm text-slate-400">No attendance records yet.</div>}
          {attendance.map((a) => (
            <div key={a.id} className="px-4 py-3 flex items-center justify-between text-sm">
              <span className="text-ink">{a.attendanceDate}</span>
              <span className="text-slate-500">
                {a.checkInAtUtc ? new Date(a.checkInAtUtc).toLocaleTimeString() : "—"} → {a.checkOutAtUtc ? new Date(a.checkOutAtUtc).toLocaleTimeString() : "—"}
              </span>
              <span className="text-xs uppercase tracking-wide text-slate-500">{a.status}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-xs uppercase tracking-wide text-slate-400">{label}</div>
      <div className="text-ink mt-0.5">{value}</div>
    </div>
  );
}
