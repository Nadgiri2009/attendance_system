# Deployment — Windows Server + IIS (no Docker, no nginx)

This covers hosting both the ASP.NET Core Web API and the Next.js frontend
on a single Windows Server under IIS, plus SQL Server setup.

## 0. Prerequisites on the server

- Windows Server 2019/2022 with IIS role installed
  (`Install-WindowsFeature -Name Web-Server -IncludeManagementTools`)
- [.NET 9 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0)
  — installs the ASP.NET Core Module (ANCM) for IIS. **Restart IIS** after
  installing (`net stop was /y && net start w3svc`).
- [Node.js 20 LTS](https://nodejs.org) (for building/running Next.js)
- [URL Rewrite Module](https://www.iis.net/downloads/microsoft/url-rewrite)
  and [Application Request Routing (ARR)](https://www.iis.net/downloads/microsoft/application-request-routing)
  — used to reverse-proxy the Next.js app through IIS since nginx is out of
  scope. After installing ARR, open IIS Manager → server node → *Application
  Request Routing Cache* → *Server Proxy Settings* → check **Enable proxy**.
- SQL Server 2022 (Developer/Standard/Enterprise) reachable from the server.
- [NSSM](https://nssm.cc/) (Non-Sucking Service Manager) — runs `next start`
  as a native Windows Service, since IIS itself doesn't execute Node.js apps
  directly (that's normally iisnode's job; NSSM + ARR reverse proxy is the
  simpler, more current path for App Router apps).

## 1. Database

1. Copy `database/` to the server (or run scripts from your workstation
   against the server's SQL instance).
2. Run `00_create_database.sql` through `08_seed_data.sql` in order, **or**
   prefer EF Core migrations (see `docs/DATABASE.md`) — run
   `dotnet ef database update` from a machine with the .NET SDK and network
   access to the SQL Server instance, then skip the manual scripts.
3. Create a SQL login for the API with `db_datareader`/`db_datawriter` on
   `EWMS_Prod` (avoid using `sa` in production).

## 2. Backend (ASP.NET Core Web API)

1. On a build machine with the .NET 9 SDK:
   ```bash
   cd backend
   dotnet restore
   dotnet publish src/EWMS.API/EWMS.API.csproj -c Release -o ./publish
   ```
2. Copy the contents of `./publish` to the server, e.g.
   `C:\inetpub\ewms-api\`.
3. Set the production connection string and JWT key as **environment
   variables** on the server (don't ship secrets in `appsettings.Production.json`):
   - In IIS Manager → your API site → *Configuration Editor* →
     `system.webServer/aspNetCore` → `environmentVariables`, add:
     - `ASPNETCORE_ENVIRONMENT` = `Production`
     - `ConnectionStrings__DefaultConnection` = `Server=...;Database=EWMS_Prod;...`
     - `Jwt__Key` = a random 32+ character secret (e.g. `openssl rand -base64 48`)
4. In IIS Manager: right-click **Sites** → *Add Website*.
   - Site name: `EWMS-API`
   - Physical path: `C:\inetpub\ewms-api`
   - Binding: `https`, port `443` (or `5001` if this server also hosts the
     frontend site on 443), with a valid TLS certificate bound.
5. Set the site's Application Pool to **No Managed Code** (ANCM handles the
   .NET runtime itself) — this is set automatically for ASP.NET Core sites
   published with the Hosting Bundle installed.
6. Confirm it's running: browse to `https://your-api-host/swagger` and
   `https://your-api-host/health`.
7. `web.config` is already included in the publish output (generated from
   `EWMS.API/web.config`) — verify `arguments` points at `.\EWMS.API.dll`.

### appsettings.Production.json

Update `backend/src/EWMS.API/appsettings.Production.json` before publishing,
or override everything via the environment variables above (preferred, since
env vars aren't checked into source control):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=EWMS_Prod;User Id=...;Password=...;TrustServerCertificate=True"
  },
  "Jwt": { "Key": "SET_VIA_ENVIRONMENT_VARIABLE" },
  "Cors": { "AllowedOrigins": [ "https://your-frontend-domain.example.com" ] }
}
```

## 3. Frontend (Next.js)

Next.js's App Router needs a running Node process (`next start`) — IIS alone
can't execute it, so IIS is used purely as a reverse proxy in front of it.

1. On a build machine with Node 20:
   ```bash
   cd frontend
   npm install
   # point the build at your real API origin:
   echo NEXT_PUBLIC_API_BASE_URL=https://your-api-host/api/v1 > .env.production
   npm run build
   ```
2. Copy the whole `frontend` folder (including `.next`, `node_modules` or a
   fresh `npm install --production` on the server, `package.json`, and
   `.env.production`) to e.g. `C:\inetpub\ewms-web\`.
3. Install as a Windows Service with NSSM so it survives reboots:
   ```powershell
   nssm install EWMS-Web "C:\Program Files\nodejs\node.exe" "node_modules\next\dist\bin\next start -p 3000"
   nssm set EWMS-Web AppDirectory "C:\inetpub\ewms-web"
   nssm start EWMS-Web
   ```
   This runs the app on `http://localhost:3000` (loopback only).
4. In IIS Manager, create a second site, `EWMS-Web`, bound to `https://` on
   `443` with your public hostname and TLS certificate, physical path can be
   an empty folder (IIS only needs it to host the rewrite rule).
5. Add a `web.config` in that empty folder to reverse-proxy to Node via ARR:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <system.webServer>
       <rewrite>
         <rules>
           <rule name="ReverseProxyToNext" stopProcessing="true">
             <match url="(.*)" />
             <action type="Rewrite" url="http://localhost:3000/{R:1}" />
           </rule>
         </rules>
       </rewrite>
     </system.webServer>
   </configuration>
   ```
6. Browse to `https://your-frontend-domain` and confirm the login page loads
   and can reach the API (check the browser console/network tab for CORS
   errors — see below).

## 4. CORS

The API's `Cors:AllowedOrigins` (in `appsettings.Production.json` or via the
`Cors__AllowedOrigins__0` environment variable) must list the exact frontend
origin (`https://your-frontend-domain.example.com`, no trailing slash) or
browser requests from the Next.js app will be blocked.

## 5. TLS certificates

Bind a real certificate (internal CA or public, e.g. via `certreq` or your
org's certificate management) to both IIS sites — do not run either site over
plain HTTP in production; `app.UseHsts()` and `UseHttpsRedirection()` are
already enabled in `Program.cs`.

## 6. Logs

- API: Serilog writes to `C:\inetpub\ewms-api\logs\ewms-*.log` (rolling
  daily) and to the console (captured by ANCM's stdout log if enabled in
  `web.config`).
- Frontend: NSSM can redirect the Node process's stdout/stderr to a file —
  `nssm set EWMS-Web AppStdout C:\inetpub\ewms-web\logs\web.log`.

## 7. Updating a deployment

1. Stop the NSSM service (`nssm stop EWMS-Web`) and/or recycle the IIS API
   app pool.
2. Replace files with a fresh `dotnet publish` / `npm run build` output.
3. Start everything back up. There's no built-in blue/green or rolling
   deploy here — for zero-downtime you'd add a load balancer in front of two
   parallel deployments, which is out of scope for this guide.
