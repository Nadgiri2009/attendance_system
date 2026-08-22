# Bug fixes & feature completion — Employee & Attendance modules

This documents a review pass over the Employee Management and Attendance
Management modules: root-cause analysis of the "forms submit but nothing
saves" issue, the missing CRUD/validation surface, and every file touched.
No architecture, project structure, or naming conventions were changed —
all fixes extend the existing Clean Architecture / CQRS / MediatR /
FluentValidation pattern already in place.

## Root cause: "Employee/Attendance forms submit but records aren't saved"

The uploaded project included real Serilog logs from local runs
(`backend/src/EWMS.API/logs/`). They show every `POST /api/v1/employees`
attempt returning `400 application/problem+json` immediately after route
matching — **before** MediatR's `Handling CreateEmployeeCommand` log line
ever appears. That means ASP.NET Core's automatic `[ApiController]` model
validation was rejecting the request during JSON deserialization, so the
request never reached the controller body, FluentValidation, or the
database. Two concrete, compounding causes:

1. **No `JsonStringEnumConverter` registered.** The frontend sends
   `"gender": "Male"` as a JSON string; `System.Text.Json` binds enums as
   numbers by default, so this throws a `JsonException` during binding.
   Same issue would have hit `AttendanceStatus` the moment Attendance CRUD
   was added.
2. **The "Designation ID" field on the Add Employee form was a raw text
   box** requiring the user to manually paste a GUID, with no lookup
   endpoint to find a valid one. An empty or malformed string fails `Guid`
   model binding the same way.

A third, related issue **masked** the above from the user rather than
causing it: the frontend's error handling read
`err.response.data.errors[0]`, which only matches the app's own
`{ success:false, errors:[...] }` shape. ASP.NET Core's automatic 400
instead returns `ValidationProblemDetails.errors` as a **dictionary**
keyed by field name, so `errors[0]` was always `undefined` and every form
silently fell back to a generic "Could not create employee" message.

A fourth finding, not a save-blocking bug but a real local-dev papercut:
there was no `Properties/launchSettings.json`, so `dotnet run` never bound
an HTTPS endpoint (`Failed to determine the https port for redirect` in
the logs), forcing manual `.env.local` / port hunting.

### Fixes
- `Program.cs`: registered `JsonStringEnumConverter` globally; restored
  config-driven CORS (`Cors:AllowedOrigins`, was hardcoded); made
  `UseHttpsRedirection` conditional on an HTTPS endpoint actually being
  configured.
- Added `backend/src/EWMS.API/Properties/launchSettings.json` with `http`
  (5000) and `https` (5001) profiles.
- Added `GET /api/v1/designations?departmentId=` (`DesignationsController`
  + `GetDesignationsQuery`) and rewired the employee form to a real
  cascading Department → Designation dropdown, removing the GUID text box
  entirely.
- Added `lib/api.ts#getErrorMessage()`, which understands both the app's
  own error shape and ASP.NET's `ValidationProblemDetails` dictionary
  shape, and wired every form (login, employee create/edit, attendance
  create/edit/self-service) to use it.

## Employee Management — validation added

`CreateEmployeeCommandValidator` / `UpdateEmployeeCommandValidator`
(FluentValidation, `MustAsync` against `IApplicationDbContext`):

| Field | Rule |
|---|---|
| Employee Code | required, unique (create only — immutable after creation) |
| First/Last Name | required |
| Email | required, valid format, unique (excludes self on update) |
| Phone | required, exactly 10 digits, unique (excludes self on update) |
| Department | required |
| Designation | required, **must belong to the selected Department** (cross-field async check) |
| Gender | required (`IsInEnum()`) |
| Date of Birth | required, cannot be a future date, must be before Date of Joining |
| Date of Joining | required |

`UpdateEmployeeCommand` was extended to include `DateOfBirth`/`DateOfJoining`
(previously immutable after creation with no way to correct a data-entry
error) and `EmployeeCode` is intentionally still not editable — same policy
as before, now enforced by disabling the field in the UI as well.

Uniqueness/cross-field checks live only in the validators (single source of
truth); the command handlers no longer duplicate a subset of that logic.

## Attendance Management — validation added

`CreateAttendanceCommandValidator` / `UpdateAttendanceCommandValidator`:

| Field | Rule |
|---|---|
| Employee | required, must exist |
| Attendance Date | required, cannot be a future date |
| Check-In | required |
| Check-Out | when provided, must be greater than Check-In |
| Duplicate | one record per employee per date (validator + existing unique DB index as a second line of defense) |
| Status | required (`IsInEnum()`) |
| Remarks | optional, max 1000 chars |

`Remarks` existed on the `AttendanceRecord` entity/table but was never
returned by `AttendanceDto` — fixed, and the DTO projection (previously
hand-copied in two query handlers) is now a single reusable
`AttendanceDto.Projection` expression used everywhere.

## CRUD completed

**Employee** — Create, Update, Delete (soft), GetById, List already existed;
added: **Search + Sort + Pagination** together in `GetEmployeesQuery`
(explicit sortable-column allow-list, not dynamic LINQ), and the
**Edit page** (`/employees/[id]/edit`) that was missing entirely — the API
endpoint worked, nothing in the UI ever called it.

**Attendance** — only employee self-service Check-In/Check-Out existed.
Added full CRUD for HR/Admin/Manager: `CreateAttendanceCommand` (manual
entry/back-fill, distinct from the self-service flow),
`UpdateAttendanceCommand`, `DeleteAttendanceCommand` (soft delete, reusing
the existing `AttendanceRecord.IsDeleted` + global query filter),
`GetAttendanceByIdQuery`, and Search/Sort/Status-filter/Pagination in
`GetAttendanceHistoryQuery`. New `AttendanceController` routes:
`GET/POST/PUT/DELETE /api/v1/attendance` and `/attendance/{id}`, alongside
the unchanged `check-in`/`check-out`/`today`/`history` routes.

Frontend: a new **Attendance management panel**
(`components/AttendanceManagement.tsx`, shown on the Attendance page for
Admin/HR/Manager roles) provides list/search/status-filter/sort/paginate/
create/edit/delete, reusing the same `DataTable`/`Pagination` components as
the Employees list rather than building a parallel table implementation.

## Files modified

**Backend**
- `EWMS.API/Program.cs`
- `EWMS.API/Properties/launchSettings.json` (new)
- `EWMS.API/Controllers/AttendanceController.cs`
- `EWMS.API/Controllers/DesignationsController.cs` (new)
- `EWMS.Application/Employees/Commands/CreateEmployee/CreateEmployeeCommand.cs`
- `EWMS.Application/Employees/Commands/CreateEmployee/CreateEmployeeCommandValidator.cs`
- `EWMS.Application/Employees/Commands/UpdateEmployee/UpdateEmployeeCommand.cs`
- `EWMS.Application/Employees/Commands/UpdateEmployee/UpdateEmployeeCommandValidator.cs`
- `EWMS.Application/Employees/Queries/GetEmployees/GetEmployeesQuery.cs`
- `EWMS.Application/Attendance/AttendanceDto.cs`
- `EWMS.Application/Attendance/Queries/GetTodayStatus/GetTodayStatusQuery.cs`
- `EWMS.Application/Attendance/Queries/GetAttendanceHistory/GetAttendanceHistoryQuery.cs`
- `EWMS.Application/Attendance/Queries/GetAttendanceById/GetAttendanceByIdQuery.cs` (new)
- `EWMS.Application/Attendance/Commands/CreateAttendance/*` (new)
- `EWMS.Application/Attendance/Commands/UpdateAttendance/*` (new)
- `EWMS.Application/Attendance/Commands/DeleteAttendance/DeleteAttendanceCommand.cs` (new)
- `EWMS.Application/Designations/Queries/GetDesignations/GetDesignationsQuery.cs` (new)

**Frontend**
- `lib/api.ts`, `lib/types.ts`, `.env.local.example`
- `app/login/page.tsx`
- `app/(dashboard)/employees/page.tsx`
- `app/(dashboard)/employees/new/page.tsx`
- `app/(dashboard)/employees/[id]/page.tsx`
- `app/(dashboard)/employees/[id]/edit/page.tsx` (new)
- `app/(dashboard)/attendance/page.tsx`
- `components/EmployeeForm.tsx` (new, shared by New/Edit)
- `components/AttendanceForm.tsx` (new)
- `components/AttendanceManagement.tsx` (new)
- `components/DataTable.tsx` (extended, backward-compatible: optional sort props)
- `components/Pagination.tsx` (new, shared by Employees/Attendance)

## Remaining known issues

- **Refresh tokens are in-memory** (`EWMS.Infrastructure/Services/TokenService.cs`)
  — fine for a single dev instance, but won't survive an API restart or work
  across multiple instances. The `RefreshTokens` table/entity already exists
  and migrated; swapping the in-memory dictionary for it is the natural next
  step, intentionally left alone here since it wasn't part of the
  Employee/Attendance scope and touches Auth.
- **Reporting Manager** has no dedicated UI field on the Employee form (it's
  shown read-only on the detail page). The Edit page passes the existing
  value straight through so saving doesn't null it out, but assigning/
  changing a manager still requires a direct API call.
- **Designation management** (creating/editing designations themselves) is
  still read-only from the frontend's perspective — only the lookup used by
  the Employee form was added, matching the Departments module's existing
  scope. Full Designation CRUD wasn't requested and mirrors how Departments
  was already read-only.
- The Attendance date shown on the manual entry form is derived from the
  Check-In timestamp's date rather than picked independently, to prevent a
  mismatched Check-In-vs-Attendance-Date data entry error; if a genuinely
  independent Attendance Date is needed (e.g. logging a night shift that
  starts before midnight), that would need a small follow-up.
- This was reviewed carefully but not run against a live .NET/SQL Server
  environment in this pass — recommend `dotnet build` and a smoke test of
  Create/Edit Employee and the new Attendance management panel before
  relying on it.
