"use client";

import dynamic from "next/dynamic";
import { useAuth } from "@/lib/auth-context";

const LiveTrackingMap = dynamic(() => import("@/components/LiveTrackingMap"), { ssr: false });

const TRACKING_ROLES = ["Admin", "HR", "Manager"];

export default function TrackingPage() {
  const { user } = useAuth();
  const canView = !!user && TRACKING_ROLES.some((r) => user.roles.includes(r));

  if (!canView) {
    return (
      <div className="text-sm text-slate-500">
        You don't have permission to view live tracking. Contact an administrator if you believe this is a mistake.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-display text-2xl text-ink">Live tracking</h1>
        <p className="text-sm text-slate-500 mt-1">
          Real-time positions of every employee currently checked in with an active GPS tracking session.
        </p>
      </div>
      <LiveTrackingMap />
    </div>
  );
}
