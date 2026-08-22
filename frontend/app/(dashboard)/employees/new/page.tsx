"use client";

import { useRouter } from "next/navigation";
import AdminEmployeeRegistration from "@/components/AdminEmployeeRegistration";

export default function NewEmployeePage() {
  const router = useRouter();

  return (
    <div className="mx-auto w-full max-w-4xl space-y-6">
      <div>
        <h1 className="font-display text-2xl text-ink">Create Employee</h1>
        <p className="mt-1 text-sm text-slate-500">Complete each step to create an employee record.</p>
      </div>
      <AdminEmployeeRegistration onClose={() => router.push("/employees")} />
    </div>
  );
}
