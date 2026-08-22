# Architecture

## Backend — Clean Architecture

```
EWMS.Domain          Entities, enums, no dependencies on anything else.
EWMS.Application      CQRS (MediatR) commands/queries, DTOs, validators
                       (FluentValidation), AutoMapper profiles, interfaces
                       that Infrastructure/Persistence implement.
EWMS.Infrastructure   JWT token issuance, ASP.NET Identity glue,
                       ICurrentUserService, IDateTimeService.
EWMS.Persistence      EF Core DbContext, entity configurations, DI wiring,
                       DbSeeder (roles, departments, the seeded admin user).
EWMS.API              Controllers, Program.cs composition root, Serilog,
                       Swagger, JWT bearer auth, global exception middleware.
```

Dependencies point inward: API → Persistence/Infrastructure → Application →
Domain. Application never references Persistence or Infrastructure directly —
only through interfaces in `EWMS.Application/Common/Interfaces`.

### Request flow

`Controller` → `Mediator.Send(command/query)` → `ValidationBehaviour` (runs
FluentValidation validators) → `Handler` (talks to `IApplicationDbContext`) →
`Result<T>` returned up the stack → controller maps it to an HTTP response.

### Adding a new module

Follow the Employee or Attendance module as a template:

1. Add the entity to `EWMS.Domain/Entities`, register it on
   `IApplicationDbContext` and `ApplicationDbContext`.
2. Add an `IEntityTypeConfiguration<T>` in `EWMS.Persistence/Configurations`.
3. Create a folder under `EWMS.Application/<ModuleName>` with
   `Commands/<Action>/<Action>Command.cs` (+ handler + validator) and
   `Queries/<Query>/<Query>Query.cs` (+ handler).
4. Add a controller in `EWMS.API/Controllers` inheriting `BaseApiController`.
5. Add a matching `.sql` script under `database/scripts` for DBA review.
6. Add a Next.js route under `frontend/app/(dashboard)/<module>`.

## Frontend — Next.js App Router

```
app/
  login/                  Public login page
  (dashboard)/            Route group behind ProtectedRoute
    layout.tsx             Sidebar + Navbar shell
    dashboard/              Landing page with summary widgets
    employees/              List, detail, create
    attendance/              GPS check-in/out, history, Leaflet map
lib/
  api.ts                  Axios instance: attaches JWT, auto-refreshes on 401
  auth-context.tsx        React context: login/logout, persisted user
  types.ts                Shared DTO shapes matching the API contracts
components/               Sidebar, Navbar, DataTable, StatCard, AttendanceMap
```

Auth tokens are kept in `localStorage` and attached via an Axios request
interceptor; a response interceptor transparently retries once on a 401 after
calling `/auth/refresh-token`. `ProtectedRoute` redirects to `/login` when
there's no authenticated user.

## API conventions

- All endpoints are versioned under `/api/v1/...`.
- Every response is wrapped as `{ success, data?, message?, errors? }`.
- List endpoints return `{ items, pageNumber, totalPages, totalCount,
  hasPreviousPage, hasNextPage }`.
- Auth: `Authorization: Bearer <accessToken>`; call `/api/v1/auth/refresh-token`
  with the stored refresh token when the access token expires.
