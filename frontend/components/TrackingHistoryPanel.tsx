"use client";

import { useEffect, useState } from "react";
import dynamic from "next/dynamic";
import { api, getErrorMessage } from "@/lib/api";
import { TrackingHistoryDto } from "@/lib/types";

const UnifiedTrackingMap = dynamic(() => import("@/components/UnifiedTrackingMap"), { ssr: false });

function formatDuration(seconds?: number | null): string {
  if (seconds == null) return "—";
  const hrs = Math.floor(seconds / 3600);
  const mins = Math.round((seconds % 3600) / 60);
  return hrs > 0 ? `${hrs}h ${mins}m` : `${mins}m`;
}

function formatDistance(meters?: number | null): string {
  if (meters == null) return "—";
  return meters >= 1000 ? `${(meters / 1000).toFixed(2)} km` : `${Math.round(meters)} m`;
}

// Used from AttendanceManagement's "View route" action — requirement 7:
// "travelled path (polyline), start and end markers, total distance,
// duration, and playback of the route."
export default function TrackingHistoryPanel({ attendanceId, onClose }: { attendanceId: string; onClose: () => void }) {
  const [history, setHistory] = useState<TrackingHistoryDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    setIsLoading(true);
    setError(null);
    api
      .get<{ data: TrackingHistoryDto }>(`/tracking/history/${attendanceId}`)
      .then((res) => setHistory(res.data.data))
      .catch((err) => {
        const status = err?.response?.status;
        setError(
          status === 404
            ? "No GPS tracking session was recorded for this attendance (it may have been added manually without a Check-In)."
            : getErrorMessage(err, "Could not load the tracking route.")
        );
      })
      .finally(() => setIsLoading(false));
  }, [attendanceId]);

  return (
    <div className="bg-surface border border-border rounded-lg p-5 space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-ink">GPS tracking route</h3>
        <button onClick={onClose} className="text-sm text-slate-500 hover:text-ink">
          Close
        </button>
      </div>

      {isLoading && <div className="text-sm text-slate-400 py-6 text-center">Loading route…</div>}
      {error && <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-sm text-danger">{error}</div>}

      {history && (
        <>
          <div className="grid grid-cols-4 gap-3 text-sm">
            <div>
              <div className="text-xs text-slate-400 uppercase">Status</div>
              <div className="text-ink mt-0.5">{history.session.status}</div>
            </div>
            <div>
              <div className="text-xs text-slate-400 uppercase">Distance</div>
              <div className="text-ink mt-0.5">{formatDistance(history.session.totalDistanceMeters)}</div>
            </div>
            <div>
              <div className="text-xs text-slate-400 uppercase">Duration</div>
              <div className="text-ink mt-0.5">{formatDuration(history.session.totalDurationSeconds)}</div>
            </div>
            <div>
              <div className="text-xs text-slate-400 uppercase">GPS points</div>
              <div className="text-ink mt-0.5">{history.session.totalPointsCaptured}</div>
            </div>
          </div>

          <div className="rounded-lg border border-border overflow-hidden h-96 bg-white">
            <UnifiedTrackingMap attendanceId={attendanceId} />
          </div>
        </>
      )}
    </div>
  );
}
