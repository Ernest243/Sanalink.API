# SanaLink API

SanaLink is a RESTful backend API for a lightweight Electronic Health Record (EHR) system built with ASP.NET Core 9. It allows hospitals and clinics to manage patients, staff, appointments, clinical encounters, prescriptions, lab orders, pharmacy dispensing, and clinical notes — all secured with JWT-based authentication and role-based access control.

---

## Features

- **Authentication** — JWT login for staff and patients, role-based access (Admin, Doctor, Nurse, Patient)
- **Facility Management** — Create and manage healthcare facilities
- **Patient Management** — Register, search, and manage patient records
- **Appointments** — Book, update, cancel, and analyse appointment data
- **Clinical Encounters** — Record and manage patient encounters with status workflow
- **Prescriptions** — Issue and track medication prescriptions
- **Lab Orders** — Create and manage lab test orders per encounter
- **Pharmacy Dispense** — Track medication dispensing linked to prescriptions
- **Clinical Notes** — Record free-text clinical notes per patient
- **Audit Logs** — Automatic logging of all write actions across the system
- **Analytics endpoints** — Appointment trends, patient registrations, and staff counts

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9 Web API |
| Database | PostgreSQL via Entity Framework Core |
| Identity | ASP.NET Core Identity |
| Auth | JWT Bearer tokens (4-hour expiry) |
| API Docs | Swagger / OpenAPI (Swashbuckle) |

---

## Roles & Permissions

| Role | Description |
|---|---|
| **Admin** | Manage facilities, staff, audit logs, cancel appointments |
| **Doctor** | Full access to patients, encounters, prescriptions, lab orders, appointments |
| **Nurse** | Read access to patients; can update encounters, lab orders, and dispenses |
| **Patient** | Read their own profile, encounters, prescriptions, lab orders, and dispenses |

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) (running instance)

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Ernest243/Sanalink.API.git
cd Sanalink.API
```

### 2. Configure the database connection

Edit `appsettings.Development.json` and update the connection string to point to your PostgreSQL instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sanalink_dev_db;Username=your_user;Password=your_password"
  }
}
```

### 3. Restore dependencies

```bash
dotnet restore
```

### 4. Apply database migrations

```bash
dotnet ef database update
```

> On first run, roles (Admin, Doctor, Nurse, Patient) and a default Admin user are seeded automatically.

### 5. Run the API

```bash
dotnet run
```

The API will be available at `http://localhost:5189`.

---

## Default Admin Credentials

A default admin account is seeded on first startup:

| Field | Value |
|---|---|
| Email | `admin@sanalink.com` |
| Password | `Admin@123456` |

> Change this password in a production environment.

---

## Exploring the API

### Swagger UI

Once the API is running, open:

```
http://localhost:5189/swagger
```

Click **Authorize**, enter `Bearer <your_token>` (token obtained from `/api/v1/auth/login`), then explore and test all endpoints directly in the browser.

### Postman Collection

A ready-to-use Postman collection is included at the root of the repository:

```
Sanalink.postman_collection.json
```

**Quick setup:**
1. Import the file into Postman
2. Set the `base_url` collection variable to `http://localhost:5189`
3. Run the **Login** request under the **Auth** folder
4. The test script will automatically save the token to the `token` collection variable
5. All authenticated requests are ready to use

---

## API Endpoint Overview

All endpoints are prefixed with `/api/v1`.

### Auth — `/api/v1/auth`

| Method | Path | Access | Description |
|---|---|---|---|
| POST | `/register` | Public | Register a new patient account |
| POST | `/register-staff` | Admin | Register a Doctor, Nurse, or Admin |
| POST | `/login` | Public | Login and receive a JWT token |
| GET | `/active-staff-count` | Admin/Doctor/Nurse | Count of active doctors and nurses |

### Health — `/api/v1/health`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/` | Public | API health check |

### Facilities — `/api/v1/facility`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/` | Admin | List all facilities |
| GET | `/{id}` | Admin | Get a facility by ID |
| POST | `/` | Admin | Create a new facility |

### Patients — `/api/v1/patient`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/me` | Patient | Get own patient profile |
| GET | `/` | Doctor/Nurse/Admin | List all patients |
| GET | `/{id}` | Doctor/Nurse/Admin | Get patient by ID |
| POST | `/` | Doctor | Create a patient record |
| PUT | `/{id}` | Doctor | Update patient details |
| GET | `/search?query=` | Doctor/Nurse/Admin | Search patients by name |
| GET | `/recent` | Doctor/Nurse/Admin | Count of patients registered in the last 7 days |
| GET | `/registrations/last7days` | Doctor/Nurse/Admin | Daily registration counts for the last 7 days |

### Appointments — `/api/v1/appointment`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/` | Doctor/Nurse/Admin | List all appointments |
| GET | `/{id}` | Doctor/Nurse/Admin | Get appointment by ID |
| POST | `/` | Doctor/Nurse/Admin | Book a new appointment |
| PUT | `/{id}` | Doctor | Update appointment details |
| DELETE | `/{id}` | Doctor/Admin | Cancel an appointment |
| GET | `/appointments-per-day` | Doctor/Nurse/Admin | Appointment counts for the last 10 days |
| GET | `/analytics` | Doctor/Nurse/Admin | Summary stats (totals, statuses, patients, prescriptions) |

### Encounters — `/api/v1/encounter`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/my` | Patient | Get own encounters |
| GET | `/` | Doctor/Nurse/Admin | List all encounters |
| GET | `/{id}` | Doctor/Nurse/Admin | Get encounter by ID |
| GET | `/patient/{patientId}` | Doctor/Nurse/Admin | Get encounters for a patient |
| POST | `/` | Doctor | Create a new encounter |
| PUT | `/{id}` | Doctor/Nurse | Update encounter details |
| PUT | `/{id}/status` | Doctor | Update encounter status |

### Notes — `/api/v1/notes`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/patient/{patientId}` | Doctor/Nurse/Admin | Get all notes for a patient |
| POST | `/` | Doctor/Nurse/Admin | Create a clinical note |

### Prescriptions — `/api/v1/prescriptions`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/my` | Patient | Get own prescriptions |
| GET | `/` | Doctor/Nurse/Admin | List all prescriptions |
| GET | `/patient/{patientId}` | Any authenticated | Get prescriptions for a patient |
| POST | `/` | Doctor/Nurse/Admin | Create a prescription |

### Lab Orders — `/api/v1/laborder`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/my` | Patient | Get own lab orders |
| GET | `/` | Doctor/Nurse/Admin | List all lab orders |
| GET | `/{id}` | Doctor/Nurse/Admin | Get lab order by ID |
| GET | `/encounter/{encounterId}` | Doctor/Nurse/Admin | Get lab orders for an encounter |
| GET | `/patient/{patientId}` | Doctor/Nurse/Admin | Get lab orders for a patient |
| POST | `/` | Doctor | Create a lab order |
| PUT | `/{id}` | Doctor/Nurse | Update lab order details |
| PUT | `/{id}/status` | Doctor/Nurse | Update lab order status |

### Pharmacy Dispenses — `/api/v1/pharmacydispense`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/my` | Patient | Get own dispense records |
| GET | `/` | Doctor/Nurse/Admin | List all dispenses |
| GET | `/{id}` | Doctor/Nurse/Admin | Get dispense by ID |
| GET | `/prescription/{prescriptionId}` | Doctor/Nurse/Admin | Get dispenses for a prescription |
| GET | `/patient/{patientId}` | Doctor/Nurse/Admin | Get dispenses for a patient |
| POST | `/` | Doctor/Nurse | Create a dispense record |
| PUT | `/{id}` | Doctor/Nurse | Update dispense details |
| PUT | `/{id}/status` | Doctor/Nurse | Update dispense status |

### Audit Logs — `/api/v1/auditlog`

| Method | Path | Access | Description |
|---|---|---|---|
| GET | `/` | Admin | List all audit log entries |
| GET | `/user/{userId}` | Admin | Get audit logs for a specific user |
