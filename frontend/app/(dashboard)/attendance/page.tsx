"use client";

import dynamic from "next/dynamic";
import { useEffect, useState } from "react";
import { api, getErrorMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { locationTracker } from "@/lib/locationTracker";
import { AttendanceDto, PaginatedList } from "@/lib/types";
import AttendanceManagement from "@/components/AttendanceManagement";
import TrackingStatusCard from "@/components/TrackingStatusCard";

const UnifiedTrackingMap = dynamic(() => import("@/components/UnifiedTrackingMap"), { ssr: false });

const MANAGEMENT_ROLES = ["Admin", "HR", "Manager"];

export default function AttendancePage() {
  const { user } = useAuth();
  const [today, setToday] = useState<AttendanceDto | null>(null);
  const [history, setHistory] = useState<AttendanceDto[]>([]);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [trackingMode, setTrackingMode] = useState<"personal" | "all">("personal");

  async function loadToday(): Promise<AttendanceDto | null> {
    if (!user?.employeeId) return null;

    try {
      const res = await api.get(`/attendance/today/${user.employeeId}`);
      const nextToday = res.data.data as AttendanceDto | null;
      setToday(nextToday);
      return nextToday;
    } catch (err) {
      const message = getErrorMessage(err, "Could not load today's attendance.");
      setStatusMessage(message);
      setToday(null);
      return null;
    }
  }

  async function loadHistory() {
    if (!user?.employeeId) return;
    const res = await api.get<{ data: PaginatedList<AttendanceDto> }>('/attendance/history', { params: { employeeId: user.employeeId, pageSize: 15 } });
    setHistory(res.data.data.items);
  }

  useEffect(() => {
    if (!user?.employeeId) return;

    void (async () => {
      await loadToday();
      await loadHistory().catch(() => {});
    })();
  }, [user?.employeeId]);

  async function requestLocationAccess(): Promise<GeolocationPosition> {
    if (!("geolocation" in navigator)) {
      throw new Error("This browser does not support location access.");
    }

    return await new Promise<GeolocationPosition>((resolve, reject) => {
      navigator.geolocation.getCurrentPosition(resolve, reject, {
        enableHighAccuracy: true,
        timeout: 20000,
        maximumAge: 0
      });
    });
  }

  async function handleCheckInOut(type: "check-in" | "check-out") {
    if (!user?.employeeId) {
      setStatusMessage("This account is not linked to an employee record. Please sign in with an employee account or contact an administrator.");
      return;
    }

    setIsBusy(true);
    setStatusMessage(null);
    try {
      const position = await requestLocationAccess();

      const latitude = position.coords.latitude;
      const longitude = position.coords.longitude;
      const accuracy = position.coords.accuracy;

      const response = await api.post(`/attendance/${type}`, {
        employeeId: user.employeeId,
        latitude,
        longitude,
        accuracyMeters: accuracy,
        isMockLocation: false,
        address: null
      });

      const attendanceId = response.data?.data?.attendanceId ?? response.data?.data?.id ?? today?.id;

      if (type === "check-in") {
        if (!attendanceId) {
          throw new Error("Attendance record ID was not returned after check-in.");
        }

        await locationTracker.start(user.employeeId, attendanceId);
        setStatusMessage("Checked in successfully. GPS tracking started.");
      } else {
        await locationTracker.stop();
        setStatusMessage("Checked out successfully. GPS tracking stopped.");
      }

      await Promise.all([loadToday(), loadHistory()]);
    } catch (err) {
      const message = getErrorMessage(
        err,
        "Location access is required to check in. Please allow browser location access and try again."
      );
      setStatusMessage(message);
    } finally {
      setIsBusy(false);
    }
  }

  async function handleStartTracking() {
    if (!user?.employeeId || !today?.id) {
      setStatusMessage("Cannot start tracking without check-in.");
      return;
    }

    try {
      setStatusMessage(null);
      await requestLocationAccess();
      await locationTracker.start(user.employeeId, today.id);
      setStatusMessage("GPS tracking started.");
    } catch (err) {
      setStatusMessage(getErrorMessage(err, "Could not start GPS tracking. Please allow browser location access and try again."));
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="font-display text-2xl text-ink">Attendance</h1>
        <p className="text-sm text-slate-500 mt-1">Check-in and check-out records.</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white border border-border rounded-lg p-6 shadow-card space-y-4">
          <div className="flex items-center justify-between">
            <span className="text-sm text-slate-500">Today's status</span>
            <span className="text-xs uppercase tracking-wide font-medium text-primary-600">
              {today?.checkInAtUtc ? (today?.checkOutAtUtc ? "Complete" : "In progress") : "Not started"}
            </span>
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <div className="text-xs text-slate-400 uppercase">Check-in</div>
              <div className="text-ink mt-0.5">{today?.checkInAtUtc ? new Date(today.checkInAtUtc).toLocaleTimeString() : "—"}</div>
            </div>
            <div>
              <div className="text-xs text-slate-400 uppercase">Check-out</div>
              <div className="text-ink mt-0.5">{today?.checkOutAtUtc ? new Date(today.checkOutAtUtc).toLocaleTimeString() : "—"}</div>
            </div>
          </div>

          <div className="flex gap-3 pt-2">
            <button
              onClick={() => handleCheckInOut("check-in")}
              disabled={isBusy || !!today?.checkInAtUtc}
              className="flex-1 rounded-md bg-primary-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-40"
            >
              Check in
            </button>
            <button
              onClick={() => handleCheckInOut("check-out")}
              disabled={isBusy || !today?.checkInAtUtc || !!today?.checkOutAtUtc}
              className="flex-1 rounded-md border border-primary-600 px-4 py-2.5 text-sm font-medium text-primary-600 hover:bg-primary-50 disabled:opacity-40"
            >
              Check out
            </button>
          </div>

          <button
            onClick={handleStartTracking}
            disabled={isBusy || !today?.checkInAtUtc || !!today?.checkOutAtUtc}
            className="w-full rounded-md border border-amber-600 px-4 py-2 text-sm font-medium text-amber-600 hover:bg-amber-50 disabled:opacity-40"
          >
            Start GPS Tracking
          </button>

          {statusMessage && <p className="text-xs text-slate-500">{statusMessage}</p>}
        </div>

        <TrackingStatusCard />
      </div>

      <div className="rounded-lg border border-border bg-white p-4 shadow-card space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-medium text-ink">Tracking Map</h2>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setTrackingMode("personal")}
              className={`text-xs px-3 py-1.5 rounded ${
                trackingMode === "personal"
                  ? "bg-primary-600 text-white"
                  : "bg-slate-100 text-slate-600 hover:bg-slate-200"
              }`}
            >
              My Tracking
            </button>
            {user && MANAGEMENT_ROLES.some((r) => user.roles.includes(r)) && (
              <button
                onClick={() => setTrackingMode("all")}
                className={`text-xs px-3 py-1.5 rounded ${
                  trackingMode === "all"
                    ? "bg-primary-600 text-white"
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                Track All Employees
              </button>
            )}
            <span className="text-xs uppercase tracking-wide text-primary-600 ml-2">Live</span>
          </div>
        </div>
        <div className="h-[22rem] overflow-hidden rounded-md border border-border bg-slate-50">
          <UnifiedTrackingMap mode={trackingMode} />
        </div>
      </div>

      <div>
        <h2 className="text-sm font-medium text-ink mb-3">Attendance history</h2>
        <div className="rounded-lg border border-border bg-white shadow-card divide-y divide-border">
          {history.length === 0 && <div className="px-4 py-8 text-center text-sm text-slate-400">No history yet.</div>}
          {history.map((h) => (
            <div key={h.id} className="px-4 py-3 flex items-center justify-between text-sm">
              <span className="text-ink">{h.attendanceDate}</span>
              <span className="text-slate-500">{h.totalHours ? `${h.totalHours} hrs` : "—"}</span>
              <span className="text-xs uppercase tracking-wide text-slate-500">{h.status}</span>
            </div>
          ))}
        </div>
      </div>

      {user && MANAGEMENT_ROLES.some((r) => user.roles.includes(r)) && (
        <div className="pt-4 border-t border-border">
          <AttendanceManagement />
        </div>
      )}
    </div>
  );
}
