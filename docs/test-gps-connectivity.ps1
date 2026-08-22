#!/usr/bin/env pwsh
# GPS Tracking Connectivity Test Script
# Usage: .\test-gps-connectivity.ps1

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║          GPS Tracking Connectivity Test                        ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Configuration
$backendUrl = "http://localhost:5000"
$apiUrl = "$backendUrl/api/v1"
$healthUrl = "$backendUrl/health"

Write-Host "📋 Configuration:" -ForegroundColor Yellow
Write-Host "  Backend URL: $backendUrl"
Write-Host "  API Base URL: $apiUrl"
Write-Host ""

# Test 1: Backend connectivity
Write-Host "🔍 Test 1: Backend Server Connectivity" -ForegroundColor Yellow
Write-Host "  Checking: $healthUrl"

try {
    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 5
    Write-Host "  ✅ Backend is running (HTTP $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "  ❌ Backend not accessible" -ForegroundColor Red
    Write-Host "     Error: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "🔧 To start the backend:" -ForegroundColor Yellow
    Write-Host "   cd backend"
    Write-Host "   dotnet run --project src/EWMS.API/EWMS.API.csproj"
    Write-Host ""
    exit 1
}

Write-Host ""

# Test 2: Check for listening port
Write-Host "🔍 Test 2: Port 5000 Listener" -ForegroundColor Yellow
try {
    $listening = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
    if ($listening) {
        Write-Host "  ✅ Port 5000 is in use by a process" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Port 5000 may not be in use" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ⚠️  Could not check port status" -ForegroundColor Yellow
}

Write-Host ""

# Test 3: CORS Configuration
Write-Host "🔍 Test 3: CORS Configuration" -ForegroundColor Yellow
$appsettingsPath = "backend/src/EWMS.API/appsettings.Development.json"

if (Test-Path $appsettingsPath) {
    $config = Get-Content $appsettingsPath | ConvertFrom-Json
    if ($config.Cors.AllowedOrigins) {
        Write-Host "  ✅ CORS configured for:" -ForegroundColor Green
        $config.Cors.AllowedOrigins | ForEach-Object {
            Write-Host "     - $_"
        }
    } else {
        Write-Host "  ⚠️  CORS origins not configured" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠️  appsettings.Development.json not found" -ForegroundColor Yellow
}

Write-Host ""

# Test 4: Frontend Environment
Write-Host "🔍 Test 4: Frontend Environment" -ForegroundColor Yellow
$envPath = "frontend/.env.local"

if (Test-Path $envPath) {
    $env_content = Get-Content $envPath | Select-String "NEXT_PUBLIC_API_BASE_URL"
    if ($env_content) {
        Write-Host "  ✅ NEXT_PUBLIC_API_BASE_URL configured:" -ForegroundColor Green
        Write-Host "     $env_content"
    } else {
        Write-Host "  ⚠️  NEXT_PUBLIC_API_BASE_URL not set in .env.local" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ℹ️  .env.local not found (using defaults)" -ForegroundColor Cyan
}

Write-Host ""

# Test 5: API Endpoints
Write-Host "🔍 Test 5: API Endpoints" -ForegroundColor Yellow

$endpoints = @(
    @{ Name = "Health Check"; Method = "GET"; Path = "/health" },
    @{ Name = "Auth Ping"; Method = "GET"; Path = "/api/v1/auth/me"; RequiresAuth = $true }
)

foreach ($endpoint in $endpoints) {
    $url = if ($endpoint.Path.StartsWith("/api")) { "$backendUrl$($endpoint.Path)" } else { "$backendUrl$($endpoint.Path)" }
    Write-Host "  Testing: $($endpoint.Method) $($endpoint.Path)"
    
    try {
        $params = @{
            Uri = $url
            Method = $endpoint.Method
            UseBasicParsing = $true
            TimeoutSec = 5
        }
        
        $response = Invoke-WebRequest @params
        Write-Host "    ✅ Responded with HTTP $($response.StatusCode)" -ForegroundColor Green
    } catch {
        $statusCode = $_.Exception.Response.StatusCode
        if ($statusCode -eq "Unauthorized") {
            Write-Host "    ℹ️  Responded with HTTP 401 (auth required - expected)" -ForegroundColor Cyan
        } elseif ($statusCode) {
            Write-Host "    ⚠️  Responded with HTTP $statusCode" -ForegroundColor Yellow
        } else {
            Write-Host "    ❌ No response: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. 🌐 Start the Backend (if not running):"
Write-Host "   cd backend"
Write-Host "   dotnet run --project src/EWMS.API/EWMS.API.csproj"
Write-Host ""
Write-Host "2. 📦 Start the Frontend (in a new terminal):"
Write-Host "   cd frontend"
Write-Host "   npm run dev"
Write-Host ""
Write-Host "3. 🧪 Test GPS Tracking:"
Write-Host "   a. Open http://localhost:3000 in your browser"
Write-Host "   b. Log in to EWMS"
Write-Host "   c. Open DevTools (F12) → Console"
Write-Host "   d. Look for [LocationTracker] and [API] messages"
Write-Host "   e. Click 'Check In' and verify tracking starts"
Write-Host ""
Write-Host "4. 📋 If issues persist:"
Write-Host "   - Check TROUBLESHOOTING_GPS.md for detailed diagnostics"
Write-Host "   - Verify NEXT_PUBLIC_API_BASE_URL in frontend environment"
Write-Host "   - Clear browser storage and try again"
Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
