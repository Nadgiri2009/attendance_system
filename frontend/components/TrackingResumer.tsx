"use client";

import { useEffect } from "react";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { locationTracker } from "@/lib/locationTracker";
import { AttendanceDto } from "@/lib/types";

// Rendered once inside the dashboard shell (not per-page) so a reload or a
// route change never silently drops an in-progress tracking session —
// "Continue tracking even if the employee navigates to other pages".
export default function TrackingResumer() {
  const { user } = useAuth();

  useEffect(() => {
    if (!user?.employeeId) return;

    api
      .get<{ data: AttendanceDto | null }>(`/attendance/today/${user.employeeId}`)
      .then((res) => {
        const today = res.data.data;
        if (today?.checkInAtUtc && !today.checkOutAtUtc) {
          void locationTracker.resumeIfNeeded(user.employeeId!, today.id);
        }
      })
      .catch((err) => {
        // Non-fatal: worst case, tracking simply doesn't resume until the
        // Attendance page is visited and the user can see/retry manually.
        console.warn("[TrackingResumer] Could not check for active tracking session:", err);
      });
  }, [user?.employeeId]);

  return null;
}
