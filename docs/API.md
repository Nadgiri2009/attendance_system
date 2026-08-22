# API reference (core modules)

Base URL: `/api/v1`. All responses: `{ success, data?, message?, errors? }`.
Authenticated endpoints require `Authorization: Bearer <accessToken>`.

## Auth

| Method | Route | Auth | Body |
|---|---|---|---|
| POST | `/auth/login` | none | `{ userNameOrEmail, password }` |
| POST | `/auth/register` | Admin, HR | `{ userName, email, password, employeeId?, roles? }` |
| POST | `/auth/refresh-token` | none | `{ refreshToken }` |

## Employees

| Method | Route | Auth | Notes |
|---|---|---|---|
| GET | `/employees?search=&departmentId=&isActive=&pageNumber=&pageSize=` | any authenticated user | paginated list |
| GET | `/employees/{id}` | any authenticated user | |
| POST | `/employees` | Admin, HR | create |
| PUT | `/employees/{id}` | Admin, HR | update |
| DELETE | `/employees/{id}` | Admin, HR | soft delete (deactivates) |

## Attendance

| Method | Route | Auth | Notes |
|---|---|---|---|
| POST | `/attendance/check-in` | any authenticated user | `{ employeeId, latitude, longitude, accuracyMeters?, isMockLocation, address? }` |
| POST | `/attendance/check-out` | any authenticated user | same shape |
| GET | `/attendance/today/{employeeId}` | any authenticated user | today's record, if any |
| GET | `/attendance/history?employeeId=&fromDate=&toDate=&pageNumber=&pageSize=` | any authenticated user | paginated |

## Departments

| Method | Route | Auth |
|---|---|---|
| GET | `/departments` | any authenticated user |

Full interactive documentation is served at `/swagger` when the API is
running (enabled in both Development and Production in this build — disable
in `Program.cs` for production if you don't want it public).
