"use client";

import { useEffect, useMemo, useState } from "react";
import { MapContainer, TileLayer, Marker, Polyline, Popup, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import L from "leaflet";
import { api, getErrorMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { LiveLocationDto, TrackingHistoryDto, LocationPointDto } from "@/lib/types";

const employeeIcon = L.divIcon({
  className: "",
  html: `<span style="display:flex;align-items:center;justify-content:center;width:22px;height:22px;border-radius:50%;background:#14385E;border:2px solid white;box-shadow:0 0 0 1px rgba(0,0,0,0.25);color:white;font-size:11px;">●</span>`,
  iconSize: [22, 22],
  iconAnchor: [11, 11]
});

const currentLocationIcon = L.divIcon({
  className: "",
  html: `<span style="display:flex;align-items:center;justify-content:center;width:18px;height:18px;border-radius:50%;background:#1d4ed8;border:2px solid white;box-shadow:0 0 0 2px rgba(29,78,216,0.15);color:white;font-size:9px;">●</span>`,
  iconSize: [18, 18],
  iconAnchor: [9, 9]
});

const startIcon = L.divIcon({
  className: "",
  html: `<span style="display:block;width:16px;height:16px;border-radius:50%;background:#1E8E5A;border:2px solid white;box-shadow:0 0 0 1px rgba(0,0,0,0.25)"></span>`,
  iconSize: [16, 16],
  iconAnchor: [8, 8]
});

const endIcon = L.divIcon({
  className: "",
  html: `<span style="display:block;width:16px;height:16px;border-radius:50%;background:#C13A3A;border:2px solid white;box-shadow:0 0 0 1px rgba(0,0,0,0.25)"></span>`,
  iconSize: [16, 16],
  iconAnchor: [8, 8]
});

const TRACKING_ROLES = ["Admin", "HR", "Manager"];
const POLL_INTERVAL_MS = 15_000;

function FitToView({ 
  currentLocation, 
  liveLocations, 
  routePoints 
}: { 
  currentLocation: [number, number] | null;
  liveLocations: LiveLocationDto[];
  routePoints: LocationPointDto[];
}) {
  const map = useMap();
  
  useEffect(() => {
    // Collect all positions to fit
    const positions: [number, number][] = [];
    
    if (currentLocation) {
      positions.push(currentLocation);
    }
    
    liveLocations.forEach((loc) => {
      if (loc.lastLatitude != null && loc.lastLongitude != null) {
        positions.push([loc.lastLatitude, loc.lastLongitude]);
      }
    });
    
    routePoints.forEach((point) => {
      positions.push([point.latitude, point.longitude]);
    });

    if (positions.length > 0) {
      if (currentLocation && positions.length > 1) {
        // Prioritize current location at detailed zoom
        map.setView(currentLocation, 17);
      } else if (positions.length > 1) {
        const bounds = L.latLngBounds(positions);
        map.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });
      } else if (positions.length === 1) {
        map.setView(positions[0], 15);
      }
    }
  }, [currentLocation, liveLocations, routePoints, map]);
  
  return null;
}

export default function UnifiedTrackingMap({ attendanceId, mode = "personal" }: { attendanceId?: string; mode?: "personal" | "all" }) {
  const { user } = useAuth();
  const [currentLocation, setCurrentLocation] = useState<[number, number] | null>(null);
  const [liveLocations, setLiveLocations] = useState<LiveLocationDto[]>([]);
  const [routeHistory, setRouteHistory] = useState<TrackingHistoryDto | null>(null);
  const [lastPolledAt, setLastPolledAt] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [geoError, setGeoError] = useState<string | null>(null);
  const [routeError, setRouteError] = useState<string | null>(null);

  const canViewAll = mode === "all" || (!!user && TRACKING_ROLES.some((role) => user.roles.includes(role)));

  // Watch current device GPS location
  useEffect(() => {
    if (!navigator.geolocation) {
      setGeoError("This browser does not support location access.");
      return;
    }

    console.log("[UnifiedTrackingMap] Starting geolocation watch...");
    
    const watchId = navigator.geolocation.watchPosition(
      (position) => {
        const lat = position.coords.latitude;
        const lng = position.coords.longitude;
        
        setGeoError(null);
        setCurrentLocation([lat, lng]);
        console.log("[UnifiedTrackingMap] Updated current location:", { lat: lat.toFixed(6), lng: lng.toFixed(6) });
      },
      (error) => {
        const message = error.code === 1
          ? "Location permission was denied."
          : error.code === 2
            ? "GPS signal unavailable."
            : "Location request timed out.";
        
        setGeoError(message);
        setCurrentLocation(null);
        console.error("[UnifiedTrackingMap] Geolocation error:", message);
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
    );

    return () => navigator.geolocation.clearWatch(watchId);
  }, []);

  // Poll live tracking data
  useEffect(() => {
    let cancelled = false;

    function poll() {
      // If viewing route history, don't poll live locations
      if (attendanceId) {
        setLiveLocations([]);
        setIsLoading(false);
        return;
      }

      const endpoint = canViewAll
        ? "/tracking/live"
        : user?.employeeId
          ? `/tracking/live/${user.employeeId}`
          : null;

      if (!endpoint) {
        if (!cancelled) {
          setLiveLocations([]);
          setIsLoading(false);
        }
        return;
      }

      api
        .get<{ data: LiveLocationDto | LiveLocationDto[] }>(endpoint)
        .then((res) => {
          if (cancelled) return;

          const payload = res.data?.data;
          let nextLocations: LiveLocationDto[] = [];

          if (canViewAll) {
            nextLocations = Array.isArray(payload) ? payload : [];
          } else if (payload && typeof payload === "object" && "isActive" in payload) {
            nextLocations = [payload as LiveLocationDto];
          }

          setLiveLocations(nextLocations.filter((loc) => loc?.isActive && loc.lastLatitude != null && loc.lastLongitude != null));
          setLastPolledAt(new Date().toISOString());
        })
        .catch((err) => {
          if (!cancelled) {
            console.warn("[UnifiedTrackingMap] Failed to fetch live locations:", err);
            setLiveLocations([]);
          }
        })
        .finally(() => !cancelled && setIsLoading(false));
    }

    poll();
    const interval = setInterval(poll, POLL_INTERVAL_MS);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, [canViewAll, user?.employeeId, attendanceId]);

  // Load route history if attendanceId provided
  useEffect(() => {
    if (!attendanceId) {
      setRouteHistory(null);
      setRouteError(null);
      return;
    }

    setRouteError(null);
    setRouteHistory(null);
    setIsLoading(true);

    api
      .get<{ data: TrackingHistoryDto }>(`/tracking/history/${attendanceId}`)
      .then((res) => {
        setRouteHistory(res.data.data);
        setIsLoading(false);
      })
      .catch((err) => {
        const status = err?.response?.status;
        const errorMsg = status === 404
          ? "No GPS tracking recorded for this record."
          : getErrorMessage(err, "Could not load tracking history.");
        
        setRouteError(errorMsg);
        setRouteHistory(null);
        setIsLoading(false);
        
        console.error("[UnifiedTrackingMap] Route history error:", { status, errorMsg });
      });
  }, [attendanceId]);

  const routePoints = routeHistory?.points ?? [];
  const emptyMessage = canViewAll
    ? "No employees are currently tracked."
    : "No active tracking session.";

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between text-sm">
        <span className="text-slate-500">
          {routeHistory
            ? `Tracking route - ${routePoints.length} points`
            : canViewAll
              ? `${liveLocations.length} employee${liveLocations.length === 1 ? "" : "s"} tracking`
              : currentLocation
                ? "Your live location"
                : "Live tracking"}
        </span>
        <span className="text-xs text-slate-400">
          {lastPolledAt ? `Updated ${new Date(lastPolledAt).toLocaleTimeString()}` : "Loading…"}
        </span>
      </div>

      {geoError && (
        <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-xs text-danger">
          {geoError}
        </div>
      )}

      {routeError && (
        <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-xs text-danger">
          {routeError}
        </div>
      )}

      <div className="rounded-lg border border-border overflow-hidden shadow-card h-[28rem] min-h-[18rem] bg-slate-50">
        {routeError && !routeHistory ? (
          <div className="h-full flex items-center justify-center text-sm text-slate-400 bg-white">
            <div className="text-center">
              <p className="text-danger font-medium mb-2">Unable to load route</p>
              <p className="text-xs text-slate-500">{routeError}</p>
            </div>
          </div>
        ) : isLoading && !currentLocation && !routeHistory ? (
          <div className="h-full flex items-center justify-center text-sm text-slate-400 bg-white">
            Loading map...
          </div>
        ) : currentLocation || routePoints.length > 0 || liveLocations.length > 0 ? (
          <MapContainer
            center={[0, 0]}
            zoom={5}
            scrollWheelZoom
            className="h-full w-full z-0"
            style={{ height: "100%", width: "100%" }}
          >
            <TileLayer attribution="&copy; OpenStreetMap contributors" url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
            <FitToView currentLocation={currentLocation} liveLocations={liveLocations} routePoints={routePoints} />

            {/* Route polyline and markers (if viewing history) */}
            {routePoints.length > 0 && (
              <>
                <Polyline positions={routePoints.map((p) => [p.latitude, p.longitude])} pathOptions={{ color: "#14385E", weight: 3, opacity: 0.6 }} />
                
                {routePoints.length > 0 && (
                  <Marker position={[routePoints[0].latitude, routePoints[0].longitude]} icon={startIcon}>
                    <Popup>Start — {new Date(routePoints[0].recordedAtUtc).toLocaleTimeString()}</Popup>
                  </Marker>
                )}
                
                {routePoints.length > 1 && (
                  <Marker position={[routePoints[routePoints.length - 1].latitude, routePoints[routePoints.length - 1].longitude]} icon={endIcon}>
                    <Popup>End — {new Date(routePoints[routePoints.length - 1].recordedAtUtc).toLocaleTimeString()}</Popup>
                  </Marker>
                )}
              </>
            )}

            {/* Current location marker */}
            {currentLocation && (
              <Marker position={currentLocation} icon={currentLocationIcon}>
                <Popup>Your current location</Popup>
              </Marker>
            )}

            {/* Live employee markers */}
            {liveLocations
              .filter((loc) => loc.lastLatitude != null && loc.lastLongitude != null)
              .map((loc) => (
                <Marker key={`${loc.employeeId}-${loc.trackingSessionId}`} position={[loc.lastLatitude!, loc.lastLongitude!]} icon={employeeIcon}>
                  <Popup>
                    <div className="text-sm">
                      <div className="font-medium">{loc.employeeName}</div>
                      <div className="text-xs text-slate-500 mt-1">
                        Last update: {loc.lastRecordedAtUtc ? new Date(loc.lastRecordedAtUtc).toLocaleTimeString() : "—"}
                      </div>
                      {loc.lastSpeedKmh != null && <div className="text-xs text-slate-500">Speed: {loc.lastSpeedKmh.toFixed(1)} km/h</div>}
                      {loc.lastBatteryPercent != null && <div className="text-xs text-slate-500">Battery: {loc.lastBatteryPercent}%</div>}
                    </div>
                  </Popup>
                </Marker>
              ))}
          </MapContainer>
        ) : (
          <div className="h-full flex items-center justify-center text-sm text-slate-400 bg-white">
            {emptyMessage}
          </div>
        )}
      </div>
    </div>
  );
}
