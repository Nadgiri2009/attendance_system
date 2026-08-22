"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { EmployeeDto } from "@/lib/types";
import EmployeeForm, { EmployeeFormValues } from "@/components/EmployeeForm";

// BUG FIX: this page didn't exist at all. The Update employee API endpoint
// (PUT /employees/{id}) worked, but there was no UI that ever called it —
// "Edit" was effectively unimplemented on the frontend.
export default function EditEmployeePage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [initialValues, setInitialValues] = useState<EmployeeFormValues | null>(null);
  // UpdateEmployeeCommand accepts ReportingManagerId, but this form has no
  // field for it. Kept out-of-band and passed straight through on submit so
  // saving the form doesn't silently null out an employee's existing manager.
  const [reportingManagerId, setReportingManagerId] = useState<string | null>(null);

  useEffect(() => {
    api.get<{ data: EmployeeDto }>(`/employees/${params.id}`)
      .then((res) => {
        const e = res.data.data;
        setReportingManagerId(e.reportingManagerId ?? null);
        setInitialValues({
          employeeCode: e.employeeCode,
          firstName: e.firstName,
          lastName: e.lastName,
          email: e.email,
          phoneNumber: e.phoneNumber,
          aadhaarNumber: e.aadhaarNumber ?? "",
          gender: e.gender,
          dateOfBirth: e.dateOfBirth.split("T")[0],
          dateOfJoining: e.dateOfJoining.split("T")[0],
          departmentId: e.departmentId,
          designationId: e.designationId,
          isActive: e.isActive
        });
      })
      .catch((err) => {
        console.warn("[EditEmployee] Failed to fetch employee:", err);
      });
  }, [params.id]);

  async function handleUpdate(values: EmployeeFormValues) {
    await api.put(`/employees/${params.id}`, { id: params.id, ...values, reportingManagerId });
    router.push(`/employees/${params.id}`);
  }

  if (!initialValues) return <div className="text-sm text-slate-400">Loading…</div>;

  return (
    <div className="max-w-2xl">
      <h1 className="font-display text-2xl text-ink mb-1">Edit employee</h1>
      <p className="text-sm text-slate-500 mb-6">
        Employee code, date of birth, and date of joining can be corrected here if entered incorrectly.
      </p>
      <EmployeeForm
        mode="edit"
        initialValues={initialValues}
        submitLabel="Save changes"
        onSubmit={handleUpdate}
        onCancel={() => router.back()}
      />
    </div>
  );
}
