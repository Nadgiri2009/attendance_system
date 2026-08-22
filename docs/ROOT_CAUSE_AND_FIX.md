# GPS Tracking Network Error - Root Cause & Solution

## The Problem
**"AxiosError: Network Error"** when trying to record GPS locations after check-in.

## Root Cause Found ✅
Your frontend was configured to connect to the **wrong backend port**:
- ❌ Frontend was looking for: `http://localhost:5007/api/v1`
- ✅ Backend was running on: `http://localhost:5000`

This caused all API calls to fail with "Network Error" because the frontend couldn't reach the backend.

## Solution Applied ✅

### File: `frontend/.env.local`
Changed from:
```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5007/api/v1
```

To:
```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000/api/v1
```

## How to Apply This Fix

### Step 1: Clear Frontend Build Cache
```bash
cd frontend
rm -r .next  # Remove Next.js cache
```

### Step 2: Rebuild Frontend
```bash
npm run build
```

Or for development (auto-rebuilds):
```bash
npm run dev
```

### Step 3: Start Both Servers

**Terminal 1 - Backend:**
```bash
cd backend
dotnet run --project src/EWMS.API/EWMS.API.csproj
# Expected output: "Now listening on: http://localhost:5000"
```

**Terminal 2 - Frontend:**
```bash
cd frontend
npm run dev
# Expected output: "- Local: http://localhost:3000"
```

### Step 4: Verify the Fix

1. Open http://localhost:3000 in your browser
2. Open DevTools (F12) → Console
3. Look for: `[API] Initialized with base URL: http://localhost:5000/api/v1`
4. Log in and check in
5. Tracking should now show "Tracking Active" ✅

## What Was Happening

### Before Fix (Port Mismatch):
```
Frontend @ localhost:3000
    ↓ tries to connect to
localhost:5007 ❌ (nothing listening there)
    ↓ Network Error
GPS tracking fails
```

### After Fix (Correct Ports):
```
Frontend @ localhost:3000
    ↓ connects to
Backend @ localhost:5000 ✅
    ↓ Success (200 OK)
GPS tracking starts ✅
```

## Port Configuration Reference

### Backend Configuration
**File**: `backend/src/EWMS.API/Properties/launchSettings.json`
```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5000"
    },
    "https": {
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    }
  }
}
```

### Frontend Configuration
**File**: `frontend/.env.local`
```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000/api/v1
```

### CORS Configuration
**File**: `backend/src/EWMS.API/appsettings.Development.json`
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3000",
      "http://127.0.0.1:3000"
    ]
  }
}
```

## Additional Improvements Made

Beyond fixing the port issue, I've also added:

1. **Better Error Logging**
   - All API calls now logged with `[API]` prefix
   - Network errors show the expected API URL
   - Easy to spot what went wrong

2. **Improved Status Display**
   - "Tracking (Offline)" status when points are queued
   - Shows GPS accuracy, points captured
   - Clear indication when auto-retrying

3. **Offline Support**
   - Points queue locally when offline
   - Auto-retry every 15 seconds
   - Detailed offline mode messages

4. **Diagnostic Tools**
   - `test-gps-connectivity.ps1` - Verifies setup
   - `TROUBLESHOOTING_GPS.md` - Comprehensive guide
   - Console logging for debugging

## Testing Checklist

- [ ] Updated `frontend/.env.local` with correct port (5000)
- [ ] Cleared `.next` build cache
- [ ] Rebuilt frontend with `npm run build`
- [ ] Started backend on port 5000
- [ ] Started frontend on port 3000
- [ ] Console shows `[API] Initialized with base URL: http://localhost:5000/api/v1`
- [ ] Checked in successfully
- [ ] Tracking shows "Tracking Active"
- [ ] Console shows `[LocationTracker] Tracking started successfully`
- [ ] GPS coordinates appear in status card
- [ ] Locations being captured (see `[LocationTracker] Location sent` messages)

## Quick Test

```powershell
# Verify backend is accessible
curl http://localhost:5000/health

# Verify frontend can see it
curl http://localhost:5000/api/v1/tracking/live/00000000-0000-0000-0000-000000000000
# (May return 404 for empty tracking, that's fine)
```

## If Issues Persist

1. **Clear browser storage**: DevTools → Application → Clear All Storage
2. **Clear npm cache**: `npm cache clean --force`
3. **Restart both servers**: Kill terminals and restart
4. **Check logs**: Look for `[API]` and `[LocationTracker]` in console
5. **Run diagnostic**: `.\test-gps-connectivity.ps1`

## Summary

| Issue | Cause | Fix |
|-------|-------|-----|
| Network Error | Port mismatch (5007 vs 5000) | Updated .env.local |
| Tracking not active | Network calls failing | Fixed port, logging added |
| No error details | Missing diagnostics | Added console logging |

The GPS tracking should now work correctly! ✅
