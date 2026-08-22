# Integration Tests

These tests spin up the full API host via `WebApplicationFactory<Program>`.
Because the API is wired to SQL Server + ASP.NET Identity in `Program.cs`,
running these tests requires either:

1. A reachable SQL Server instance matching `ConnectionStrings:DefaultConnection`, or
2. Swapping `AddPersistence` for an in-memory / SQLite provider in a custom
   `WebApplicationFactory` fixture for CI (recommended — see EF Core docs on
   "Test with InMemory" and replace the `DbContextOptions` in a
   `ConfigureWebHost` override before running Application/Attendance/Employees
   controller tests here).

Add controller-level tests (Employees CRUD, Attendance check-in/out, Auth
login/refresh) once a fixture with a swapped-out database provider is in
place, following the same CQRS handlers already unit-tested in
`EWMS.UnitTests`.
