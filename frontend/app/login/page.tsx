"use client";

import { useEffect, useState, FormEvent } from "react";
import { useAuth } from "@/lib/auth-context";
import { api, getErrorMessage } from "@/lib/api";
import { DepartmentDto, DesignationDto } from "@/lib/types";
import PublicAttendance from "@/components/PublicAttendance";

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
  identityInput: ""
};

export default function LoginPage() {
  const { login } = useAuth();
  const [mode, setMode] = useState<"login" | "register" | "attendance">("login");
  const [userNameOrEmail, setUserNameOrEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [form, setForm] = useState(INITIAL_FORM);
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [designations, setDesignations] = useState<DesignationDto[]>([]);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [regStep, setRegStep] = useState<"mobile" | "otp" | "details" | "done">("mobile");
  const [isSendingOtp, setIsSendingOtp] = useState(false);
  const [isVerifyingOtp, setIsVerifyingOtp] = useState(false);
  const [isCompleting, setIsCompleting] = useState(false);
  const [regSuccess, setRegSuccess] = useState<string | null>(null);
  const [createdEmployeeId, setCreatedEmployeeId] = useState<string | null>(null);
  const [photoFile, setPhotoFile] = useState<File | null>(null);

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
      .get<{ data: DesignationDto[] }>("/designations", { params: { departmentId: form.departmentId } })
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

  function updateForm<K extends keyof typeof INITIAL_FORM>(key: K, value: (typeof INITIAL_FORM)[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login(userNameOrEmail, password);
    } catch (err) {
      setError(getErrorMessage(err, "Sign in failed. Check your credentials and try again."));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleRegisterOtp(e?: FormEvent) {
    e?.preventDefault();
    setError(null);
    setRegSuccess(null);
    setIsSendingOtp(true);

    try {
      const startResponse = await api.post<{ data: { sessionId: string } }>("/EmployeeRegistration/start", {
        mobileNumber: form.mobileNumber
      });

      const newSessionId = startResponse.data.data.sessionId;
      setSessionId(newSessionId);

      await api.post("/EmployeeRegistration/send-otp", { sessionId: newSessionId });
      setRegStep("otp");
      setRegSuccess("OTP sent successfully. Please enter the code below.");
    } catch (err) {
      setError(getErrorMessage(err, "Could not start employee registration. Please try again."));
    } finally {
      setIsSendingOtp(false);
    }
  }

  async function handleRegisterVerifyOtp(e?: FormEvent) {
    e?.preventDefault();
    if (!sessionId || form.otp.length !== 6) return;

    setError(null);
    setRegSuccess(null);
    setIsVerifyingOtp(true);

    try {
      await api.post("/EmployeeRegistration/verify-otp", {
        sessionId,
        otp: form.otp
      });
      setRegStep("details");
      setRegSuccess("Mobile number verified. Please enter the employee details below.");
    } catch (err) {
      setError(getErrorMessage(err, "OTP verification failed. Please check the code and try again."));
    } finally {
      setIsVerifyingOtp(false);
    }
  }

  async function handleRegisterComplete(e?: FormEvent) {
    e?.preventDefault();
    if (!sessionId) return;

    setError(null);
    setRegSuccess(null);
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

      const result = await api.post<{ data: { employeeId: string } }>("/EmployeeRegistration/complete", { sessionId });
      setCreatedEmployeeId(result.data.data.employeeId ?? null);
      setRegStep("done");
      setRegSuccess("Registration completed successfully.");
    } catch (err) {
      setError(getErrorMessage(err, "Registration could not be completed. Please review your details and try again."));
    } finally {
      setIsCompleting(false);
    }
  }

  return (
    <div className="min-h-screen grid grid-cols-1 lg:grid-cols-5">
      <div className="hidden lg:flex lg:col-span-3 bg-primary-700 relative overflow-hidden flex-col justify-between p-16">
        <div className="absolute inset-0 opacity-[0.07]" style={{
          backgroundImage: "radial-gradient(circle at 1px 1px, white 1px, transparent 0)",
          backgroundSize: "28px 28px"
        }} />
        <div className="relative z-10">
          <span className="font-mono text-xs tracking-[0.3em] text-primary-100 uppercase">EWMS</span>
        </div>
        <div className="relative z-10 max-w-md">
          <h1 className="font-display text-4xl leading-tight text-white mb-4">
            Every shift, every site,<br />accounted for.
          </h1>
          <p className="text-primary-100 text-base leading-relaxed">
            GPS-verified attendance, live field visibility, and a single record of your workforce — from headquarters to the last mile.
          </p>
        </div>
        <div className="relative z-10 font-mono text-xs text-primary-100/70">
          SMC - Attendance Mngt System
        </div>
      </div>

      <div className="lg:col-span-2 flex items-center justify-center p-6">
        <div className="w-full max-w-md">
          <div className="lg:hidden mb-8 font-mono text-xs tracking-[0.3em] text-primary-600 uppercase">EWMS</div>

          <div className="mb-6 grid grid-cols-2 gap-1 rounded-lg border border-border bg-surface p-1">
            <button
              type="button"
              onClick={() => setMode("login")}
              className={`w-full rounded-md px-3 py-2 text-sm font-medium ${mode === "login" ? "bg-white text-primary-700 shadow-sm" : "text-slate-500 hover:text-primary-700"}`}
            >
              Sign in
            </button>
            <button
              type="button"
              onClick={() => setMode("attendance")}
              className="flex w-full items-center justify-center rounded-md px-3 py-2 text-sm font-medium text-slate-500 transition-colors hover:text-primary-700"
            >
              Mark attendance
            </button>
          </div>

          {mode === "attendance" ? (
            <PublicAttendance />
          ) : mode === "login" ? (
            <>
              <h2 className="font-display text-2xl text-ink mb-1">Sign in</h2>
              <p className="text-sm text-slate-500 mb-8">Use your organization credentials.</p>

              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label htmlFor="userNameOrEmail" className="block text-sm font-medium text-ink mb-1.5">
                    Username or email
                  </label>
                  <input
                    id="userNameOrEmail"
                    type="text"
                    required
                    autoFocus
                    value={userNameOrEmail}
                    onChange={(e) => setUserNameOrEmail(e.target.value)}
                    className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    placeholder="admin"
                  />
                </div>
                <div>
                  <label htmlFor="password" className="block text-sm font-medium text-ink mb-1.5">
                    Password
                  </label>
                  <input
                    id="password"
                    type="password"
                    required
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                    placeholder="••••••••"
                  />
                </div>

                {error && (
                  <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-sm text-danger">
                    {error}
                  </div>
                )}

                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="w-full rounded-md bg-primary-600 px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:opacity-60 focus-ring"
                >
                  {isSubmitting ? "Signing in…" : "Sign in"}
                </button>
              </form>

              <p className="mt-8 text-xs text-slate-400">
                Use the administrator credentials configured for this environment.
              </p>
            </>
          ) : (
            <>
              <h2 className="font-display text-2xl text-ink mb-1">Create employee account</h2>
              <p className="text-sm text-slate-500 mb-8">Register your employee account for attendance tracking.</p>

              {regSuccess && (
                <div className="mb-4 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
                  {regSuccess}
                </div>
              )}

              {error && (
                <div className="mb-4 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                  {error}
                </div>
              )}

              {regStep === "done" ? (
                <div className="space-y-4 rounded-lg border border-emerald-200 bg-emerald-50 p-5 text-center">
                  <h3 className="font-display text-2xl text-emerald-800">Registration complete</h3>
                  <p className="text-sm text-emerald-700">Your account has been created successfully.</p>
                  {createdEmployeeId && (
                    <p className="font-mono text-sm text-emerald-900">Employee ID: {createdEmployeeId}</p>
                  )}
                  <button
                    type="button"
                    onClick={() => setMode("login")}
                    className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700"
                  >
                    Go to sign in
                  </button>
                </div>
              ) : (
                <form className="space-y-5" onSubmit={regStep === "mobile" ? handleRegisterOtp : regStep === "otp" ? handleRegisterVerifyOtp : handleRegisterComplete}>
                  <div className="space-y-4">
                    {regStep === "mobile" && (
                      <div>
                        <label className="mb-1.5 block text-sm font-medium text-ink">Mobile number</label>
                        <input
                          type="tel"
                          required
                          value={form.mobileNumber}
                          onChange={(e) => updateForm("mobileNumber", e.target.value.replace(/\D/g, "").slice(0, 15))}
                          className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                          placeholder="9876543210"
                        />
                      </div>
                    )}

                    {regStep === "otp" && (
                      <div>
                        <label className="mb-1.5 block text-sm font-medium text-ink">OTP</label>
                        <input
                          type="text"
                          required
                          value={form.otp}
                          onChange={(e) => updateForm("otp", e.target.value.replace(/\D/g, "").slice(0, 6))}
                          className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                          placeholder="Enter 6-digit OTP"
                        />
                      </div>
                    )}

                    {regStep === "details" && (
                      <>
                        <div className="grid gap-4 sm:grid-cols-2">
                          <div>
                            <label className="mb-1.5 block text-sm font-medium text-ink">First name</label>
                            <input
                              type="text"
                              required
                              value={form.firstName}
                              onChange={(e) => updateForm("firstName", e.target.value)}
                              className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                            />
                          </div>
                          <div>
                            <label className="mb-1.5 block text-sm font-medium text-ink">Last name</label>
                            <input
                              type="text"
                              required
                              value={form.lastName}
                              onChange={(e) => updateForm("lastName", e.target.value)}
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
                              onChange={(e) => updateForm("email", e.target.value)}
                              className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                            />
                          </div>
                          <div>
                            <label className="mb-1.5 block text-sm font-medium text-ink">Gender</label>
                            <select
                              value={form.gender}
                              onChange={(e) => updateForm("gender", e.target.value as typeof form.gender)}
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
                              onChange={(e) => updateForm("dateOfBirth", e.target.value)}
                              className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                            />
                          </div>
                          <div>
                            <label className="mb-1.5 block text-sm font-medium text-ink">Employment type</label>
                            <select
                              value={form.employmentType}
                              onChange={(e) => updateForm("employmentType", e.target.value)}
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
                            onChange={(e) => updateForm("address", e.target.value)}
                            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                          />
                        </div>

                        <div className="grid gap-4 sm:grid-cols-2">
                          <div>
                            <label className="mb-1.5 block text-sm font-medium text-ink">Department</label>
                            <select
                              value={form.departmentId}
                              required
                              onChange={(e) => updateForm("departmentId", e.target.value)}
                              className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                            >
                              <option value="">Select department</option>
                              {departments.map((department) => (
                                <option key={department.id} value={department.id}>{department.name}</option>
                              ))}
                            </select>
                          </div>
                          <div>
                            <label className="mb-1.5 block text-sm font-medium text-ink">Designation</label>
                            <select
                              value={form.designationId}
                              required
                              disabled={!form.departmentId}
                              onChange={(e) => updateForm("designationId", e.target.value)}
                              className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400 disabled:bg-surface"
                            >
                              <option value="">Select designation</option>
                              {designations.map((designation) => (
                                <option key={designation.id} value={designation.id}>{designation.title}</option>
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
                            onChange={(e) => updateForm("dateOfJoining", e.target.value)}
                            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink focus-ring focus:border-primary-400"
                          />
                        </div>

                        <div>
                          <label className="mb-1.5 block text-sm font-medium text-ink">Aadhaar number</label>
                          <input
                            type="text"
                            required
                            inputMode="numeric"
                            maxLength={12}
                            value={form.identityInput}
                            onChange={(e) => updateForm("identityInput", e.target.value.replace(/\D/g, "").slice(0, 12))}
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
                  </div>

                  <button
                    type="submit"
                    disabled={isSendingOtp || isVerifyingOtp || isCompleting || (regStep === "mobile" ? !form.mobileNumber : regStep === "otp" ? form.otp.length !== 6 : false)}
                    className="w-full rounded-md bg-primary-600 px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:opacity-60 focus-ring"
                  >
                    {regStep === "mobile"
                      ? (isSendingOtp ? "Sending OTP…" : "Send OTP")
                      : regStep === "otp"
                        ? (isVerifyingOtp ? "Verifying OTP…" : "Verify OTP")
                        : (isCompleting ? "Completing registration…" : "Complete registration")}
                  </button>
                </form>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
