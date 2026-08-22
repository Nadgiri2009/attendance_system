"use client";

import { FormEvent, useState } from "react";
import { api, getErrorMessage } from "@/lib/api";
import { captureFingerprint } from "@/components/BiometricEnrollment";

export default function PublicAttendance() {
  const [lastEight, setLastEight] = useState("");
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const captureUrl = process.env.NEXT_PUBLIC_BIOMETRIC_BRIDGE_URL;

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setMessage(null);
    if (!/^\d{8}$/.test(lastEight)) {
      setError("Enter exactly the last 8 digits of your Aadhaar number.");
      return;
    }
    if (!captureUrl) {
      setError("The fingerprint service is not configured.");
      return;
    }

    setIsBusy(true);
    try {
      const capture = await captureFingerprint(captureUrl, "attendance");
      await api.post("/attendance/mark", {
        aadhaarLastEight: lastEight,
        templateDataBase64: capture.templateDataBase64
      });
      setLastEight("");
      setMessage("Attendance recorded successfully.");
    } catch (err) {
      setError(getErrorMessage(err, "Attendance could not be recorded."));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <section className="w-full max-w-md rounded-lg border border-border bg-white p-6 shadow-card">
      <h2 className="font-display text-2xl text-ink">Mark Attendance</h2>
      <p className="mt-1 text-sm text-slate-500">Enter your last 8 Aadhaar digits, then scan one registered finger.</p>
      <form onSubmit={submit} className="mt-5 space-y-4">
        <input
          type="password"
          inputMode="numeric"
          autoComplete="off"
          required
          pattern="[0-9]{8}"
          maxLength={8}
          value={lastEight}
          onChange={(event) => setLastEight(event.target.value.replace(/\D/g, "").slice(0, 8))}
          placeholder="Last 8 Aadhaar digits"
          className="w-full rounded-md border border-border px-3 py-2 text-sm focus-ring"
        />
        {error && <p className="rounded-md bg-danger/5 px-3 py-2 text-sm text-danger">{error}</p>}
        {message && <p className="rounded-md bg-success/5 px-3 py-2 text-sm text-success">{message}</p>}
        <button type="submit" disabled={isBusy} className="w-full rounded-md bg-primary-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-60">
          {isBusy ? "Place finger on scanner..." : "Scan fingerprint and mark attendance"}
        </button>
      </form>
    </section>
  );
}