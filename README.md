# EntryPoint — Timesheet & Expense Management

A full-stack web application for managing employee timesheets, daily work logs, and job-related expenses. Supports role-based workflows for employees, managers, and administrators with an approval pipeline, receipt attachments, and Excel export.

## What It Does

- Employees create weekly timesheets and log daily entries for hours, mileage, tolls, and parking, with optional receipt uploads.
- Managers review, approve, or deny submitted timesheets for their assigned team.
- Admins manage users, assign roles and managers, set pay rates, and view all timesheets across the organisation.

## Tech Stack

### Backend — ASP.NET Core 10 Web API

| Concern            | Library                                                      |
| ------------------ | ------------------------------------------------------------ |
| ORM                | Entity Framework Core 10                                     |
| Database           | SQL Server                                                   |
| Authentication     | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Password hashing   | BCrypt.Net-Next                                              |
| Request validation | FluentValidation.AspNetCore                                  |
| Email              | Mailjet.Api                                                  |
| Excel export       | ClosedXML                                                    |
| API docs           | Swashbuckle / OpenAPI                                        |

### Frontend — Angular 20 SPA

| Concern          | Library                                             |
| ---------------- | --------------------------------------------------- |
| Framework        | Angular 20 (standalone components, no NgModules)    |
| Reactivity       | Signals (`signal`, `computed`, `effect`) — zoneless |
| Styling          | Tailwind CSS 4 + DaisyUI 5                          |
| HTTP             | Angular `HttpClient` with a JWT auth interceptor    |
| Reactive streams | RxJS 7                                              |
| i18n             | @ngx-translate/core                                 |
| Language         | TypeScript 5.8                                      |

## Key Features

- **Role-based access** — User, Manager, and Admin roles each have distinct route guards and API policies.
- **Timesheet lifecycle** — Draft → Submitted → Approved / Denied, with optional manager comments on denial.
- **Daily log entries** — Hours, mileage, tolls, parking per day; batch creation of up to 7 entries at once.
- **Receipt management** — Upload, view, and delete file attachments per daily log entry.
- **Pay rate history** — Admin can set time-bound rates per user; historical rates are preserved.
- **Excel reports** — Generate and download spreadsheet exports of timesheet data.
- **Email notifications** — Password reset and notification emails via Mailjet.
- **Soft deletes** — Records are flagged `IsDeleted` rather than permanently removed.

## Project Structure

```
EntryPointApp/
├── EntryPointApp.Api/          # ASP.NET Core backend
│   ├── Controllers/            # Thin HTTP layer, JWT claim extraction
│   ├── Services/               # Business logic (one interface + impl per domain)
│   ├── Models/
│   │   ├── Entities/           # EF Core entities
│   │   └── Dtos/               # Request/response types
│   └── Data/                   # ApplicationDbContext
└── EntryPointApp.Client/       # Angular frontend
    └── src/app/
        ├── core/
        │   ├── services/       # Reactive state + API calls
        │   ├── models/         # TypeScript interfaces
        │   ├── guards/         # authGuard, managerGuard, adminGuard
        │   └── interceptors/   # JWT auth interceptor
        └── features/           # Route-level feature components
```

## Running Locally

Both servers must run simultaneously. The Angular dev server proxies API requests to `http://localhost:5077`.

```bash
# Backend (from EntryPointApp.Api/)
dotnet run

# Frontend (from EntryPointApp.Client/)
ng serve
```
