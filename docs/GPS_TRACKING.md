# Continuous GPS Tracking

Tracks an employee's location continuously between Check-In and Check-Out.
Built on the existing Clean Architecture / CQRS / MediatR / FluentValidation
/ EF Core conventions already used by the Employee and Attendance modules —
no new architectural patterns were introduced.

## Design decisions worth knowing

**`GpsLog` is reused as the location-history table, not duplicated.** The
spec asked for an `EmployeeLocationHistory` table with fields
(EmployeeId, AttendanceId, TrackingSessionId, Latitude, Longitude, Accuracy,
Speed, Heading, BatteryLevel, RecordedAt). A `GpsLog`/`GPS.GpsLogs` table
already existed serving exactly this purpose (it's what Check-In/Check-Out
already wrote a point to). Rather than create a second, near-identical
table, `GpsLog` was extended with the two fields it was missing
(`TrackingSessionId`, `Heading`) and continuous tracking points are stored
there, distinguished from Check-In/Check-Out's one-off points by having a
non-null `TrackingSessionId`. This is the "avoid duplicate implementations"
instruction applied to the schema, not just the code.

**SignalR was not implemented; live tracking uses polling instead.** The
spec listed SignalR as optional. `GET /api/tracking/live` (all employees)
and `GET /api/tracking/live/{employeeId}` (one) are polled every 15s by
`components/LiveTrackingMap.tsx`. This keeps the feature consistent with
the rest of this REST-based app and avoids standing up a new hub, its DI
wiring, and its own connection-lifecycle handling for a feature explicitly
marked optional. If push-based updates become a real requirement later,
`TrackingHub` would broadcast on the same `RecordLocationCommandHandler`
save, and `LiveTrackingMap`'s polling `useEffect` is a drop-in point to
replace with a hub connection.

**Mock-location detection isn't implemented.** The browser Geolocation API
has no concept of "mock location" — that's an Android OS/native-app
concept (spoofed GPS providers), not something a web page can detect.
`isMockLocation` is always sent as `false` from the frontend, matching how
Check-In/Check-Out already behaved before this feature existed. A future
React Native or Capacitor wrapper around this same app could set it
honestly using native APIs; the field and column already exist for that.

**Continuous tracking is foreground-only, by browser design.** No web page
can run JavaScript after its tab is actually closed or the browser process
is killed — this is a browser sandboxing constraint, not something this
codebase can work around. What's implemented instead:
- `navigator.geolocation.watchPosition` runs for as long as the tab is
  open, and survives in-app navigation between pages because the tracker
  is a module-level singleton (`lib/locationTracker.ts`), not tied to any
  one React component's lifecycle.
- A page reload resumes tracking automatically: `TrackingResumer` (mounted
  once in the dashboard shell) checks on load whether the employee is still
  checked in and, if so, calls `locationTracker.resumeIfNeeded(...)`, which
  re-issues `/tracking/start` (idempotent — see below) and resumes
  `watchPosition`.
- On tab close (`pagehide`), a best-effort `fetch(..., { keepalive: true })`
  call to `/tracking/stop` is fired. `keepalive: true` (not `sendBeacon`)
  was used deliberately: `sendBeacon` cannot attach custom headers, so it
  can't carry the `Authorization: Bearer` token this API requires — it
  would always fail with 401. `fetch` with `keepalive: true` does support
  headers and is the correct modern replacement for this exact scenario.
  It's still best-effort: if the OS kills the browser process instead of a
  clean tab close, this never fires.
- As a **server-side safety net**, `CheckOutCommandHandler` also stops any
  active tracking session for that attendance record directly (reusing
  `StopTrackingSessionCommandHandler` via `ISender`), so a session can't
  stay "Active" forever just because the client-side stop call was missed.
- What's *not* built: a scheduled job that reaps sessions abandoned by a
  crashed browser/OS (no ping for N minutes ⇒ auto-stop). This project has
  no background job runner wired in (Hangfire was in the original tech
  list but isn't actually configured anywhere in this codebase), and adding
  one was out of scope for this pass. Until that exists, a session can be
  left `Active` indefinitely if the tab is killed abnormally *and* the
  employee never explicitly checks out. This is the main known gap — see
  "Remaining issues" below.

## Data model

```
TrackingSession (GPS.TrackingSessions) — one per Check-In
  EmployeeId, AttendanceRecordId (unique — one session per attendance)
  StartedAtUtc, StartLatitude/Longitude, StartAccuracyMeters,
  StartBatteryPercent, DeviceInfo
  EndedAtUtc, EndLatitude/Longitude
  TotalDistanceMeters, TotalDurationSeconds, TotalPointsCaptured
  Status: Active | Stopped

GpsLog (GPS.GpsLogs) — one row per captured point (continuous tracking)
  or per Check-In/Check-Out (ad-hoc, TrackingSessionId = null)
  EmployeeId, AttendanceRecordId, TrackingSessionId (nullable)
  Latitude, Longitude, AccuracyMeters, SpeedKmh, Heading, BatteryPercent,
  IsMockLocation, RecordedAtUtc
```

A filtered unique index (`IX_TrackingSessions_OneActivePerEmployee`, on
`EmployeeId` `WHERE Status = 'Active'`) enforces "no two active sessions for
the same employee" at the database level, in addition to the application
check in `StartTrackingSessionCommandValidator` — so this holds even under
concurrent requests, not just in the common case.

## API endpoints

| Method | Route | Notes |
|---|---|---|
| POST | `/api/v1/tracking/start` | Idempotent per attendance record — a repeat call for the same `attendanceRecordId` returns the existing session instead of erroring. |
| POST | `/api/v1/tracking/location` | Rejected (400) if the session isn't `Active`. |
| POST | `/api/v1/tracking/stop` | Idempotent — stopping an already-stopped session is a no-op success, not an error. Computes `TotalDistanceMeters` (Haversine sum across all captured points), `TotalDurationSeconds`, `TotalPointsCaptured`. |
| GET | `/api/v1/tracking/history/{attendanceId}` | Session summary + every captured point, ordered — powers the route map + playback. |
| GET | `/api/v1/tracking/live/{employeeId}` | One employee's current session (`isActive: false` if none). |
| GET | `/api/v1/tracking/live` | **Additive beyond the spec** — every currently-active session at once, `Admin`/`HR`/`Manager` only. Needed for the live dashboard (requirement 7) to show more than one employee at a time. |

## Frontend

- `lib/locationTracker.ts` — singleton background service. Throttles
  uploads to the spec's range (every 20s **or** 30m of movement, whichever
  comes first — both configurable via constants at the top of the file).
  Failed uploads are queued in `localStorage` and retried on an interval
  and on the browser's `online` event ("queue locally if offline and sync
  when connection is restored").
- `components/TrackingResumer.tsx` — mounted once in the dashboard shell;
  resumes tracking after a reload if still checked in.
- `components/TrackingStatusCard.tsx` — "Tracking Active/Inactive", current
  coordinates, last sync time, pending-queue count, permission/GPS errors.
- `components/TrackingRouteMap.tsx` — polyline + start/end markers +
  a scrub-through playback slider, used from `TrackingHistoryPanel`
  (wired into the Attendance management table's new "View route" action).
- `components/LiveTrackingMap.tsx` + `app/(dashboard)/tracking/page.tsx` —
  the live dashboard, `Admin`/`HR`/`Manager` only (hidden from the sidebar
  and guarded again on the page itself for direct-URL access).

## Validation summary

| Rule | Enforced by |
|---|---|
| No tracking before Check-In / after Check-Out | `StartTrackingSessionCommandValidator` (attendance must be checked-in, not checked-out) |
| No location points once a session is stopped | `RecordLocationCommandValidator` |
| One active session per employee | `StartTrackingSessionCommandValidator` (app-level) + filtered unique index (DB-level) |
| Denied GPS permission / lost signal / offline | Handled client-side in `locationTracker.ts` — surfaced via `TrackingStatus.lastError`/`permissionDenied`, never thrown as an unhandled error |

## Remaining issues

- **No stale-session reaper.** A session can stay `Active` if the browser
  process is killed abnormally and the employee never checks out through
  this app. Fixing this properly needs a background job runner, which
  isn't part of this codebase yet — out of scope for this pass.
- **SignalR not implemented** (optional per spec) — see design decision
  above.
- **`getBattery()` (Battery Status API) is deprecated/unavailable** in
  Firefox and many Chrome contexts; `BatteryPercent` will be `null` on
  those browsers. This is a browser platform limitation, not a bug — the
  code already feature-detects and degrades gracefully rather than
  failing tracking.
- This was reviewed carefully but not run against a live .NET/SQL
  Server/browser environment in this pass — recommend a real
  `dotnet build`, an EF Core migration, and a manual Check-In → move
  around → Check-Out smoke test (with browser dev tools open to watch
  the Network tab for `/tracking/location` calls) before relying on it.
