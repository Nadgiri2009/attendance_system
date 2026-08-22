"use client";

import { useEffect, useState } from "react";
import { api, getErrorMessage } from "@/lib/api";

const FINGER_LABELS = [
  "Right thumb",
  "Right index",
  "Right middle",
  "Right ring",
  "Left thumb",
  "Left index",
  "Left middle",
  "Left ring"
];

type BiometricStatus = {
  status: string;
  requiredFingers: number;
  enrolledFingers: number;
  enrolledFingerNumbers: number[];
  progressPercentage: number;
};

type CaptureResponse = { templateDataBase64?: string };

function createPidOptions() {
  return '<?xml version="1.0" encoding="UTF-8"?><PidOptions ver="1.0"><Opts fCount="1" fType="2" iCount="0" iType="0" pCount="0" pType="0" format="0" pidVer="2.0" timeout="30000" otp="" wadh="" posh="UNKNOWN" env="P" /></PidOptions>';
}

export async function captureFingerprint(captureUrl: string, purpose?: string): Promise<CaptureResponse> {
  const response = await fetch(captureUrl.replace(/\/$/, ""), {
    method: "CAPTURE",
    headers: { "Content-Type": "text/xml; charset=UTF-8" },
    body: createPidOptions()
  });
  const xml = await response.text();
  if (!response.ok) throw new Error(`Fingerprint service returned HTTP ${response.status}.`);

  const document = new DOMParser().parseFromString(xml, "text/xml");
  const parserError = document.querySelector("parsererror");
  if (parserError) throw new Error("Fingerprint service returned invalid XML.");

  const responseNode = document.querySelector("Resp");
  const errorCode = responseNode?.getAttribute("errCode");
  if (errorCode && errorCode !== "0") {
    const errorInfo = responseNode?.getAttribute("errInfo") || "Fingerprint capture was rejected by the device.";
    throw new Error(`${errorInfo} (code ${errorCode}).`);
  }

  const dataNode = document.querySelector("Data");
  const templateDataBase64 = dataNode?.textContent?.trim();
  if (!templateDataBase64) throw new Error(purpose ? "The device returned no verification template." : "The device returned no fingerprint template.");
  return { templateDataBase64 };
}

export default function BiometricEnrollment({
  sessionId,
  onComplete,
  onError,
  stepLabel = "Step 4"
}: {
  sessionId: string;
  onComplete: () => void;
  onError: (message: string) => void;
  stepLabel?: string;
}) {
  const [status, setStatus] = useState<BiometricStatus | null>(null);
  const [activeFinger, setActiveFinger] = useState<number | null>(null);
  const [isStarting, setIsStarting] = useState(false);
  const [isCompleting, setIsCompleting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const captureUrl = process.env.NEXT_PUBLIC_BIOMETRIC_BRIDGE_URL;

  async function refreshStatus() {
    const response = await api.get<{ data: BiometricStatus }>("/EmployeeRegistration/biometric/status", {
      params: { sessionId }
    });
    setStatus(response.data.data);
  }

  useEffect(() => {
    refreshStatus().catch((error) => onError(getErrorMessage(error, "Could not load biometric enrollment status.")));
  }, [sessionId]);

  async function startEnrollment() {
    setMessage(null);
    setIsStarting(true);
    try {
      await api.post("/EmployeeRegistration/biometric/start", { sessionId, requiredFingers: 8 });
      await refreshStatus();
      setMessage("Enrollment started. Scan the fingers in the order shown below.");
    } catch (error) {
      onError(getErrorMessage(error, "Could not start biometric enrollment."));
    } finally {
      setIsStarting(false);
    }
  }

  async function captureFinger(fingerNumber: number) {
    if (!captureUrl) {
      onError("Configure NEXT_PUBLIC_BIOMETRIC_BRIDGE_URL for the Access FM220U L1 capture service.");
      return;
    }

    setMessage(null);
    setActiveFinger(fingerNumber);
    try {
      const capture = await captureFingerprint(captureUrl);

      await api.post("/EmployeeRegistration/biometric/finger", {
        sessionId,
        fingerNumber,
        templateDataBase64: capture.templateDataBase64
      });
      await refreshStatus();
      setMessage(`${FINGER_LABELS[fingerNumber - 1]} enrolled successfully.`);
    } catch (error) {
      onError(getErrorMessage(error, "Fingerprint capture failed. Check the device and bridge service."));
    } finally {
      setActiveFinger(null);
    }
  }

  async function completeEnrollment() {
    setMessage(null);
    setIsCompleting(true);
    try {
      await api.post("/EmployeeRegistration/biometric/complete", {
        sessionId
      });
      onComplete();
    } catch (error) {
      onError(getErrorMessage(error, "Could not complete biometric enrollment."));
    } finally {
      setIsCompleting(false);
    }
  }

  const enrolled = new Set(status?.enrolledFingerNumbers ?? []);
  const ready = status?.enrolledFingers === 8;

  return (
    <section className="space-y-5 rounded-lg border border-border bg-surface p-5">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-primary-600">{stepLabel}</p>
        <h2 className="mt-1 font-display text-2xl text-ink">Enroll eight fingerprints</h2>
        <p className="mt-1 text-sm text-slate-600">
          Connect the Access FM220U L1 through the configured device bridge and scan each finger once.
        </p>
      </div>

      {!status || status.status === "NotStarted" ? (
        <button type="button" onClick={startEnrollment} disabled={isStarting} className="rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-60">
          {isStarting ? "Starting device…" : "Start fingerprint enrollment"}
        </button>
      ) : (
        <>
          <div className="h-2 overflow-hidden rounded-full bg-slate-200" aria-label={`${status.progressPercentage}% enrolled`}>
            <div className="h-full bg-primary-600 transition-all" style={{ width: `${status.progressPercentage}%` }} />
          </div>
          <p className="text-sm font-medium text-ink">{status.enrolledFingers} of 8 fingers enrolled</p>

          <div className="grid gap-2 sm:grid-cols-2">
            {FINGER_LABELS.map((label, index) => {
              const fingerNumber = index + 1;
              const isEnrolled = enrolled.has(fingerNumber);
              return (
                <button
                  key={label}
                  type="button"
                  disabled={isEnrolled || activeFinger !== null || status.status !== "InProgress"}
                  onClick={() => captureFinger(fingerNumber)}
                  className={`flex items-center justify-between rounded-md border px-3 py-3 text-left text-sm ${isEnrolled ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-border bg-white text-ink hover:border-primary-400 disabled:opacity-60"}`}
                >
                  <span>{fingerNumber}. {label}</span>
                  <span className="text-xs">{isEnrolled ? "Enrolled" : activeFinger === fingerNumber ? "Scanning…" : "Scan"}</span>
                </button>
              );
            })}
          </div>

          {ready && (
            <button type="button" onClick={completeEnrollment} disabled={isCompleting} className="w-full rounded-md bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-60">
              {isCompleting ? "Completing registration…" : "Complete registration"}
            </button>
          )}
        </>
      )}

      {message && <p className="text-sm text-emerald-700">{message}</p>}
      {!captureUrl && <p className="text-sm text-amber-700">The device bridge URL is not configured yet.</p>}
    </section>
  );
}
