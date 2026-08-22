"use client";

import { useEffect, useState } from "react";
import { locationTracker, TrackingStatus } from "@/lib/locationTracker";

export default function TrackingStatusCard() {
  const [status, setStatus] = useState<TrackingStatus>(locationTracker.getStatus());

  useEffect(() => locationTracker.subscribe(setStatus), []);

  const isOfflineOrQueued = status.pendingQueueSize > 0;
  
  return (
    <div className="bg-white border border-border rounded-lg p-4 shadow-card space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-sm text-slate-500">GPS tracking</span>
        <span
          className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${
            status.isActive
              ? isOfflineOrQueued
                ? "bg-warn/10 text-warn"
                : "bg-success/10 text-success"
              : "bg-slate-100 text-slate-500"
          }`}
        >
          <span
            className={`h-1.5 w-1.5 rounded-full ${
              status.isActive
                ? isOfflineOrQueued
                  ? "bg-warn animate-pulse"
                  : "bg-success animate-pulse"
                : "bg-slate-400"
            }`}
          />
          {status.isActive
            ? isOfflineOrQueued
              ? "Tracking (Offline)"
              : "Tracking Active"
            : "Tracking Inactive"}
        </span>
      </div>

      {status.isActive && (
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <div className="text-xs text-slate-400 uppercase">Current position</div>
            <div className="text-ink mt-0.5 font-mono text-xs">
              {status.lastLatitude != null && status.lastLongitude != null
                ? `${status.lastLatitude.toFixed(5)}, ${status.lastLongitude.toFixed(5)}`
                : "—"}
            </div>
          </div>
          <div>
            <div className="text-xs text-slate-400 uppercase">Last sync</div>
            <div className="text-ink mt-0.5">
              {status.lastSyncAt ? new Date(status.lastSyncAt).toLocaleTimeString() : "—"}
            </div>
          </div>
          <div>
            <div className="text-xs text-slate-400 uppercase">Points captured</div>
            <div className="text-ink mt-0.5">{status.pointsCaptured}</div>
          </div>
          <div>
            <div className="text-xs text-slate-400 uppercase">Accuracy</div>
            <div className="text-ink mt-0.5">
              {status.lastAccuracyMeters != null ? `±${Math.round(status.lastAccuracyMeters)}m` : "—"}
            </div>
          </div>
        </div>
      )}

      {status.pendingQueueSize > 0 && (
        <div className="bg-warn/5 border border-warn/20 rounded px-2.5 py-2 text-xs text-warn">
          <p className="font-medium mb-1">📡 Offline mode active</p>
          <p>
            {status.pendingQueueSize} location point{status.pendingQueueSize === 1 ? "" : "s"} queued. Will sync automatically when connection is restored.
          </p>
        </div>
      )}

      {status.lastError && !status.pendingQueueSize && (
        <div className="bg-danger/5 border border-danger/20 rounded px-2.5 py-2 text-xs text-danger">
          {status.lastError}
        </div>
      )}

      {status.permissionDenied && (
        <div className="bg-danger/5 border border-danger/20 rounded px-2.5 py-2 text-xs text-danger">
          <p className="font-medium">Location Permission Denied</p>
          <p>Enable location access in your browser settings to continue tracking.</p>
        </div>
      )}
    </div>
  );
}
