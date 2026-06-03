# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Restore dependencies
dotnet restore

# Run the API (dev, http://localhost:5189)
dotnet run

# Build without running
dotnet build

# Apply EF Core migrations (dev only — prod auto-migrates on startup)
dotnet ef database update

# Add a new EF Core migration
dotnet ef migrations add <MigrationName>

# Docker build
docker build -t sanalink-api .
```

There are no automated tests in this project.

## Architecture

Single-project ASP.NET Core 9 Web API (`Sanalink.API.csproj`). All code lives in the root; there is no layered solution with multiple .csproj files.

**Request flow:**
```
HTTP Request
  → AuditLoggingMiddleware (buffers req/res bodies, persists AuditLog after every request)
  → Controller (thin — delegates to Service)
  → Service (business logic, EF Core queries)
  → AppDbContext (PostgreSQL via Npgsql)
```

### Key architectural decisions

**Multi-database setup:** The project references both `Npgsql.EntityFrameworkCore.PostgreSQL` and `Microsoft.EntityFrameworkCore.Sqlite`. Production and development both use PostgreSQL (`DefaultConnection` in `appsettings.json`). SQLite is a leftover/fallback and is not used in the active config.

**Dual-environment config:** `appsettings.Development.json` holds dev credentials. Production connection string and JWT key must be supplied via environment variables or a secrets manager — `appsettings.Production.json` only overrides Serilog levels. EF migrations run automatically on startup in Production only; in Development, run `dotnet ef database update` manually.

**DateTime handling:** `AppDbContext.OnModelCreating` installs a global EF value converter that stores all `DateTime` properties as UTC and reads them back as `DateTimeKind.Utc`. Always use `DateTime.UtcNow` — never `DateTime.Now`.

**Audit logging:** `AuditLoggingMiddleware` intercepts every request, captures request/response bodies (skipping `/auth/login` and `/auth/register-staff`), and writes an `AuditLog` row. `SensitiveDataMasker` scrubs known sensitive JSON keys (`password`, `token`, etc.) before storage.

**JWT claims:** The token includes `sub` (userId), `email`, `firstName`, `lastName`, `role` (both as `ClaimTypes.Role` and a custom `"role"` claim), and `facilityId`. Controllers read `facilityId` via `User.FindFirstValue("facilityId")` to scope queries to the user's facility.

**Facility scoping:** Most staff endpoints filter results by the caller's `facilityId` claim when it is present. Admins without a facility see all records.

**Encounter status workflow:** Encounters follow a strict linear state machine enforced in `EncounterService.UpdateStatusAsync`: `Open → InProgress → Closed`. Any other transition is rejected. Encounter numbers are auto-generated as `ENC-{yyyyMMdd}-{sequence:D3}`.

**Service pattern:** Each domain (Encounter, Prescription, LabOrder, etc.) has an interface (`IXService`) and implementation registered as `AddScoped`. Controllers never query `AppDbContext` directly except in `AuthController`, which uses `UserManager<ApplicationUser>` and `_db` for Identity operations.

### Roles

Seeded at startup by `RoleSeeder`: `Admin`, `Doctor`, `Nurse`, `Accueil`, `Caisse`, `DAF`, `LabTech`, `Pharmacist`.

Default admin: `admin@sanalink.com` / `Admin@123456` (change in production).

### Logging

Serilog writes to console, rolling daily files (`logs/sanalink-.log`, 14-day retention), and optionally to BetterStack when `BetterStack:SourceToken` is configured.

### CORS

Development allows `localhost` origins; production allows `*.vercel.app` origins (`AllowVercel` policy).

### All endpoints are prefixed `/api/v1`

Route template: `[Route("api/v1/[controller]")]` on each controller.
