"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api, getErrorMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { DepartmentDto, DesignationDto } from "@/lib/types";
import BiometricEnrollment from "@/components/BiometricEnrollment";

const TODAY = new Date().toISOString().split("T")[0];

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
  identityInput: "",
};

export default function EmployeeRegisterPage() {
  const router = useRouter();
  const { user, isLoading: isAuthLoading } = useAuth();
  const [form, setForm] = useState(INITIAL_FORM);
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [designations, setDesignations] = useState<DesignationDto[]>([]);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [step, setStep] = useState<"mobile" | "otp" | "details" | "biometric" | "done">("mobile");
  const [isSendingOtp, setIsSendingOtp] = useState(false);
  const [isVerifyingOtp, setIsVerifyingOtp] = useState(false);
  const [isCompleting, setIsCompleting] = useState(false);
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createdEmployeeId, setCreatedEmployeeId] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthLoading && !user?.roles.includes("Admin")) router.replace("/login");
  }, [isAuthLoading, router, user]);

  useEffect(() => {
    api
      .get<{ data: DepartmentDto[] }>("/departments")
      .then((res) => setDepartments(res.data.data))
      .catch(() => setDepartments([]));
  }, []);

  useEffect(() => {
    if (!form.departmentId) {
      setDesignations([]);
      setForm((prev) => ({ ...prev, designationId: "" }));
      return;
    }

    api
      .get<{ data: DesignationDto[] }>("/designations", {
        params: { departmentId: form.departmentId },
      })
      .then((res) => {
        setDesignations(res.data.data);
        setForm((prev) => {
          if (!prev.designationId || !res.data.data.some((d) => d.id === prev.designationId)) {
            return { ...prev, designationId: "" };
          }
          return prev;
        });
      })
      .catch(() => {
        setDesignations([]);
        setForm((prev) => ({ ...prev, designationId: "" }));
      });
  }, [form.departmentId]);

  function update<K extends keyof typeof INITIAL_FORM>(key: K, value: (typeof INITIAL_FORM)[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function sendOtp(e?: FormEvent) {
    e?.preventDefault();
    setError(null);
    setSuccess(null);
    setIsSendingOtp(true);

    try {
      const startResponse = await api.post<{ data: { sessionId: string } }>("/EmployeeRegistration/start", {
        mobileNumber: form.mobileNumber,
      });

      const newSessionId = startResponse.data.data.sessionId;
      setSessionId(newSessionId);

      const statusResponse = await api.get<{ data: { status: string } }>(`/EmployeeRegistration/status/${newSessionId}`);
      const currentStatus = statusResponse.data.data.status;

      if (currentStatus === "OtpVerified") {
        setStep("details");
        setSuccess("Mobile number is already verified. Continue with employee details.");
      } else if (["AwaitingIdentityVerification", "IdentityVerified", "BiometricEnrollmentStarted", "BiometricEnrollmentCompleted", "BiometricVerified"].includes(currentStatus)) {
        setStep("biometric");
        setSuccess("Resuming your registration. Continue biometric enrollment below.");
      } else {
        await api.post("/EmployeeRegistration/send-otp", { sessionId: newSessionId });
        setStep("otp");
        setSuccess("OTP sent successfully. Please enter the code below.");
      }
    } catch (err) {
      setError(getErrorMessage(err, "Could not start registration. Please try again."));
    } finally {
      setIsSendingOtp(false);
    }
  }

  async function verifyOtp(e?: FormEvent) {
    e?.preventDefault();
    if (!sessionId || form.otp.length !== 6) return;

    setError(null);
    setSuccess(null);
    setIsVerifyingOtp(true);

    try {
      await api.post("/EmployeeRegistration/verify-otp", {
        sessionId,
        otp: form.otp,
      });
      setStep("details");
      setSuccess("Mobile number verified. Please enter the employee details below.");
    } catch (err) {
      setError(getErrorMessage(err, "OTP verification failed. Please check the code and try again."));
    } finally {
      setIsVerifyingOtp(false);
    }
  }

  async function submitDetails(e?: FormEvent) {
    e?.preventDefault();
    if (!sessionId) return;

    setError(null);
    setSuccess(null);
    setIsCompleting(true);

    try {
      if (!photoFile) throw new Error("Employee photo is required.");
      if (photoFile.size > 5 * 1024 * 1024) throw new Error("Employee photo must be 5 MB or smaller.");
      if (!["image/jpeg", "image/png", "image/webp"].includes(photoFile.type)) {
        throw new Error("Employee photo must be a JPEG, PNG, or WebP image.");
      }

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
      details.append("aadhaarNumber", form.identityInput);
      details.append("photo", photoFile);
      await api.post("/EmployeeRegistration/details", details);

      setStep("biometric");
      setSuccess("Details saved. Complete eight-finger enrollment to finish registration.");
    } catch (err) {
      setError(getErrorMessage(err, "Registration details could not be saved. Please review the details and try again."));
    } finally {
      setIsCompleting(false);
    }
  }

  async function completeRegistration() {
    if (!sessionId) return;

    setError(null);
    setSuccess(null);
    setIsCompleting(true);
    try {
      const result = await api.post<{ data: { employeeId: string } }>("/EmployeeRegistration/complete", { sessionId });
      setCreatedEmployeeId(result.data.data.employeeId ?? null);
      setStep("done");
      setSuccess("Registration completed successfully.");
    } catch (err) {
      setError(getErrorMessage(err, "Registration could not be completed. Please review the details and try again."));
    } finally {
      setIsCompleting(false);
    }
  }

  if (isAuthLoading || !user?.roles.includes("Admin")) return null;

  return (
    <div className="min-h-screen bg-surface px-4 py-10">
      <div className="mx-auto max-w-3xl rounded-2xl border border-border bg-white p-6 shadow-card sm:p-8">
        <div className="mb-6 flex items-center justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.25em] text-primary-600">EWMS</p>
            <h1 className="mt-2 font-display text-3xl text-ink">Create Employee</h1>
          </div>
          <button
            type="button"
            onClick={() => router.push("/login")}
            className="rounded-md border border-border px-3 py-2 text-sm text-slate-700 hover:bg-surface"
          >
            Back to Admin Panel
          </button>
        </div>

        {success && (
          <div className="mb-5 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
            {success}
          </div>
        )}

        {error && (
          <div className="mb-5 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {error}
          </div>
        )}

        {step === "done" ? (
          <div className="space-y-4 rounded-lg border border-emerald-200 bg-emerald-50 p-6 text-center">
            <h2 className="font-display text-2xl text-emerald-800">Registration complete</h2>
            <p className="text-sm text-emerald-700">
              Your employee account has been created successfully.
            </p>
            {createdEmployeeId && (
              <p className="font-mono text-sm text-emerald-800">Employee ID: {createdEmployeeId}</p>
            )}
            <button
              type="button"
              onClick={() => router.push("/login")}
              className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700"
            >
              Go to login
            </button>
          </div>
        ) : step === "biometric" && sessionId ? (
            <BiometricEnrollment
              sessionId={sessionId}
              onComplete={completeRegistration}
              onError={(message) => {
                setError(message);
                setSuccess(null);
              }}
            />
          ) : (
          <form className="space-y-6" onSubmit={step === "mobile" ? sendOtp : step === "otp" ? verifyOtp : submitDetails}>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <label className="mb-1.5 block text-sm font-medium text-ink">Mobile number</label>
                <input
                  type="tel"
                  required
                  value={form.mobileNumber}
                  onChange={(e) => update("mobileNumber", e.target.value.replace(/\D/g, "").slice(0, 15))}
                  className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                  placeholder="9876543210"
                  disabled={step !== "mobile"}
                />
              </div>

              {step === "otp" && (
                <div className="sm:col-span-2">
                  <label className="mb-1.5 block text-sm font-medium text-ink">OTP</label>
                  <input
                    type="text"
                    required
                    value={form.otp}
                    onChange={(e) => update("otp", e.target.value.replace(/\D/g, "").slice(0, 6))}
                    className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    placeholder="Enter 6-digit OTP"
                  />
                </div>
              )}
            </div>

            {step === "details" && (
              <>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-ink">First name</label>
                    <input
                      type="text"
                      required
                      value={form.firstName}
                      onChange={(e) => update("firstName", e.target.value)}
                      className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-ink">Last name</label>
                    <input
                      type="text"
                      required
                      value={form.lastName}
                      onChange={(e) => update("lastName", e.target.value)}
                      className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    />
                  </div>
                </div>

                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-ink">Email</label>
                    <input
                      type="email"
                      required
                      value={form.email}
                      onChange={(e) => update("email", e.target.value)}
                      className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-ink">Gender</label>
                    <select
                      value={form.gender}
                      onChange={(e) => update("gender", e.target.value as typeof form.gender)}
                      className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    >
                      <option value="Male">Male</option>
                      <option value="Female">Female</option>
                      <option value="Other">Other</option>
                    </select>
                  </div>
                </div>

                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-ink">Date of birth</label>
                    <input
                      type="date"
                      required
                      max={TODAY}
                      value={form.dateOfBirth}
                      onChange={(e) => update("dateOfBirth", e.target.value)}
                      className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-ink">Employment type</label>
                    <select
                      value={form.employmentType}
                      onChange={(e) => update("employmentType", e.target.value)}
                      className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    >
                      <option value="Permanent">Permanent</option>
                      <option value="Contract">Contract</option>
                      <option value="Temporary">Temporary</option>
                    </select>
                  </div>
                </div>

                <div>
                  <label className="mb-1.5 block text-sm font-medium text-ink">Address</label>
                  <textarea
                    rows={3}
                    required
                    value={form.address}
                    onChange={(e) => update("address", e.target.value)}
                    className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                  />
                </div>

                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-ink">Department</label>
                    <select
                      value={form.departmentId}
                      required
                      onChange={(e) => update("departmentId", e.target.value)}
                      className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    >
                      <option value="">Select department</option>
                      {departments.map((department) => (
                        <option key={department.id} value={department.id}>
                          {department.name}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-ink">Designation</label>
                    <select
                      value={form.designationId}
                      required
                      disabled={!form.departmentId}
                      onChange={(e) => update("designationId", e.target.value)}
                      className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400 disabled:bg-surface"
                    >
                      <option value="">Select designation</option>
                      {designations.map((designation) => (
                        <option key={designation.id} value={designation.id}>
                          {designation.title}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div>
                  <label className="mb-1.5 block text-sm font-medium text-ink">Date of joining</label>
                  <input
                    type="date"
                    required
                    value={form.dateOfJoining}
                    onChange={(e) => update("dateOfJoining", e.target.value)}
                    className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                  />
                </div>

                <div>
                  <label className="mb-1.5 block text-sm font-medium text-ink">Aadhaar number</label>
                  <input
                    type="text"
                    inputMode="numeric"
                    maxLength={12}
                    value={form.identityInput}
                    onChange={(e) => update("identityInput", e.target.value.replace(/\D/g, "").slice(0, 12))}
                    className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    placeholder="12-digit Aadhaar number"
                  />
                </div>

                <div>
                  <label className="mb-1.5 block text-sm font-medium text-ink">Employee photo</label>
                  <input
                    type="file"
                    required
                    accept="image/jpeg,image/png,image/webp"
                    onChange={(e) => {
                      setPhotoFile(e.target.files?.[0] ?? null);
                      setError(null);
                    }}
                    className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                  />
                  <p className="mt-1 text-xs text-slate-500">Required. JPEG, PNG, or WebP, maximum 5 MB.</p>
                </div>
              </>
            )}

            <div className="flex flex-col gap-3 sm:flex-row sm:justify-end">
              {step === "mobile" ? (
                <button
                  type="submit"
                  disabled={isSendingOtp || !form.mobileNumber}
                  className="rounded-md bg-primary-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-60"
                >
                  {isSendingOtp ? "Sending OTP…" : "Send OTP"}
                </button>
              ) : step === "otp" ? (
                <button
                  type="submit"
                  disabled={isVerifyingOtp || form.otp.length !== 6}
                  className="rounded-md bg-primary-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-60"
                >
                  {isVerifyingOtp ? "Verifying OTP…" : "Verify OTP"}
                </button>
              ) : step === "details" ? (
                <button
                  type="submit"
                  disabled={isCompleting}
                  className="rounded-md bg-primary-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-60"
                >
                  {isCompleting ? "Completing registration…" : "Complete registration"}
                </button>
              ) : null}
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
