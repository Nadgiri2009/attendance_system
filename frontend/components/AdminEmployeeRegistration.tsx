"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, getErrorMessage } from "@/lib/api";
import { DepartmentDto, DesignationDto } from "@/lib/types";
import BiometricEnrollment from "@/components/BiometricEnrollment";
import { Check } from "lucide-react";

const TODAY = new Date().toISOString().slice(0, 10);
const INITIAL_FORM = {
  mobileNumber: "",
  otp: "",
  firstName: "",
  lastName: "",
  email: "",
  gender: "Male",
  dateOfBirth: "",
  address: "",
  departmentId: "",
  designationId: "",
  dateOfJoining: TODAY,
  employmentType: "Permanent",
  aadhaarNumber: "",
  photoFile: null as File | null
};

export default function AdminEmployeeRegistration({ onClose }: { onClose: () => void }) {
  const [form, setForm] = useState(INITIAL_FORM);
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [designations, setDesignations] = useState<DesignationDto[]>([]);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [step, setStep] = useState<1 | 2 | 3 | 4>(1);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    api.get<{ data: DepartmentDto[] }>("/departments").then((response) => setDepartments(response.data.data)).catch(() => setDepartments([]));
  }, []);

  useEffect(() => {
    if (!form.departmentId) {
      setDesignations([]);
      return;
    }
    api.get<{ data: DesignationDto[] }>("/designations", { params: { departmentId: form.departmentId } })
      .then((response) => setDesignations(response.data.data))
      .catch(() => setDesignations([]));
  }, [form.departmentId]);

  function update<K extends keyof typeof form>(key: K, value: (typeof form)[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function sendOtp(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsBusy(true);
    try {
      const response = await api.post<{ data: { sessionId: string } }>("/EmployeeRegistration/start", { mobileNumber: form.mobileNumber });
      const id = response.data.data.sessionId;
      await api.post("/EmployeeRegistration/send-otp", { sessionId: id });
      setSessionId(id);
      setSuccess("OTP sent to the employee phone number.");
    } catch (err) {
      setError(getErrorMessage(err, "Could not send OTP."));
    } finally {
      setIsBusy(false);
    }
  }

  async function verifyOtp(event: FormEvent) {
    event.preventDefault();
    if (!sessionId || form.otp.length !== 6) return;
    setError(null);
    setIsBusy(true);
    try {
      await api.post("/EmployeeRegistration/verify-otp", { sessionId, otp: form.otp });
      setSuccess("Phone verified. Continue with employee details.");
      setStep(2);
    } catch (err) {
      setError(getErrorMessage(err, "OTP verification failed."));
    } finally {
      setIsBusy(false);
    }
  }

  async function saveDetails(event: FormEvent) {
    event.preventDefault();
    if (!sessionId || !form.photoFile) {
      setError("Employee photo is required.");
      return;
    }
    if (!/^[2-9]\d{11}$/.test(form.aadhaarNumber)) {
      setError("Enter a valid 12-digit Aadhaar number.");
      return;
    }
    setError(null);
    setIsBusy(true);
    try {
      const details = new FormData();
      details.append("sessionId", sessionId);
      details.append("firstName", form.firstName);
      details.append("lastName", form.lastName);
      details.append("email", form.email);
      details.append("dateOfBirth", form.dateOfBirth);
      details.append("gender", form.gender);
      details.append("address", form.address);
      details.append("departmentId", form.departmentId);
      details.append("designationId", form.designationId);
      details.append("dateOfJoining", form.dateOfJoining);
      details.append("employmentType", form.employmentType);
      details.append("aadhaarNumber", form.aadhaarNumber);
      details.append("photo", form.photoFile);
      await api.post("/EmployeeRegistration/details", details);
      setStep(3);
      setSuccess("Details saved. Capture all eight fingerprints.");
    } catch (err) {
      setError(getErrorMessage(err, "Could not save employee details."));
    } finally {
      setIsBusy(false);
    }
  }

  async function complete() {
    if (!sessionId) return;
    setError(null);
    setIsBusy(true);
    try {
      const response = await api.post<{ data: { employeeId: string } }>("/EmployeeRegistration/complete", { sessionId });
      setSuccess(`Employee registration completed. Employee ID: ${response.data.data.employeeId}`);
      onClose();
    } catch (err) {
      setError(getErrorMessage(err, "Could not complete employee registration."));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <section className="space-y-5 rounded-lg border border-primary-200 bg-primary-50/30 p-4 sm:p-5">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-lg font-medium text-ink">Create Employee</h2>
          <p className="mt-1 text-sm text-slate-500">Step {step} of 4</p>
        </div>
        <button type="button" onClick={onClose} className="text-sm text-slate-500 hover:text-ink">Cancel</button>
      </div>
      <div className="flex items-start px-2 py-2 sm:px-6">
        {["Employee details", "Contact & identity", "Biometric verification", "Review & complete"].map((label, index) => {
          const itemStep = index + 1;
          const isComplete = step > itemStep;
          const isCurrent = step === itemStep;
          return <div key={label} className="flex min-w-0 flex-1 items-start">
            <div className="flex min-w-0 flex-1 flex-col items-center gap-2">
              <div className={`flex h-10 w-10 items-center justify-center rounded-full border-2 text-sm font-semibold ${isComplete ? "border-emerald-500 bg-emerald-500 text-white" : isCurrent ? "border-primary-600 bg-primary-600 text-white ring-4 ring-primary-100" : "border-slate-300 bg-white text-slate-400"}`}>
                {isComplete ? <Check size={20} strokeWidth={3} aria-label="Completed" /> : itemStep}
              </div>
              <span className={`max-w-[90px] text-center text-[10px] leading-tight sm:max-w-none sm:text-xs ${isCurrent ? "font-semibold text-primary-700" : isComplete ? "text-emerald-700" : "text-slate-500"}`}>{label}</span>
            </div>
            {index < 3 && <div className={`mt-5 h-0.5 flex-1 ${step > itemStep ? "bg-emerald-500" : "bg-slate-200"}`} />}
          </div>;
        })}
      </div>
      {error && <div className="rounded-md bg-danger/5 px-3 py-2 text-sm text-danger">{error}</div>}
      {success && <div className="rounded-md bg-success/5 px-3 py-2 text-sm text-success">{success}</div>}

      {step === 1 && (
        <form onSubmit={sessionId ? verifyOtp : sendOtp} className="space-y-4 rounded-md border border-border bg-white p-4">
          <label className="block text-sm font-medium text-ink">Employee phone number</label>
          <input required type="tel" pattern="\d{10,15}" value={form.mobileNumber} disabled={!!sessionId} onChange={(event) => update("mobileNumber", event.target.value.replace(/\D/g, "").slice(0, 15))} className="w-full rounded-md border border-border px-3 py-2 text-sm" />
          {sessionId && <><label className="block text-sm font-medium text-ink">6-digit OTP</label><input required minLength={6} maxLength={6} value={form.otp} onChange={(event) => update("otp", event.target.value.replace(/\D/g, "").slice(0, 6))} className="w-full rounded-md border border-border px-3 py-2 text-sm" /></>}
          <div className="flex flex-wrap justify-end gap-2"><button type="button" onClick={onClose} className="rounded-md border border-border px-4 py-2 text-sm text-slate-600">Cancel</button><button type="submit" disabled={isBusy} className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60">{isBusy ? "Working..." : sessionId ? "Next" : "Send OTP"}</button></div>
        </form>
      )}

      {step === 2 && (
        <form onSubmit={saveDetails} className="space-y-4 rounded-md border border-border bg-white p-4">
          <div className="grid gap-4 sm:grid-cols-2"><input required placeholder="First name" value={form.firstName} onChange={(e) => update("firstName", e.target.value)} className="rounded-md border border-border px-3 py-2 text-sm" /><input required placeholder="Last name" value={form.lastName} onChange={(e) => update("lastName", e.target.value)} className="rounded-md border border-border px-3 py-2 text-sm" /></div>
          <div className="grid gap-4 sm:grid-cols-2"><input required type="email" placeholder="Email" value={form.email} onChange={(e) => update("email", e.target.value)} className="rounded-md border border-border px-3 py-2 text-sm" /><input required placeholder="Phone verified" value={form.mobileNumber} disabled className="rounded-md border border-border bg-slate-50 px-3 py-2 text-sm" /></div>
          <div className="grid gap-4 sm:grid-cols-2"><input required type="date" max={TODAY} value={form.dateOfBirth} onChange={(e) => update("dateOfBirth", e.target.value)} className="rounded-md border border-border px-3 py-2 text-sm" /><select required value={form.gender} onChange={(e) => update("gender", e.target.value)} className="rounded-md border border-border bg-white px-3 py-2 text-sm"><option>Male</option><option>Female</option><option>Other</option></select></div>
          <textarea required placeholder="Address" rows={2} value={form.address} onChange={(e) => update("address", e.target.value)} className="w-full rounded-md border border-border px-3 py-2 text-sm" />
          <div className="grid gap-4 sm:grid-cols-2"><select required value={form.departmentId} onChange={(e) => update("departmentId", e.target.value)} className="rounded-md border border-border bg-white px-3 py-2 text-sm"><option value="">Department</option>{departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select><select required value={form.designationId} onChange={(e) => update("designationId", e.target.value)} className="rounded-md border border-border bg-white px-3 py-2 text-sm"><option value="">Designation</option>{designations.map((item) => <option key={item.id} value={item.id}>{item.title}</option>)}</select></div>
          <div className="grid gap-4 sm:grid-cols-2"><input required type="date" value={form.dateOfJoining} onChange={(e) => update("dateOfJoining", e.target.value)} className="rounded-md border border-border px-3 py-2 text-sm" /><select value={form.employmentType} onChange={(e) => update("employmentType", e.target.value)} className="rounded-md border border-border bg-white px-3 py-2 text-sm"><option>Permanent</option><option>Contract</option><option>Temporary</option></select></div>
          <input required inputMode="numeric" pattern="[2-9]\d{11}" maxLength={12} placeholder="12-digit Aadhaar number" value={form.aadhaarNumber} onChange={(e) => update("aadhaarNumber", e.target.value.replace(/\D/g, "").slice(0, 12))} className="w-full rounded-md border border-border px-3 py-2 text-sm" />
          <input required type="file" accept="image/jpeg,image/png,image/webp" onChange={(e) => update("photoFile", e.target.files?.[0] ?? null)} className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm" />
          <div className="flex justify-end gap-2"><button type="button" onClick={() => setStep(1)} disabled={isBusy} className="rounded-md border border-border px-4 py-2 text-sm text-slate-600">Back</button><button type="button" onClick={onClose} disabled={isBusy} className="rounded-md border border-border px-4 py-2 text-sm text-slate-600">Cancel</button><button type="submit" disabled={isBusy} className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60">{isBusy ? "Saving..." : "Next"}</button></div>
        </form>
      )}

      {step === 3 && sessionId && <><BiometricEnrollment sessionId={sessionId} stepLabel="Step 3" onError={setError} onComplete={() => setStep(4)} /><div className="flex justify-end gap-2"><button type="button" onClick={() => setStep(2)} disabled={isBusy} className="rounded-md border border-border px-4 py-2 text-sm text-slate-600">Back</button><button type="button" onClick={onClose} disabled={isBusy} className="rounded-md border border-border px-4 py-2 text-sm text-slate-600">Cancel</button></div></>}

      {step === 4 && <div className="space-y-4 rounded-md border border-border bg-white p-4"><div><h3 className="text-base font-medium text-ink">Review employee details</h3><p className="mt-1 text-sm text-slate-500">Confirm the information before completing registration.</p></div><div className="grid gap-3 text-sm sm:grid-cols-2"><div><span className="text-xs text-slate-400">Name</span><p className="text-ink">{form.firstName} {form.lastName}</p></div><div><span className="text-xs text-slate-400">Mobile</span><p className="text-ink">{form.mobileNumber}</p></div><div><span className="text-xs text-slate-400">Email</span><p className="text-ink">{form.email}</p></div><div><span className="text-xs text-slate-400">Department / designation</span><p className="text-ink">{departments.find((item) => item.id === form.departmentId)?.name ?? "-"} / {designations.find((item) => item.id === form.designationId)?.title ?? "-"}</p></div><div><span className="text-xs text-slate-400">Date of joining</span><p className="text-ink">{form.dateOfJoining}</p></div><div><span className="text-xs text-slate-400">Photo</span><p className="text-ink">{form.photoFile?.name ?? "-"}</p></div></div><div className="flex flex-wrap justify-end gap-2"><button type="button" onClick={() => setStep(3)} disabled={isBusy} className="rounded-md border border-border px-4 py-2 text-sm text-slate-600">Back</button><button type="button" onClick={onClose} disabled={isBusy} className="rounded-md border border-border px-4 py-2 text-sm text-slate-600">Cancel</button><button type="button" onClick={complete} disabled={isBusy} className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-60">{isBusy ? "Completing..." : "Complete registration"}</button></div></div>}
    </section>
  );
}
