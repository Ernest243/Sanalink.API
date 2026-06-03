# Sanalink API — Architecture Document

## Overview

Sanalink is a RESTful Electronic Health Record (EHR) backend built with **ASP.NET Core 9**. It manages the full clinical workflow of a healthcare facility — patient registration, appointments, clinical encounters, prescriptions, lab orders, pharmacy dispensing, and audit logging — secured with JWT-based authentication and role-based access control.

The system is designed for deployment in **French-speaking African healthcare contexts** (terminology, gender codes, and analytics terminology reflect this), with an architecture that aligns with international health informatics standards: **FHIR R4 (HL7)**, **ICD-10 / CIM-10**, **ISO/IEC 27001**, and **DHIS2**.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9 Web API |
| ORM / Database | Entity Framework Core 9 + PostgreSQL (Npgsql) |
| Identity | ASP.NET Core Identity |
| Authentication | JWT Bearer — 4-hour expiry, HMAC-SHA256 signing |
| API Documentation | Swagger / OpenAPI (Swashbuckle) |
| Logging | Serilog — console, rolling file, optional BetterStack sink |
| Feature Flags | Microsoft.FeatureManagement.AspNetCore |
| FHIR R4 | Firely SDK (`Hl7.Fhir.R4`) |

---

## Project Structure

Single-project solution — all code lives in the root of `Sanalink.API.csproj`. There is no separate test project or layered solution file.

```
Sanalink.API/
├── Controllers/          # HTTP layer — thin, delegates to Services
│   └── Fhir/             # FHIR R4 endpoints (gated by FHIR feature flag)
├── Data/                 # AppDbContext (IdentityDbContext<ApplicationUser>)
├── Dtos/                 # Input and output data transfer objects
├── Infrastructure/       # Cross-cutting concerns (FeatureFlags constants)
├── Middleware/           # AuditLoggingMiddleware, TokenRevocationMiddleware
├── Migrations/           # EF Core migration history
├── Models/               # Domain entities
├── Services/             # Business logic interfaces + implementations
│   └── Fhir/             # FhirMappingService (internal → FHIR R4 resources)
├── appsettings.json      # Base configuration (all feature flags default false)
├── appsettings.Development.json
├── appsettings.Production.json
└── Program.cs            # Startup — DI, middleware pipeline, feature-flag wiring
```

---

## Request Pipeline

```
HTTP Request
  │
  ├─ [Middleware] CORS
  ├─ [Middleware] UseAuthentication         — JWT token validated, claims populated
  ├─ [Middleware] AuditLoggingMiddleware    — buffers req/res, writes AuditLog row after every request
  ├─ [Middleware] TokenRevocationMiddleware — (ISO27001_TokenRevocation flag) rejects revoked JWTs
  ├─ [Middleware] UseRateLimiter            — sliding-window policy on /auth/login
  ├─ [Middleware] UseAuthorization          — role/policy checks
  │
  └─ Controller → Service → AppDbContext → PostgreSQL
```

Controllers are thin: they validate the HTTP contract and delegate all logic to scoped Services. The only exception is `AppointmentController` and `PatientController`, which query `AppDbContext` directly because their operations are simple aggregations with no reusable business logic.

---

## Domain Model

```
Facility
  └── ApplicationUser (staff, IdentityUser)
        └── FacilityId FK

Patient
  └── FacilityId FK

Appointment
  ├── PatientId FK
  └── DoctorId FK → ApplicationUser

Encounter                        ← central clinical record
  ├── PatientId FK
  ├── DoctorId  FK → ApplicationUser
  ├── NurseId   FK → ApplicationUser (optional)
  └── Diagnoses → EncounterDiagnosis[]  (ICD-10, enabled by ICD10 flag)

Prescription
  ├── PatientId FK
  └── DoctorId  FK → ApplicationUser

PharmacyDispense
  ├── PrescriptionId FK
  ├── PatientId      FK
  └── DispensedById  FK → ApplicationUser

LabOrder
  ├── EncounterId FK
  ├── PatientId   FK
  └── DoctorId    FK → ApplicationUser

Note
  ├── PatientId FK
  └── DoctorId  FK → ApplicationUser

AuditLog
  └── UserId FK → ApplicationUser (nullable)

— Compliance tables (always migrated; usage controlled by feature flags) —
EncounterDiagnosis   ICD-10 coded diagnoses per encounter
TokenBlacklist       Revoked JWT entries (jti + expiry)
Dhis2Mapping         Local metric → DHIS2 Data Element + Org Unit mapping
```

### Key model invariants

- All `DateTime` columns are stored and read as **UTC** via a global EF value converter in `AppDbContext.OnModelCreating`.
- `Encounter.Status` follows a strict linear state machine enforced in `EncounterService`: `Open → InProgress → Closed`. Any other transition is rejected.
- `Encounter.EncounterNumber` is auto-generated as `ENC-{yyyyMMdd}-{seq:D3}` and has a unique index.
- `Patient` is always scoped to a `Facility`. Staff queries are also scoped by the caller's `facilityId` JWT claim when present.

---

## Authentication & Authorization

**JWT token structure** (claims included in every token):

| Claim | Value |
|---|---|
| `sub` | `ApplicationUser.Id` |
| `jti` | `Guid.NewGuid()` — required for token revocation |
| `email` | User email |
| `firstName` / `lastName` | Display name |
| `role` / `ClaimTypes.Role` | Assigned Identity role |
| `facilityId` | `ApplicationUser.FacilityId` (empty string if null) |

**Roles** (seeded at startup):

`Admin` · `Doctor` · `Nurse` · `Accueil` · `Caisse` · `DAF` · `LabTech` · `Pharmacist`

**Default admin**: `admin@sanalink.com` / `Admin@123456` — change in production.

Token expiry: **4 hours**. Tokens cannot be refreshed; the user must log in again (or immediately via `POST /api/v1/auth/logout` when the revocation flag is on).

---

## Feature Flag System

All compliance-related features ship behind **Microsoft.FeatureManagement** flags. Every flag defaults to `false` — enabling one is a single config change with no code deployment required. To roll back, set the flag back to `false`.

### Flag reference

| Flag key | Default | What it enables |
|---|---|---|
| `ISO27001_Https` | `false` | `UseHttpsRedirection` + `UseHsts` (HSTS skipped in Development) |
| `ISO27001_SwaggerGate` | `false` | Hides Swagger UI in non-Development environments |
| `ISO27001_RateLimiting` | `false` | 10 req/min sliding window on `POST /api/v1/auth/login` |
| `ISO27001_AccountLockout` | `false` | 5 failed login attempts → 15-minute lockout |
| `ISO27001_TokenRevocation` | `false` | `TokenRevocationMiddleware` active; `POST /auth/logout` writes to `TokenBlacklists` |
| `ISO27001_DataRetention` | `false` | Nightly background job purges audit logs older than `Compliance:DataRetentionDays` (default: 365) |
| `ICD10` | `false` | Structured `EncounterDiagnosis` rows persisted and returned in encounter DTOs |
| `FHIR` | `false` | `/fhir/r4/metadata`, `/fhir/r4/Patient`, `/fhir/r4/Encounter` endpoints active |
| `DHIS2` | `false` | Monthly background export to DHIS2 + manual `POST /api/v1/dhis2/export` |

### How flags are evaluated

- **Startup-time decisions** (lockout options, rate-limit permit count, background service registration): read directly from `IConfiguration` in `Program.cs` before `builder.Build()`, because `IFeatureManager` is not available at that point.
- **Runtime decisions** (inside services and controllers): injected `IFeatureManager` with `await _features.IsEnabledAsync("FlagName")`.
- **Whole controllers** gated by flag: `[FeatureGate(FeatureFlags.FHIR)]` / `[FeatureGate(FeatureFlags.DHIS2)]` — returns HTTP 404 when the flag is off.

### Enabling flags

In `appsettings.json` (or via environment variable `FeatureManagement__ISO27001_Https=true`):

```json
"FeatureManagement": {
  "ISO27001_Https":           false,
  "ISO27001_SwaggerGate":     false,
  "ISO27001_RateLimiting":    false,
  "ISO27001_AccountLockout":  false,
  "ISO27001_TokenRevocation": false,
  "ISO27001_DataRetention":   false,
  "ICD10":                    false,
  "FHIR":                     false,
  "DHIS2":                    false
}
```

---

## Compliance Alignment

### FHIR R4 (HL7)

**Status: Implemented behind `FHIR` flag.**

The API exposes a parallel FHIR R4 façade at `/fhir/r4/` using the **Firely SDK** (`Hl7.Fhir.R4`). Responses use `Content-Type: application/fhir+json`.

**Implemented endpoints:**

| FHIR endpoint | Maps to |
|---|---|
| `GET /fhir/r4/metadata` | `CapabilityStatement` listing supported resources |
| `GET /fhir/r4/Patient/{id}` | Single patient by internal ID |
| `GET /fhir/r4/Patient?family=&given=` | Patient search — returns `Bundle` of type `searchset` |
| `GET /fhir/r4/Encounter/{id}` | Single encounter with ICD-10 diagnoses as `ReasonCode` |
| `GET /fhir/r4/Encounter?subject=Patient/{id}&status=` | Encounter search — returns `Bundle` |

**Internal model → FHIR R4 resource mapping:**

| Internal model | FHIR R4 resource |
|---|---|
| `Patient` | `Patient` — name, gender, birthDate, telecom |
| `Encounter` | `Encounter` — status, class (AMB), subject, participant, period, reasonCode |
| `Prescription` | `MedicationRequest` *(roadmap)* |
| `LabOrder` | `ServiceRequest` *(roadmap)* |
| `PharmacyDispense` | `MedicationDispense` *(roadmap)* |
| `Appointment` | `Appointment` *(roadmap)* |
| `Note` | `DocumentReference` *(roadmap)* |
| `Facility` | `Organization` *(roadmap)* |
| `ApplicationUser` | `Practitioner` *(roadmap)* |

**Gender mapping** (internal string → FHIR `AdministrativeGender`):

| Internal value | FHIR code |
|---|---|
| `male` / `homme` / `m` | `male` |
| `female` / `femme` / `f` | `female` |
| any other | `unknown` |

**Encounter status mapping:**

| Internal status | FHIR `EncounterStatus` |
|---|---|
| `Open` | `planned` |
| `InProgress` | `in-progress` |
| `Closed` | `finished` |

---

### ICD-10 / CIM-10

**Status: Implemented behind `ICD10` flag.**

When the `ICD10` flag is enabled, the `Encounter` entity supports structured, multi-coded diagnoses via the `EncounterDiagnoses` table in place of the legacy free-text `Diagnosis` field. The free-text field is preserved for backward compatibility.

**`EncounterDiagnosis` schema:**

| Column | Description |
|---|---|
| `ICD10Code` | ICD-10 / CIM-10 code, e.g. `J18.9` |
| `ICD10Description` | Human-readable label, e.g. `Pneumonie, sans précision` |
| `DiagnosisType` | `Primary` · `Secondary` · `Comorbidity` |
| `Rank` | Ordering within the encounter (1 = most significant) |

**API behavior:**
- `EncounterCreateDto` and `EncounterUpdateDto` accept an optional `Diagnoses[]` list.
- `EncounterReadDto` returns both `Diagnosis` (free-text, always present) and `Diagnoses[]` (ICD-10 list, present only when flag is on).
- In FHIR responses, coded diagnoses appear as `Encounter.reasonCode` with system `http://hl7.org/fhir/sid/icd-10`.

**Gap / roadmap:** Code validation against a full ICD-10 reference table (autocomplete, typo prevention) is not yet implemented. The API currently trusts the caller to supply valid codes.

---

### ISO/IEC 27001

**Status: Implemented behind individual `ISO27001_*` flags.**

The following controls from ISO/IEC 27001 Annex A are addressed:

#### A.9 — Access Control

| Control | Implementation |
|---|---|
| A.9.4.2 — Secure log-on | JWT Bearer authentication; `IsActive` check on every login |
| A.9.4.3 — Password management | ASP.NET Identity enforces digit + minimum length requirements |
| A.9.1.1 — Access control policy | Eight distinct roles with per-endpoint `[Authorize(Roles="...")]` |
| A.9.4.5 — Access control (lockout) | `ISO27001_AccountLockout` flag — 5 attempts → 15-min lockout |
| A.9.2.6 — Removal of access rights | `IsActive = false` immediately blocks login without deleting the account |
| A.9.4.4 — Token revocation | `ISO27001_TokenRevocation` flag — `POST /auth/logout` blacklists the `jti` claim; `TokenRevocationMiddleware` rejects blacklisted tokens on every subsequent request |

#### A.12 — Operations Security

| Control | Implementation |
|---|---|
| A.12.4.1 — Event logging | `AuditLoggingMiddleware` captures every HTTP request: userId, role, facilityId, action, resource, resourceId, endpoint, old/new values (sensitive fields masked), status code, IP address, user agent, timestamp |
| A.12.4.2 — Protection of log information | Auth endpoints (`/auth/login`, `/auth/register-staff`) are excluded from body capture; `SensitiveDataMasker` scrubs `password`, `token`, `key`, etc. from stored bodies |
| A.12.4.3 — Administrator and operator logs | All write actions by any role (including Admin) are captured in `AuditLogs` |
| A.12.1.3 — Capacity management | `ISO27001_DataRetention` flag — nightly job purges audit logs older than `Compliance:DataRetentionDays` (default 365) and expired token blacklist entries |

#### A.14 — System Acquisition, Development and Maintenance

| Control | Implementation |
|---|---|
| A.14.1.2 — Securing app services | `ISO27001_Https` flag — `UseHttpsRedirection` + `UseHsts` in production |
| A.14.2.5 — Secure development principles | `ISO27001_SwaggerGate` flag — Swagger UI restricted to Development environment |
| A.14.1.3 — Rate limiting | `ISO27001_RateLimiting` flag — sliding-window rate limiting on login endpoint |

#### Multi-tenancy isolation (data boundary control)

All patient and staff queries are scoped to the caller's `facilityId` JWT claim when present. An Admin without a facility assignment sees all records across facilities.

---

### DHIS2

**Status: Implemented behind `DHIS2` flag.**

The integration exports aggregate health metrics from Sanalink to a DHIS2 instance using the **DHIS2 Data Value Sets API** (`POST /api/dataValueSets`).

**Exported metrics (current):**

| Metric name | Description |
|---|---|
| `NewPatientRegistrations` | Patients created within the period |
| `TotalAppointments` | Appointments booked within the period |
| `CompletedEncounters` | Encounters closed within the period |
| `LabOrders` | Lab orders placed within the period |

**Configuration** (`appsettings.json`):

```json
"DHIS2": {
  "BaseUrl":  "https://your-dhis2-instance.org",
  "Username": "...",
  "Password": "...",
  "DataSet":  "..."
}
```

**Mapping table (`Dhis2Mappings`):**

Each row maps a `MetricName` to a DHIS2 `DataElementUid`, an optional `FacilityId`, and its corresponding DHIS2 `OrgUnitUid`. Multiple org units are handled by grouping mappings and POSTing one `dataValueSet` payload per org unit.

**Trigger modes:**
- **Automatic**: `Dhis2ExportBackgroundService` fires on the 1st of each month at 03:00 UTC, exporting the previous calendar month.
- **Manual**: `POST /api/v1/dhis2/export?period=202601` (Admin/DAF roles only) — useful for backfills or re-exports.

**Period format**: `yyyyMM` (e.g., `202601` for January 2026), matching DHIS2's monthly period convention.

**Gap / roadmap:** Adding new metrics requires inserting rows into `Dhis2Mappings` and adding the corresponding aggregate query to `Dhis2ExportService.ExportAsync`. Daily and weekly period types are defined in the mapping model but not yet handled by the export logic.

---

## Audit Log

Every HTTP request is intercepted by `AuditLoggingMiddleware` regardless of outcome. The following is stored per request:

| Field | Source |
|---|---|
| `UserId` | JWT `sub` claim |
| `UserEmail` | JWT `email` claim |
| `UserRole` | JWT `role` claim |
| `FacilityId` | JWT `facilityId` claim |
| `Action` | `POST→CREATE`, `PUT/PATCH→UPDATE`, `DELETE→DELETE`, `GET→VIEW` |
| `Resource` | 3rd URL segment (`/api/v1/{Resource}/...`) |
| `ResourceId` | 4th URL segment if numeric |
| `Endpoint` | Full request path |
| `OldValue` | Request body (masked) for UPDATE/DELETE |
| `NewValue` | Response body (masked) for CREATE/UPDATE |
| `StatusCode` | HTTP response status |
| `IpAddress` | `RemoteIpAddress` |
| `UserAgent` | Truncated to 500 chars |
| `Timestamp` | `DateTime.UtcNow` |

Sensitive JSON keys (`password`, `passwordHash`, `token`, `accessToken`, `refreshToken`, `secret`, `key`, `jwt`, `authorization`) are replaced with `"***"` by `SensitiveDataMasker` before storage. Auth endpoints (`/auth/login`, `/auth/register-staff`) skip body capture entirely.

---

## Analytics Endpoints

These endpoints feed dashboards and, when the DHIS2 flag is enabled, flow into DHIS2 exports.

| Endpoint | Description |
|---|---|
| `GET /api/v1/appointment/analytics` | Totals: appointments by status, patient count, prescription count |
| `GET /api/v1/appointment/appointments-per-day` | 10-day appointment time series |
| `GET /api/v1/patient/registrations?days=7` | Daily patient registration counts for N days |
| `GET /api/v1/patient/recent` | Count of patients registered in the last 7 days |
| `GET /api/v1/patient/gender-distribution` | Male / female / other breakdown |
| `GET /api/v1/patient/per-facility` | Patient count per facility |
| `GET /api/v1/patient/natality?days=30` | Newborn registrations (DateOfBirth within last 30 days) |
| `GET /api/v1/auth/active-staff-count` | Active doctor and nurse headcount |

---

## Deployment

**Development**: Run locally with `dotnet run`. Swagger UI at `http://localhost:5189/swagger`. Apply migrations manually with `dotnet ef database update`.

**Production**: Docker image built from `Dockerfile` (multi-stage, ASP.NET 9 runtime). EF migrations are applied automatically on startup via `db.Database.Migrate()`. Swagger is disabled when `ISO27001_SwaggerGate: true`. CORS allows `*.vercel.app` origins.

**Environment variables** (production secrets — never commit values):

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | JWT signing key (min. 32 bytes, base64) |
| `BetterStack__SourceToken` | BetterStack log ingestion token |
| `DHIS2__BaseUrl` / `__Username` / `__Password` | DHIS2 push credentials |
| `FeatureManagement__<FlagName>` | Override any feature flag per-environment |
