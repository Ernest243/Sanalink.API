# SanaLink API

SanaLink is a minimal Electronic Health Record (EHR) system built with ASP.NET Core and SQLite. It enables hospitals and clinics to securely manage patients, appointments, and clinical data.

## 🚀 Features

- User registration & JWT-based login
- Role-based access (Doctor, Nurse, Admin)
- Secure patient record management
- Appointment scheduling & tracking
- SQLite integration with Entity Framework Core

## 📦 Tech Stack

- ASP.NET Core Web API
- Entity Framework Core (SQLite)
- ASP.NET Identity
- Swagger UI (for API testing)

## 📂 Roles and Permissions

| Role     | Patient Access | Appointment Access |
|----------|----------------|--------------------|
| Doctor   | View, Create, Edit | View, Create, Edit, Cancel |
| Nurse    | View only      | View only          |
| Admin    | View only      | View, Cancel       |

## 🛠 Getting Started

1. Clone the repository
2. Restore dependencies  
   ```bash
   dotnet restore
3. Apply migrations
   ```bash
   dotnet ef database update
4. Run the API
    ```bash
    dotnet run
5. Test with Swagger at
    ```bash
    http://localhost:<port>/swagger
