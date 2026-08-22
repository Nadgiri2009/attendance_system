# What's in this build — and what isn't

The original brief described a system spanning 20+ modules (GPS, geofencing,
task/workflow engines, survey builder, GIS, asset & inventory management,
payroll integration, and more) across dozens of dashboards. That is a
multi-month, multi-team engineering effort — generating all of it as working
code in one pass was not realistic, and doing so would have meant producing
mostly hollow, non-functional boilerplate.

This build instead delivers a **real, working core** on the requested stack
(Next.js + ASP.NET Core Web API + SQL Server, no Docker/nginx), with the
architecture, conventions, and patterns set up so every remaining module can
be added the same way.

## Fully implemented (real, working code)

- **Auth**: ASP.NET Core Identity, JWT access + refresh tokens, role-based
  authorization (Admin / HR / Manager / Employee), login/register/refresh
  endpoints, protected Next.js routes with automatic token refresh.
- **Employee Management**: full CRUD (create/update/soft-delete/list/detail),
  department & designation relationships, reporting manager hierarchy,
  search + pagination, Next.js list/detail/create pages.
- **Attendance & GPS**: check-in/check-out with GPS coordinates, mock-location
  flag capture, per-day uniqueness, attendance history with filters, a
  Leaflet-based map on the check-in page, dashboard summary widgets.
- **Clean Architecture** wiring: Domain / Application (CQRS via MediatR,
  FluentValidation, AutoMapper) / Infrastructure / Persistence (EF Core,
  SQL Server, Identity) / API, with global exception handling, Serilog
  logging, health checks, and Swagger.
- **Database**: EF Core entity configurations (source of truth) plus a
  matching set of reviewable `.sql` DDL scripts, schemas, indexes, views,
  and stored procedures for the implemented modules.
- **Tests**: FluentValidation unit tests for the Employee and Attendance
  command validators; an integration test project wired to
  `WebApplicationFactory` with notes on swapping in a test database provider.
- **Deployment**: IIS-based deployment guide (no Docker/nginx), since that's
  what you asked for.

## Scaffolded but not built out

Departments read-only listing is implemented; Designations, Leave, Payroll,
Asset, Inventory, Task/Workflow, Survey, GIS, Notification, and the various
role-specific dashboards from the original brief are **not** included. Adding
one follows the same recipe every existing module uses:

1. Domain entity in `EWMS.Domain/Entities`
2. EF configuration in `EWMS.Persistence/Configurations`
3. CQRS commands/queries + validators in `EWMS.Application/<Module>`
4. Controller in `EWMS.API/Controllers`
5. Next.js page(s) under `frontend/app/(dashboard)/<module>`

See `ARCHITECTURE.md` for the conventions to follow.
