"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { AttendanceDashboardSummary } from "@/lib/types";
import { ClipboardCheck, LogIn, LogOut, UsersRound } from "lucide-react";

export default function DashboardPage() {
  const { user } = useAuth();
  const [summary, setSummary] = useState<AttendanceDashboardSummary | null>(null);

  useEffect(() => {
    api
      .get<{ data: AttendanceDashboardSummary }>("/attendance/dashboard-summary")
      .then((response) => setSummary(response.data.data))
      .catch((err) => console.warn("[Dashboard] Failed to load attendance summary:", err));
  }, []);

  const summaryCards = [
    { label: "TOTAL EMPLOYEES", value: summary?.totalEmployees ?? 0, tone: "border-blue-200 bg-blue-50 text-blue-700", Icon: UsersRound },
    { label: "PRESENT", value: summary?.present ?? 0, tone: "border-emerald-200 bg-emerald-50 text-emerald-700", Icon: UsersRound },
    { label: "ABSENT", value: summary?.absent ?? 0, tone: "border-red-200 bg-red-50 text-red-700", Icon: ClipboardCheck },
    { label: "CHECK IN", value: summary?.checkedIn ?? 0, tone: "border-cyan-200 bg-cyan-50 text-cyan-700", Icon: LogIn },
    { label: "CHECK OUT", value: summary?.checkedOut ?? 0, tone: "border-indigo-200 bg-indigo-50 text-indigo-700", Icon: LogOut },
    { label: "ATTENDANCE", value: summary ? `${summary.attendancePercentage}%` : "0%", tone: "border-amber-200 bg-amber-50 text-amber-700", Icon: ClipboardCheck },
    { label: "CURRENTLY WORKING", value: summary?.currentlyWorking ?? 0, tone: "border-slate-200 bg-slate-100 text-slate-700", Icon: UsersRound }
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-display text-2xl text-ink">Attendance dashboard</h1>
        <p className="mt-1 text-sm text-slate-500">Today&apos;s workforce summary.</p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {summaryCards.map(({ label, value, tone, Icon }) => (
          <div key={label} className={`flex min-h-[150px] flex-col rounded-xl border p-5 shadow-sm ${tone}`}>
            <div className="flex items-start justify-between gap-3">
              <span className="text-[11px] font-semibold uppercase tracking-[0.16em]">{label}</span>
              <div className="rounded-md bg-white/60 p-2"><Icon size={18} aria-hidden="true" /></div>
            </div>
            <div className="mt-auto pt-7 text-4xl font-semibold leading-none text-ink">{value}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
