"use client";

import { api, getApiBaseUrl, getErrorMessage } from "@/lib/api";

// Configurable per the spec's ranges: capture every 15-30s OR every
// 20-50m of movement, whichever comes first.
const LOCATION_INTERVAL_MS = 20_000;
const MIN_DISTANCE_METERS = 30;
const QUEUE_FLUSH_INTERVAL_MS = 15_000;
const STORAGE_KEY = "ewms_tracking_state";
const QUEUE_KEY = "ewms_tracking_queue";

export interface TrackingStatus {
  isActive: boolean;
  trackingSessionId: string | null;
  attendanceId: string | null;
  lastLatitude: number | null;
  lastLongitude: number | null;
  lastAccuracyMeters: number | null;
  lastSyncAt: string | null;
  pointsCaptured: number;
  pendingQueueSize: number;
  lastError: string | null;
  permissionDenied: boolean;
}

interface QueuedPoint {
  trackingSessionId: string;
  latitude: number;
  longitude: number;
  accuracyMeters: number | null;
  speedKmh: number | null;
  heading: number | null;
  batteryPercent: number | null;
  isMockLocation: boolean;
  recordedAtUtc: string;
}

interface PersistedState {
  employeeId: string;
  attendanceId: string;
  trackingSessionId: string;
}

function haversineMeters(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6_371_000;
  const toRad = (d: number) => (d * Math.PI) / 180;
  const dLat = toRad(lat2 - lat1);
  const dLon = toRad(lon2 - lon1);
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

// Battery Status API is deprecated/unavailable in most browsers (removed in
// Firefox, restricted in Chrome) — feature-detected, and every call site
// treats a missing reading as "unknown" rather than failing tracking.
async function readBatteryPercent(): Promise<number | null> {
  try {
    const nav = navigator as Navigator & { getBattery?: () => Promise<{ level: number }> };
    if (!nav.getBattery) return null;
    const battery = await nav.getBattery();
    return Math.round(battery.level * 100);
  } catch {
    return null;
  }
}

function getDeviceInfo(): string {
  return typeof navigator !== "undefined" ? navigator.userAgent.slice(0, 500) : "unknown";
}

class LocationTrackingService {
  private watchId: number | null = null;
  private flushTimer: ReturnType<typeof setInterval> | null = null;
  private lastPosted: { lat: number; lon: number; at: number } | null = null;
  private listeners = new Set<(status: TrackingStatus) => void>();

  private status: TrackingStatus = {
    isActive: false,
    trackingSessionId: null,
    attendanceId: null,
    lastLatitude: null,
    lastLongitude: null,
    lastAccuracyMeters: null,
    lastSyncAt: null,
    pointsCaptured: 0,
    pendingQueueSize: 0,
    lastError: null,
    permissionDenied: false
  };

  constructor() {
    if (typeof window === "undefined") return;
    this.status.pendingQueueSize = this.readQueue().length;
    window.addEventListener("online", () => this.flushQueue());
    // Best-effort final stop on tab close. sendBeacon can't carry our Bearer
    // auth header, so this uses fetch(..., { keepalive: true }) instead,
    // which DOES support custom headers and is allowed to outlive unload —
    // this is the correct modern replacement for sendBeacon when auth is
    // required. It's still best-effort: if the OS kills the process instead
    // of a clean tab close, this never fires (a known, documented limitation
    // — see docs/GPS_TRACKING.md).
    window.addEventListener("pagehide", () => this.bestEffortStopOnUnload());
  }

  subscribe(listener: (status: TrackingStatus) => void): () => void {
    this.listeners.add(listener);
    listener(this.status);
    return () => this.listeners.delete(listener);
  }

  getStatus(): TrackingStatus {
    return this.status;
  }

  private emit() {
    this.status.pendingQueueSize = this.readQueue().length;
    this.listeners.forEach((l) => l({ ...this.status }));
  }

  private readQueue(): QueuedPoint[] {
    try {
      const raw = window.localStorage.getItem(QUEUE_KEY);
      return raw ? (JSON.parse(raw) as QueuedPoint[]) : [];
    } catch {
      return [];
    }
  }

  private writeQueue(queue: QueuedPoint[]) {
    try {
      window.localStorage.setItem(QUEUE_KEY, JSON.stringify(queue));
    } catch {
      // Storage full/unavailable — points are lost, which is preferable to
      // crashing the tracker; surfaced via lastError on the next attempt.
    }
  }

  private persistState(state: PersistedState | null) {
    if (state) window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    else window.localStorage.removeItem(STORAGE_KEY);
  }

  private readPersistedState(): PersistedState | null {
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as PersistedState) : null;
    } catch {
      return null;
    }
  }

  // Called once when the dashboard shell mounts (and after a page reload)
  // to detect "we should still be tracking" and resume — satisfies
  // "Continue tracking even if the employee navigates to other pages".
  // Re-calling /tracking/start is safe: the backend treats a repeat call
  // for the same attendance record as idempotent, and self-heals (clears
  // local state) if the session or attendance was actually closed
  // server-side while this tab was gone.
  async resumeIfNeeded(employeeId: string, attendanceId: string): Promise<void> {
    if (this.status.isActive && this.status.attendanceId === attendanceId) return;

    const persisted = this.readPersistedState();
    if (persisted && (persisted.employeeId !== employeeId || persisted.attendanceId !== attendanceId)) {
      this.persistState(null);
    }

    await this.start(employeeId, attendanceId);
  }

  async start(employeeId: string, attendanceId: string): Promise<void> {
    if (this.status.isActive && this.status.attendanceId === attendanceId) {
      console.log("[LocationTracker] Already tracking this attendance, skipping start");
      return; // already tracking this session
    }

    if (!("geolocation" in navigator)) {
      this.status.lastError = "Geolocation is not supported on this device.";
      this.emit();
      console.error("[LocationTracker]", this.status.lastError);
      throw new Error(this.status.lastError);
    }

    console.log("[LocationTracker] Starting tracking for employee:", employeeId, "attendance:", attendanceId);

    const position = await this.getCurrentPosition().catch((err: GeolocationPositionError) => {
      this.status.permissionDenied = err.code === err.PERMISSION_DENIED;
      
      let errorMsg = "Could not get your current location.";
      if (err.code === 1) { // PERMISSION_DENIED
        errorMsg = "Location permission was denied. Enable it in your browser to start tracking.";
      } else if (err.code === 2) { // POSITION_UNAVAILABLE
        errorMsg = "GPS signal unavailable. Try in an open area.";
      } else if (err.code === 3) { // TIMEOUT
        errorMsg = "Location request timed out. Try again.";
      }
      
      this.status.lastError = errorMsg;
      this.emit();
      console.error(`[LocationTracker] Geolocation error code ${err.code}: ${errorMsg}`);
      return null;
    });
    if (!position) {
      this.emit();
      return;
    }

    const battery = await readBatteryPercent();

    // Retry logic for transient failures (e.g., database sync delays after check-in)
    let lastError: any;
    for (let attempt = 0; attempt < 3; attempt++) {
      try {
        const res = await api.post("/tracking/start", {
          employeeId,
          attendanceRecordId: attendanceId,
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracyMeters: position.coords.accuracy,
          batteryPercent: battery,
          deviceInfo: getDeviceInfo()
        });

        const trackingSessionId: string = res.data.data.trackingSessionId;

        this.status = {
          ...this.status,
          isActive: true,
          trackingSessionId,
          attendanceId,
          lastLatitude: position.coords.latitude,
          lastLongitude: position.coords.longitude,
          lastAccuracyMeters: position.coords.accuracy,
          lastSyncAt: new Date().toISOString(),
          lastError: null,
          permissionDenied: false
        };
        this.persistState({ employeeId, attendanceId, trackingSessionId });
        this.lastPosted = { lat: position.coords.latitude, lon: position.coords.longitude, at: Date.now() };

        this.beginWatch();
        this.beginQueueFlushLoop();
        this.emit();
        
        console.log("[LocationTracker] Tracking started successfully:", {
          trackingSessionId,
          startLat: position.coords.latitude,
          startLng: position.coords.longitude,
          accuracy: position.coords.accuracy
        });
        return; // Success - exit retry loop
      } catch (err) {
        lastError = err;
        const statusCode = (err as any)?.response?.status;
        // Don't retry on 4xx validation errors - these won't succeed on retry
        if (statusCode && statusCode >= 400 && statusCode < 500) {
          const validationErrors = this.extractValidationErrors((err as any)?.response?.data);
          this.status.lastError = validationErrors.length > 0 
            ? validationErrors.join("; ")
            : getErrorMessage(err, "Could not start GPS tracking.");
          this.emit();
          console.error("[LocationTracker] Validation failed:", {
            errorMsg: this.status.lastError,
            validationErrors,
            statusCode,
            responseData: (err as any)?.response?.data
          });
          throw err;
        }
        
        // Retry on network or server errors
        if (attempt < 2) {
          const delayMs = Math.min(500 * Math.pow(2, attempt), 2000);
          console.log(`[LocationTracker] Attempt ${attempt + 1}/3 failed (${statusCode || 'network error'}), retrying in ${delayMs}ms...`);
          await new Promise(resolve => setTimeout(resolve, delayMs));
        }
      }
    }

    // All retries exhausted
    this.status.lastError = getErrorMessage(lastError, "Could not start GPS tracking after multiple attempts.");
    this.emit();
    console.error("[LocationTracker] Failed to start tracking after retries:", {
      errorMsg: this.status.lastError,
      lastError,
      statusCode: (lastError as any)?.response?.status
    });
    throw lastError;
  }

  private extractValidationErrors(responseData: any): string[] {
    if (!responseData) return [];
    const errors: string[] = [];
    
    // Handle array format: { errors: ["message"] }
    if (Array.isArray(responseData.errors)) {
      errors.push(...responseData.errors.filter((e: any) => typeof e === 'string'));
    }
    // Handle object format: { errors: { field: ["message"] } }
    else if (responseData.errors && typeof responseData.errors === 'object') {
      for (const [field, msgs] of Object.entries(responseData.errors)) {
        if (Array.isArray(msgs)) {
          errors.push(`${field}: ${(msgs as any[]).join(', ')}`);
        } else if (typeof msgs === 'string') {
          errors.push(`${field}: ${msgs}`);
        }
      }
    }
    
    return errors;
  }

  async stop(): Promise<void> {
    const { trackingSessionId } = this.status;
    this.endWatch();
    this.endQueueFlushLoop();

    if (trackingSessionId) {
      try {
        await api.post("/tracking/stop", {
          trackingSessionId,
          endLatitude: this.status.lastLatitude,
          endLongitude: this.status.lastLongitude
        });
      } catch {
        // Check-Out already stops the session server-side as a safety net
        // (see CheckOutCommandHandler), so a failed explicit stop call here
        // isn't fatal — the session won't stay Active either way.
      }
      await this.flushQueue();
    }

    this.status = {
      isActive: false,
      trackingSessionId: null,
      attendanceId: null,
      lastLatitude: null,
      lastLongitude: null,
      lastAccuracyMeters: null,
      lastSyncAt: this.status.lastSyncAt,
      pointsCaptured: 0,
      pendingQueueSize: this.readQueue().length,
      lastError: null,
      permissionDenied: false
    };
    this.persistState(null);
    this.emit();
  }

  private getCurrentPosition(): Promise<GeolocationPosition> {
    return new Promise((resolve, reject) => {
      navigator.geolocation.getCurrentPosition(resolve, reject, {
        enableHighAccuracy: true,
        timeout: 15_000,
        maximumAge: 0
      });
    });
  }

  private beginWatch() {
    this.endWatch();
    this.watchId = navigator.geolocation.watchPosition(
      (position) => this.handlePosition(position),
      (err: GeolocationPositionError) => {
        let errorMsg = "Location request timed out.";
        if (err.code === 1) { // PERMISSION_DENIED
          errorMsg = "Location permission was denied.";
        } else if (err.code === 2) { // POSITION_UNAVAILABLE
          errorMsg = "GPS signal lost.";
        }
        this.status.lastError = errorMsg;
        this.emit();
        console.warn(`[LocationTracker] Watch error code ${err.code}: ${errorMsg}`);
      },
      { enableHighAccuracy: true, maximumAge: 5_000, timeout: 20_000 }
    );
  }

  private endWatch() {
    if (this.watchId !== null) {
      navigator.geolocation.clearWatch(this.watchId);
      this.watchId = null;
    }
  }

  private handlePosition(position: GeolocationPosition) {
    if (!this.status.isActive || !this.status.trackingSessionId) return;

    const { latitude, longitude, accuracy, speed, heading } = position.coords;
    const now = Date.now();

    this.status.lastLatitude = latitude;
    this.status.lastLongitude = longitude;
    this.status.lastAccuracyMeters = accuracy;
    this.status.permissionDenied = false;
    this.status.lastError = null;

    const elapsedMs = this.lastPosted ? now - this.lastPosted.at : Infinity;
    const distanceMoved = this.lastPosted
      ? haversineMeters(this.lastPosted.lat, this.lastPosted.lon, latitude, longitude)
      : Infinity;

    // Throttle: only upload when enough time OR enough distance has
    // elapsed (spec: every 15-30s or every 20-50m of movement).
    // Throttle: only upload when enough time OR enough distance has
// elapsed (spec: every 15-30s or every 20-50m of movement).
if (
  elapsedMs >= LOCATION_INTERVAL_MS ||
  distanceMoved >= MIN_DISTANCE_METERS
) {
  // Make sure we have a valid tracking session before
  // creating the location point.
  const trackingSessionId = this.status.trackingSessionId;

  if (!trackingSessionId) {
    this.status.lastError =
      "Tracking session ID is missing. Cannot upload location.";

    this.emit();
    return;
  }

  this.lastPosted = {
    lat: latitude,
    lon: longitude,
    at: now,
  };

  const point: QueuedPoint = {
    trackingSessionId,
    latitude,
    longitude,
    accuracyMeters: accuracy ?? null,
    speedKmh:
      speed != null
        ? Math.round(speed * 3.6 * 10) / 10
        : null,
    heading: heading ?? null,

    // Battery is populated inside captureAndSend().
    batteryPercent: null,

    // Browser Geolocation API does not provide
    // mock-location detection.
    isMockLocation: false,

    recordedAtUtc: new Date().toISOString(),
  };

  void this.captureAndSend(point);
} else {
  this.emit();
}
  }

  private async captureAndSend(point: QueuedPoint) {
    try {
      const battery = await readBatteryPercent();
      point.batteryPercent = battery;

      await api.post("/tracking/location", point);
      this.status.pointsCaptured += 1;
      this.status.lastSyncAt = new Date().toISOString();
      this.status.lastError = null; // Clear error on success
      this.emit();
      console.log("[LocationTracker] Location sent successfully:", { lat: point.latitude, lng: point.longitude });
    } catch (err) {
      const errorMsg = getErrorMessage(err, "Could not upload location. It will be retried automatically.");
      this.status.lastError = errorMsg;
      
      // Log detailed error for debugging
      console.error("[LocationTracker] Failed to send location:", {
        error: err,
        errorMsg,
        isNetworkError: err instanceof TypeError || (err as any)?.code === 'ECONNABORTED',
        axiosError: (err as any)?.isAxiosError,
        axiosCode: (err as any)?.code,
        response: (err as any)?.response?.status
      });
      
      // Offline or the request failed — queue locally and retry later
      // ("Queue locations locally if offline and sync when connection is
      // restored" / "Retry failed uploads").
      const queue = this.readQueue();
      queue.push(point);
      this.writeQueue(queue);
      this.emit();
    }
  }

  private beginQueueFlushLoop() {
    this.endQueueFlushLoop();
    this.flushTimer = setInterval(() => void this.flushQueue(), QUEUE_FLUSH_INTERVAL_MS);
  }

  private endQueueFlushLoop() {
    if (this.flushTimer !== null) {
      clearInterval(this.flushTimer);
      this.flushTimer = null;
    }
  }

  async flushQueue(): Promise<void> {
    const queue = this.readQueue();
    if (queue.length === 0) return;

    console.log("[LocationTracker] Flushing queue with", queue.length, "points");
    
    const remaining: QueuedPoint[] = [];
    for (const point of queue) {
      try {
        await api.post("/tracking/location", point);
        this.status.pointsCaptured += 1;
        console.log("[LocationTracker] Queued location synced:", { lat: point.latitude, lng: point.longitude });
      } catch (err) {
        const errorMsg = getErrorMessage(err, "Failed to sync queued locations. Will retry later.");
        this.status.lastError = errorMsg;
        
        console.error("[LocationTracker] Failed to flush queued point:", {
          error: err,
          errorMsg,
          isNetworkError: err instanceof TypeError || (err as any)?.code === 'ECONNABORTED',
          queuedPoints: queue.length,
          failedPoint: { lat: point.latitude, lng: point.longitude }
        });
        
        remaining.push(point); // still failing (still offline, or session ended) — keep for next attempt
      }
    }

    this.writeQueue(remaining);
    if (remaining.length < queue.length) this.status.lastSyncAt = new Date().toISOString();
    this.emit();
    
    if (remaining.length > 0) {
      console.warn("[LocationTracker] Could not sync all points. Remaining in queue:", remaining.length);
    }
  }

  private bestEffortStopOnUnload() {
    if (!this.status.trackingSessionId) return;
    const body = JSON.stringify({
      trackingSessionId: this.status.trackingSessionId,
      endLatitude: this.status.lastLatitude,
      endLongitude: this.status.lastLongitude
    });
    const token = window.localStorage.getItem("ewms_access_token");
    const base = getApiBaseUrl();
    void fetch(`${base}/tracking/stop`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body,
      keepalive: true
    }).catch(() => {});
  }
}

// Singleton: one tracker per browser tab, independent of which React
// component happens to be mounted — this is what lets tracking survive
// in-app navigation between pages.
export const locationTracker = new LocationTrackingService();
