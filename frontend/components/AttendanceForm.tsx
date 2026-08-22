"use client";

import { useState, FormEvent } from "react";
import { getErrorMessage } from "@/lib/api";
import { ATTENDANCE_STATUSES, EmployeeDto } from "@/lib/types";

export interface AttendanceFormValues {
  employeeId: string;
  checkInAtLocal: string; // datetime-local input value
  checkOutAtLocal: string; // datetime-local input value, "" if not set
  status: string;
  remarks: string;
}

const NOW_LOCAL = new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 16);

export default function AttendanceForm({
  mode,
  initialValues,
  employees,
  onSubmit,
  onCancel
}: {
  mode: "create" | "edit";
  initialValues: AttendanceFormValues;
  employees: EmployeeDto[];
  onSubmit: (values: AttendanceFormValues) => Promise<void>;
  onCancel: () => void;
}) {
  const [form, setForm] = useState<AttendanceFormValues>(initialValues);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function update<K extends keyof AttendanceFormValues>(key: K, value: AttendanceFormValues[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    // Client-side mirror of the server-side rule (check-out must be after
    // check-in) so the user gets instant feedback instead of a round trip.
    if (form.checkOutAtLocal && form.checkOutAtLocal <= form.checkInAtLocal) {
      setError("Check-out time must be greater than check-in time.");
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit(form);
    } catch (err) {
      setError(getErrorMessage(err, "Could not save attendance record."));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 bg-surface border border-border rounded-lg p-5">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Employee</label>
          <select
            value={form.employeeId}
            required
            disabled={mode === "edit"}
            onChange={(e) => update("employeeId", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400 disabled:bg-white disabled:text-slate-400"
          >
            <option value="">Select employee…</option>
            {employees.map((e) => (
              <option key={e.id} value={e.id}>{e.firstName} {e.lastName} ({e.employeeCode})</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Status</label>
          <select
            value={form.status}
            required
            onChange={(e) => update("status", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
          >
            <option value="">Select status…</option>
            {ATTENDANCE_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Check-in</label>
          <input
            type="datetime-local"
            required
            max={NOW_LOCAL}
            value={form.checkInAtLocal}
            onChange={(e) => update("checkInAtLocal", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
          />
          <p className="text-xs text-slate-400 mt-1">Attendance date is taken from this date.</p>
        </div>
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Check-out (optional)</label>
          <input
            type="datetime-local"
            value={form.checkOutAtLocal}
            onChange={(e) => update("checkOutAtLocal", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
          />
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-ink mb-1.5">Remarks (optional)</label>
        <textarea
          value={form.remarks}
          maxLength={1000}
          rows={2}
          onChange={(e) => update("remarks", e.target.value)}
          className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
        />
      </div>

      {error && <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-sm text-danger">{error}</div>}

      <div className="flex gap-3">
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-60"
        >
          {isSubmitting ? "Saving…" : mode === "create" ? "Add record" : "Save changes"}
        </button>
        <button type="button" onClick={onCancel} className="rounded-md px-4 py-2 text-sm font-medium text-slate-600 hover:bg-white">
          Cancel
        </button>
      </div>
    </form>
  );
}
