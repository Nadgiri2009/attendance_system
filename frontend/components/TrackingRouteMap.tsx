"use client";

import { useEffect, useMemo, useState } from "react";
import { MapContainer, TileLayer, Marker, Polyline, Popup, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import L from "leaflet";
import { LocationPointDto } from "@/lib/types";

function dotIcon(color: string) {
  return L.divIcon({
    className: "",
    html: `<span style="display:block;width:16px;height:16px;border-radius:50%;background:${color};border:2px solid white;box-shadow:0 0 0 1px rgba(0,0,0,0.25)"></span>`,
    iconSize: [16, 16],
    iconAnchor: [8, 8]
  });
}
const startIcon = dotIcon("#1E8E5A");
const endIcon = dotIcon("#C13A3A");
const cursorIcon = dotIcon("#14385E");

function FitToPath({ positions }: { positions: [number, number][] }) {
  const map = useMap();
  useEffect(() => {
    if (positions.length > 0) map.fitBounds(positions, { padding: [30, 30] });
  }, [positions, map]);
  return null;
}

export default function TrackingRouteMap({ points }: { points: LocationPointDto[] }) {
  const [playbackIndex, setPlaybackIndex] = useState(0);

  const positions = useMemo<[number, number][]>(
    () => points.map((p) => [p.latitude, p.longitude]),
    [points]
  );

  useEffect(() => setPlaybackIndex(points.length > 0 ? points.length - 1 : 0), [points]);

  if (positions.length === 0) {
    return <div className="h-full flex items-center justify-center text-sm text-slate-400 bg-white">No GPS points captured yet.</div>;
  }

  const cursor = points[playbackIndex];

  return (
    <div className="flex flex-col h-full">
      <div className="flex-1 min-h-0">
        <MapContainer center={positions[0]} zoom={15} scrollWheelZoom style={{ height: "100%", width: "100%" }}>
          <TileLayer attribution="&copy; OpenStreetMap contributors" url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
          <FitToPath positions={positions} />
          <Polyline positions={positions} pathOptions={{ color: "#14385E", weight: 3 }} />
          <Marker position={positions[0]} icon={startIcon}>
            <Popup>Start — {new Date(points[0].recordedAtUtc).toLocaleTimeString()}</Popup>
          </Marker>
          {positions.length > 1 && (
            <Marker position={positions[positions.length - 1]} icon={endIcon}>
              <Popup>Latest — {new Date(points[points.length - 1].recordedAtUtc).toLocaleTimeString()}</Popup>
            </Marker>
          )}
          {cursor && (
            <Marker position={[cursor.latitude, cursor.longitude]} icon={cursorIcon}>
              <Popup>{new Date(cursor.recordedAtUtc).toLocaleTimeString()}</Popup>
            </Marker>
          )}
        </MapContainer>
      </div>

      {points.length > 1 && (
        <div className="px-1 pt-2 flex items-center gap-3">
          <span className="text-xs text-slate-400 font-mono w-16">
            {new Date(cursor.recordedAtUtc).toLocaleTimeString()}
          </span>
          <input
            type="range"
            min={0}
            max={points.length - 1}
            value={playbackIndex}
            onChange={(e) => setPlaybackIndex(Number(e.target.value))}
            className="flex-1"
          />
          <span className="text-xs text-slate-400 w-20 text-right">
            Point {playbackIndex + 1} / {points.length}
          </span>
        </div>
      )}
    </div>
  );
}
