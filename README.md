Attendance Mngt System

Employee tracking, GPS-verified attendance, and workforce management,
built on **Next.js + ASP.NET Core Web API + SQL Server** — no Docker, no
nginx.

> **Read `docs/SCOPE.md` first.** This delivers a fully working core (Auth,
> Employee Management, Attendance/GPS) with production-grade architecture —
> not the entire 20-module enterprise spec, which is a multi-month build.
> Every additional module follows the same pattern already established here.

## Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 15 (App Router), TypeScript, Tailwind CSS, Leaflet |
| Backend | ASP.NET Core 9 Web API, Clean Architecture, CQRS (MediatR) |
| Database | SQL Server 2022, EF Core |
| Auth | ASP.NET Core Identity, JWT access + refresh tokens, RBAC |
| Deployment | Windows Server + IIS (see `docs/DEPLOYMENT_IIS.md`) |

## Repository layout

```
backend/
  EWMS.sln
  src/
    EWMS.Domain/          Entities, enums
    EWMS.Application/     CQRS commands/queries, validators, DTOs
    EWMS.Infrastructure/  JWT, Identity glue
    EWMS.Persistence/     EF Core DbContext, configurations, seeding
    EWMS.API/              Controllers, Program.cs, appsettings
  tests/
    EWMS.UnitTests/
    EWMS.IntegrationTests/
frontend/
  app/                    Next.js App Router pages (login, dashboard, employees, attendance)
  components/, lib/
database/
  scripts/                Reviewable SQL DDL matching the EF Core model
docs/
  SCOPE.md                What's built vs. what's scaffolded
  ARCHITECTURE.md         How the layers fit together, how to add a module
  DATABASE.md             EF Core migrations vs. manual SQL scripts
  API.md                  Endpoint reference
  DEPLOYMENT_IIS.md        Windows Server + IIS deployment (no Docker/nginx)
  BUGFIXES.md             Root-cause analysis + fixes from the review pass
  GPS_TRACKING.md         Continuous GPS tracking: design, endpoints, limitations
```

## Local development

### Prerequisites
- .NET 9 SDK
- Node.js 20+
- SQL Server 2022 (Developer Edition is free) — local instance or `localhost\SQLEXPRESS`

### 1. Backend

```bash
cd backend
dotnet restore

# configure your local SQL Server and secrets with environment variables or
# the .NET user-secrets store; do not put credentials in appsettings files

dotnet ef database update --project src/EWMS.Persistence --startup-project src/EWMS.API
dotnet run --project src/EWMS.API
```

The API starts on `https://localhost:5001` (see the generated launch
profile), auto-migrates, and seeds:
- Roles: Admin, HR, Manager, Employee
- 3 departments / 3 designations
- Admin credentials must be supplied through environment variables and must
  never be committed to the repository.

Swagger UI: `https://localhost:5001/swagger`.

### 2. Frontend

```bash
cd frontend
cp .env.local.example .env.local
# edit .env.local if your API isn't on https://localhost:5001

npm install
npm run dev
```

Visit `http://localhost:3000` — you'll be redirected to `/login`.

### 3. Run tests

```bash
cd backend
dotnet test
```

## Production deployment

See `docs/DEPLOYMENT_IIS.md` for the full Windows Server + IIS walkthrough
(ASP.NET Core Module for the API, IIS + URL Rewrite/ARR reverse-proxying to a
Node-hosted Next.js process via NSSM — no Docker or nginx anywhere).

## Security notes before going live

- Change the seeded admin password immediately.
- Set `Jwt:Key` via an environment variable / secret store, never commit a
  real value.
- Set `AclSms` credentials via environment variables such as
  `AclSms__Password`, never commit them to `appsettings*.json`.
- Set `Cors:AllowedOrigins` to your real frontend origin only.
- Consider disabling Swagger in production (`Program.cs`) or putting it
  behind auth if you'd rather not expose it publicly.
- Refresh tokens are currently held in an in-memory dictionary
  (`EWMS.Infrastructure/Services/TokenService.cs`) for simplicity — swap
  this for the `RefreshTokens` table (already modeled and migrated) before
  running with more than one API instance or across restarts.
