"use client";

import { useEffect, useMemo, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import L from "leaflet";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { LiveLocationDto } from "@/lib/types";

const employeeIcon = L.divIcon({
  className: "",
  html: `<span style="display:flex;align-items:center;justify-content:center;width:22px;height:22px;border-radius:50%;background:#14385E;border:2px solid white;box-shadow:0 0 0 1px rgba(0,0,0,0.25);color:white;font-size:11px;">●</span>`,
  iconSize: [22, 22],
  iconAnchor: [11, 11]
});

const TRACKING_ROLES = ["Admin", "HR", "Manager"];
const POLL_INTERVAL_MS = 15_000;

function FitToMarkers({ locations, currentLocation }: { locations: LiveLocationDto[]; currentLocation: [number, number] | null }) {
  const map = useMap();
  useEffect(() => {
    // Priority 1: If user has a live location, center there
    if (currentLocation) {
      map.setView(currentLocation, 17);
      console.log("[LiveTrackingMap] Centered on current location:", currentLocation);
      return;
    }

    // Priority 2: If there are employee locations, fit bounds to them
    const positions = locations
      .filter((l) => l.lastLatitude != null && l.lastLongitude != null)
      .map((l) => [l.lastLatitude as number, l.lastLongitude as number] as [number, number]);

    if (positions.length > 0) {
      const bounds = L.latLngBounds(positions);
      map.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });
      console.log("[LiveTrackingMap] Fitted bounds to employee locations:", positions);
    }
  }, [locations, currentLocation, map]);
  return null;
}

export default function LiveTrackingMap() {
  const { user } = useAuth();
  const [locations, setLocations] = useState<LiveLocationDto[]>([]);
  const [lastPolledAt, setLastPolledAt] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [currentLocation, setCurrentLocation] = useState<[number, number] | null>(null);
  const [geoError, setGeoError] = useState<string | null>(null);

  const canViewAll = !!user && TRACKING_ROLES.some((role) => user.roles.includes(role));

  useEffect(() => {
    if (!navigator.geolocation) {
      setGeoError("This browser does not support location access.");
      return;
    }

    console.log("[LiveTrackingMap] Starting geolocation watch for live location...");

    const watchId = navigator.geolocation.watchPosition(
      (position) => {
        const lat = position.coords.latitude;
        const lng = position.coords.longitude;
        const accuracy = position.coords.accuracy;
        
        setGeoError(null);
        setCurrentLocation([lat, lng]);
        
        console.log("[LiveTrackingMap] Got fresh GPS position:", {
          latitude: lat.toFixed(6),
          longitude: lng.toFixed(6),
          accuracy: accuracy?.toFixed(1) + "m"
        });
      },
      (error) => {
        const message = error.code === 1
          ? "Location permission was denied. Please allow browser location access to see the live GPS position."
          : error.code === 2
            ? "GPS signal is unavailable right now. Try again in an open area."
            : error.code === 3
              ? "Location request timed out. Please try again."
              : "Could not read your current location.";

        setGeoError(message);
        setCurrentLocation(null);
        console.error("[LiveTrackingMap] Geolocation error code " + error.code + ":", message);
      },
      { 
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 0  // Always get fresh GPS, never use cached position
      }
    );

    return () => {
      navigator.geolocation.clearWatch(watchId);
      console.log("[LiveTrackingMap] Stopped geolocation watch");
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    function poll() {
      const endpoint = canViewAll
        ? "/tracking/live"
        : user?.employeeId
          ? `/tracking/live/${user.employeeId}`
          : null;

      if (!endpoint) {
        if (!cancelled) {
          setLocations([]);
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

          setLocations(nextLocations.filter((loc) => loc?.isActive && loc.lastLatitude != null && loc.lastLongitude != null));
          setLastPolledAt(new Date().toISOString());
        })
        .catch((err) => {
          if (!cancelled) {
            console.warn("[LiveTrackingMap] Failed to fetch live locations:", err);
            setLocations([]);
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
  }, [canViewAll, user?.employeeId]);

  const emptyMessage = canViewAll
    ? "No employees are currently checked in with active GPS tracking."
    : "You are not currently checked in with an active GPS tracking session.";

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between text-sm">
        <span className="text-slate-500">
          {canViewAll
            ? `${locations.length} employee${locations.length === 1 ? "" : "s"} currently tracking`
            : locations.length > 0 ? "Your live location" : "Live tracking status"}
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

      <div className="rounded-lg border border-border overflow-hidden shadow-card h-[28rem] min-h-[18rem] bg-slate-50">
        {isLoading || !currentLocation ? (
          <div className="h-full flex items-center justify-center text-sm text-slate-400 bg-white">
            {!currentLocation && geoError ? "Waiting for location..." : "Loading map..."}
          </div>
        ) : (
          <MapContainer
            center={[0, 0]}
            zoom={5}
            scrollWheelZoom
            className="h-full w-full z-0"
            style={{ height: "500px", width: "100%", minHeight: "18rem" }}
          >
            <TileLayer attribution="&copy; OpenStreetMap contributors" url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
            <FitToMarkers locations={locations} currentLocation={currentLocation} />
            {currentLocation && (
              <Marker position={currentLocation} icon={L.divIcon({
                className: "",
                html: `<span style="display:flex;align-items:center;justify-content:center;width:18px;height:18px;border-radius:50%;background:#1d4ed8;border:2px solid white;box-shadow:0 0 0 2px rgba(29,78,216,0.15);color:white;font-size:9px;">●</span>`,
                iconSize: [18, 18],
                iconAnchor: [9, 9]
              })}>
                <Popup>
                  <div className="text-sm">
                    <div className="font-medium">Your live location</div>
                    <div className="text-xs text-slate-500 mt-1">Current browser GPS position</div>
                  </div>
                </Popup>
              </Marker>
            )}
            {locations
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
        )}
      </div>

      {!isLoading && locations.length === 0 && (
        <p className="text-sm text-slate-400 text-center py-4">{emptyMessage}</p>
      )}
    </div>
  );
}
