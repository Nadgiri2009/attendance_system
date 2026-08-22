"use client";

import { useEffect, useState, FormEvent } from "react";
import { api, getErrorMessage } from "@/lib/api";
import { DepartmentDto, DesignationDto } from "@/lib/types";

export interface EmployeeFormValues {
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  aadhaarNumber: string;
  gender: string;
  dateOfBirth: string;
  dateOfJoining: string;
  departmentId: string;
  designationId: string;
  isActive: boolean;
}

const TODAY = new Date().toISOString().split("T")[0];

export default function EmployeeForm({
  mode,
  initialValues,
  onSubmit,
  submitLabel,
  onCancel
}: {
  mode: "create" | "edit";
  initialValues: EmployeeFormValues;
  onSubmit: (values: EmployeeFormValues) => Promise<void>;
  submitLabel: string;
  onCancel: () => void;
}) {
  const [form, setForm] = useState<EmployeeFormValues>(initialValues);
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [designations, setDesignations] = useState<DesignationDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    api
      .get<{ data: DepartmentDto[] }>("/departments")
      .then((res) => setDepartments(res.data.data))
      .catch((err) => {
        console.warn("[EmployeeForm] Failed to fetch departments:", err);
        setDepartments([]);
      });
  }, []);

  useEffect(() => {
    if (!form.departmentId) {
      setDesignations([]);
      return;
    }

    api
      .get<{ data: DesignationDto[] }>("/designations", { params: { departmentId: form.departmentId } })
      .then((res) => {
        setDesignations(res.data.data);
        setForm((prev) =>
          prev.designationId && res.data.data.some((d) => d.id === prev.designationId)
            ? prev
            : { ...prev, designationId: "" }
        );
      })
      .catch(() => {
        setDesignations([]);
        setForm((prev) => ({ ...prev, designationId: "" }));
      });
  }, [form.departmentId]);

  function update<K extends keyof EmployeeFormValues>(key: K, value: EmployeeFormValues[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await onSubmit(form);
    } catch (err) {
      setError(getErrorMessage(err, "Could not save employee."));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 bg-white border border-border rounded-lg p-6 shadow-card">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Employee code</label>
          <input
            type="text"
            value={form.employeeCode}
            required
            disabled={mode === "edit"}
            onChange={(e) => update("employeeCode", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400 disabled:bg-surface disabled:text-slate-400"
          />
          {mode === "edit" && <p className="text-xs text-slate-400 mt-1">Employee code cannot be changed.</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Gender</label>
          <select
            value={form.gender}
            required
            onChange={(e) => update("gender", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
          >
            <option value="">Select gender…</option>
            <option value="Male">Male</option>
            <option value="Female">Female</option>
            <option value="Other">Other</option>
          </select>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-ink mb-1.5">Aadhaar number</label>
        <input
          type="text"
          required
          inputMode="numeric"
          value={form.aadhaarNumber}
          pattern="[2-9][0-9]{11}"
          maxLength={12}
          title="Enter a valid 12-digit Aadhaar number"
          onChange={(e) => update("aadhaarNumber", e.target.value.replace(/\D/g, "").slice(0, 12))}
          className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">First name</label>
          <input type="text" required value={form.firstName} onChange={(e) => update("firstName", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400" />
        </div>
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Last name</label>
          <input type="text" required value={form.lastName} onChange={(e) => update("lastName", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400" />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Email</label>
          <input type="email" required value={form.email} onChange={(e) => update("email", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400" />
        </div>
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Phone number</label>
          <input
            type="tel"
            required
            value={form.phoneNumber}
            pattern="\d{10}"
            maxLength={10}
            title="Enter exactly 10 digits"
            placeholder="9876543210"
            onChange={(e) => update("phoneNumber", e.target.value.replace(/\D/g, "").slice(0, 10))}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Date of birth</label>
          <input
            type="date"
            required
            max={TODAY}
            value={form.dateOfBirth}
            onChange={(e) => update("dateOfBirth", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Date of joining</label>
          <input
            type="date"
            required
            value={form.dateOfJoining}
            onChange={(e) => update("dateOfJoining", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Department</label>
          <select
            value={form.departmentId}
            required
            onChange={(e) => update("departmentId", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
          >
            <option value="">Select department…</option>
            {departments.map((d) => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-ink mb-1.5">Designation</label>
          <select
            value={form.designationId}
            required
            disabled={!form.departmentId}
            onChange={(e) => update("designationId", e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400 disabled:bg-surface"
          >
            <option value="">{form.departmentId ? "Select designation…" : "Select a department first"}</option>
            {designations.map((d) => (
              <option key={d.id} value={d.id}>{d.title}</option>
            ))}
          </select>
        </div>
      </div>

      {mode === "edit" && (
        <label className="flex items-center gap-2 text-sm text-ink">
          <input type="checkbox" checked={form.isActive} onChange={(e) => update("isActive", e.target.checked)}
            className="rounded border-border" />
          Active
        </label>
      )}

      {error && <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-sm text-danger">{error}</div>}

      <div className="flex gap-3 pt-2">
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-60"
        >
          {isSubmitting ? "Saving…" : submitLabel}
        </button>
        <button type="button" onClick={onCancel} className="rounded-md px-4 py-2 text-sm font-medium text-slate-600 hover:bg-surface">
          Cancel
        </button>
      </div>
    </form>
  );
}
