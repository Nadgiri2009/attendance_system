# GPS Tracking Troubleshooting Guide

## Problem: "Checked in, but GPS tracking could not be started" or "AxiosError: Network Error"

This guide helps you diagnose and fix GPS tracking issues.

## Quick Diagnostic Checklist

### 1. Backend Server Status
```powershell
# Check if backend is running on port 5000
netstat -ano | findstr :5000

# Or try to access it
curl -X GET http://localhost:5000/health
```

**Expected**: Backend should respond with HTTP 200 on `/health`

### 2. Frontend Configuration
- Open DevTools (F12)
- Check Console tab
- Look for: `[API] Initialized with base URL: http://localhost:5000/api/v1`

**Expected**: Should show `http://localhost:5000/api/v1`
**If wrong**: Set `NEXT_PUBLIC_API_BASE_URL` environment variable before running frontend

### 3. CORS Configuration
- Backend must allow frontend origin in `appsettings.Development.json`
- Already configured for: `http://localhost:3000`, `http://localhost:3000`, `http://127.0.0.1:3000`

### 4. Check Network Errors in Console
Open DevTools → Console tab and look for:

```
[API Request] POST /tracking/start
[API Response] 200 /tracking/start
```

OR errors like:

```
[API Error] {
  url: "/tracking/start",
  status: undefined,
  message: "Network Error",
  isNetworkError: true
}
```

### 5. Verify Location Permission
- Chrome: Settings → Privacy and security → Site settings → Location
- Firefox: Address bar lock icon → Permissions → Location
- Safari: Preferences → Privacy → Location

**Expected**: EWMS app should have "Allow" or "Allow" permission

## Common Issues and Solutions

### Issue 1: "Network Error" in Console

**Cause**: Backend server not running or not accessible

**Solution**:
```bash
cd backend
dotnet run --project src/EWMS.API/EWMS.API.csproj
```

Expected output:
```
Now listening on: http://localhost:5000
```

### Issue 2: CORS Error (Request blocked by browser)

**Cause**: Frontend origin not allowed in backend CORS config

**Solution**:
1. Edit `backend/src/EWMS.API/appsettings.Development.json`
2. Add your frontend origin to `Cors.AllowedOrigins`:
```json
"Cors": {
  "AllowedOrigins": ["http://localhost:3000", "http://your-frontend-url"]
}
```
3. Restart backend

### Issue 3: 401 Unauthorized Error

**Cause**: Auth token missing or expired

**Solution**:
1. Clear browser storage: DevTools → Application → Storage → Clear All
2. Log out and log back in
3. Check that Bearer token is being sent: DevTools → Network → POST /tracking/start → Headers → Authorization

### Issue 4: Location Permission Denied

**Cause**: Browser geolocation permission not granted

**Solution**:
1. Click browser lock icon next to URL
2. Find "Location" permission
3. Click dropdown and select "Allow"
4. Refresh page and try check-in again

### Issue 5: Tracking shows "Offline" but Internet is Connected

**Cause**: Queued locations couldn't sync (temporary network hiccup)

**Solution**:
- System will auto-retry every 15 seconds
- Check Console for `[LocationTracker] Flushing queue`
- Wait for "Offline mode active" message to disappear

## Advanced Debugging

### Step 1: Enable Detailed Console Logging

1. Open DevTools → Console
2. Look for all messages starting with:
   - `[API]` - API client activity
   - `[LocationTracker]` - GPS tracking activity
   - `[API Error]` - Network errors

### Step 2: Test API Endpoints Directly

```powershell
# Test backend health
curl -X GET http://localhost:5000/health

# Get your access token from DevTools → Application → Local Storage → ewms_access_token
# Replace TOKEN below
$headers = @{
    "Authorization" = "Bearer YOUR_TOKEN_HERE"
    "Content-Type" = "application/json"
}

# Test tracking start (replace with real IDs)
$body = @{
    employeeId = "00000000-0000-0000-0000-000000000000"
    attendanceRecordId = "00000000-0000-0000-0000-000000000000"
    latitude = 40.7128
    longitude = -74.0060
    accuracyMeters = 10
} | ConvertTo-Json

curl -X POST http://localhost:5000/api/v1/tracking/start `
  -Headers $headers `
  -Body $body
```

### Step 3: Check Database

```sql
-- Check if tracking session was created
SELECT * FROM GPS.TrackingSessions 
WHERE EmployeeId = 'your-employee-id'
ORDER BY StartedAtUtc DESC

-- Check if GPS locations were recorded
SELECT * FROM GPS.GpsLogs 
WHERE TrackingSessionId IS NOT NULL
ORDER BY RecordedAtUtc DESC
LIMIT 10
```

### Step 4: Monitor Real-Time Logs

**Frontend (Browser Console)**:
- All `[LocationTracker]` and `[API]` messages

**Backend (Terminal)**:
```
info: EWMS.Application.Tracking.Commands.StartTrackingSession.StartTrackingSessionCommandHandler
[Start GPS tracking session ...]
```

## Network Error Resolution Workflow

1. **Immediate**: Check backend is running
   - Open http://localhost:5000/health in browser
   - Should see: `Healthy`

2. **If 404 on /health**: Backend port is wrong or not running
   - Edit `frontend/lib/api.ts` if needed to use different port
   - Restart backend on correct port

3. **If CORS error**: Update `appsettings.Development.json`
   - Add your frontend URL to `Cors.AllowedOrigins`

4. **If 401 Unauthorized**: Clear auth and log back in
   - DevTools → Application → Clear All Storage
   - Log out from EWMS
   - Log back in

5. **If locations still don't sync**: 
   - Wait 15 seconds for auto-retry
   - Check Console for retry messages
   - Manually refresh page to trigger queue flush

## Tracking Active State

**Tracking should show "Tracking Active" when**:
- ✅ Check-in successful (backend returns `attendanceId`)
- ✅ Geolocation permission granted
- ✅ Current location obtained successfully
- ✅ `/tracking/start` API call succeeds

**Tracking shows "Tracking (Offline)" when**:
- ℹ️ Tracking is active BUT points are queued
- ℹ️ Network error occurred while sending locations
- ✅ Still capturing location data (will sync automatically)

**Tracking shows "Tracking Inactive" when**:
- ❌ Not checked in
- ❌ Geolocation permission denied
- ❌ Check-out completed

## Quick Reference: Environment Variables

```bash
# Frontend - set before npm run dev
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000/api/v1

# Backend - set in appsettings.Development.json or via env
ASPNETCORE_URLS=http://localhost:5000
```

## Still Having Issues?

1. **Check logs**: DevTools Console for `[LocationTracker]` and `[API]` prefixed messages
2. **Restart everything**:
   ```bash
   # Terminal 1: Backend
   cd backend && dotnet run --project src/EWMS.API/EWMS.API.csproj
   
   # Terminal 2: Frontend
   cd frontend && npm run dev
   ```
3. **Clear everything**:
   - Browser: DevTools → Application → Clear All Storage
   - Database: Reseed via backend startup
4. **Try fresh check-in**: Log out, log in, check in again
