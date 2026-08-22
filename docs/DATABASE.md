# Database setup

## Recommended: EF Core migrations (source of truth)

The C# entity configurations under
`backend/src/EWMS.Persistence/Configurations` are the source of truth for the
schema. Generate and apply migrations from the `backend` folder:

```bash
cd backend
dotnet tool install --global dotnet-ef   # once, if not already installed
dotnet ef migrations add InitialCreate --project src/EWMS.Persistence --startup-project src/EWMS.API
dotnet ef database update --project src/EWMS.Persistence --startup-project src/EWMS.API
```

This creates every table (Identity + Organization + Employee + Attendance +
GPS schemas) in the database referenced by `ConnectionStrings:DefaultConnection`
in `src/EWMS.API/appsettings.json` (or `appsettings.Production.json`).

On application startup, `DbSeeder.SeedAsync` also runs `context.Database
.MigrateAsync()` automatically and seeds:
- Roles: `Admin`, `HR`, `Manager`, `Employee`
- Three departments (Administration, IT, Field Operations) with one
  designation each
- An admin user can be seeded when `ADMIN_USERNAME`, `ADMIN_EMAIL`, and
  `ADMIN_PASSWORD` are supplied through environment variables —
  change this password immediately after first login in any real deployment.

Set `"Database": { "AutoMigrateAndSeed": false }` in `appsettings.json` to
disable auto-migration on startup (e.g. in production, where a DBA runs
migrations as a separate, reviewed step).

## Alternative: manual SQL scripts

`database/scripts/*.sql` mirror the same schema for environments where a DBA
must review and run DDL by hand rather than letting the application run
migrations. Run them in numeric order (00 → 08) against a SQL Server 2022
instance. See `database/scripts/README.md`.

If you use the manual scripts, skip EF Core's auto-migrate
(`AutoMigrateAndSeed: false`) so the app doesn't try to reapply migrations
against a schema it didn't create itself — you'll still get role/department
seeding from `DbSeeder`, since that only inserts rows, not tables.

## Connection string

```
Server=<host>;Database=EWMS_Prod;User Id=<user>;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true
```

For Windows-integrated auth instead: `Trusted_Connection=True` in place of
`User Id`/`Password`.
