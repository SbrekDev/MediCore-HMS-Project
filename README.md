# MediCore HMS

A Hospital Management System for managing the clinical front desk of a sanatorio: who patients are, which professionals practice which specialties, and when appointments happen. Built as a portfolio-grade demo for **Sanatorio Los Ceibos** (fictional institution), with production discipline: Clean Architecture, strict TDD, and spec-driven development.

## Features

- **Patient management** — registration with document type/number (DNI, Pasaporte, LC, LE, DNI Extranjero), coverage history, minor-responsible rules, consent capture.
- **Professional management** — registration with provincial licenses (MN/MP), multi-specialty assignment with primary flag, per-professional duration overrides.
- **Specialty catalog** — seeded, idempotent, with active/inactive flags.
- **Agenda management** — weekly availability blocks, full/partial-day exceptions, daily sobreturno (overbooking) caps.
- **Appointment scheduling** — double-booking prevention, patient-overlap rules, booking windows, concurrency-safe booking with SQL Server locking.
- **Identity & access** — ASP.NET Core Identity + JWT with rotating refresh tokens, role-based policies (Admin, Receptionist, Professional).
- **Audit & compliance** — every mutation captured with actor, action, target, timestamp, and changes.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | C# / ASP.NET Core Web API, .NET 10 (LTS), Clean Architecture |
| Persistence | EF Core + SQL Server, ASP.NET Core Identity |
| Frontend | React 19, TypeScript, Vite, React Router |
| Backend tests | xUnit, FluentAssertions, NetArchTest, Testcontainers |
| Frontend tests | Vitest, Testing Library |
| Infrastructure | Docker Compose (SQL Server) |

## Project Structure

```
MediCore.slnx
src/
  Domain/           # Entities, primitives, domain errors — zero dependencies
  Application/      # Use cases, abstractions, handlers
  Infrastructure/   # EF Core, Identity, persistence, seeding
  Api/              # HTTP endpoints, auth, composition root
tests/
  Architecture.Tests/       # Clean Architecture dependency rules
  Domain.UnitTests/
  Application.UnitTests/
  Api.IntegrationTests/     # Testcontainers-backed integration tests
frontend/           # React 19 + TS + Vite SPA
openspec/           # Spec-driven development artifacts (specs, design, tasks)
docker-compose.yml  # SQL Server for local dev and integration tests
```

## Quick Start

**Prerequisites:** .NET 10 SDK, Node.js 20+, Docker Desktop.

1. **Start SQL Server**
   ```bash
   docker compose up -d sqlserver
   ```
2. **Apply migrations and run the API**
   ```bash
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   dotnet run --project src/Api
   ```
   Roles (Admin, Receptionist, Professional) are seeded idempotently at startup.
3. **Run the frontend**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

## Testing

| Suite | Command | Notes |
|-------|---------|-------|
| All backend tests | `dotnet test` | |
| Architecture rules | `dotnet test tests/Architecture.Tests` | CA dependency enforcement |
| Integration | `dotnet test tests/Api.IntegrationTests` | Requires Docker running (Testcontainers) |
| Frontend | `cd frontend && npm test` | |

## Development Workflow

| Practice | Detail |
|----------|--------|
| Methodology | Spec-Driven Development (SDD) — proposals, specs, design, and tasks live in `openspec/` |
| TDD | Strict RED → GREEN → REFACTOR; every task starts as a failing test |
| Delivery | Chained PRs (`feature-branch-chain`), one work-unit commit per task |
| Commits | Conventional Commits |
| Review budget | ≤400 changed lines per PR; larger scopes split into chained slices |

## Project Status

Active change: **`core-clinical`** — Patients, Professionals, Agendas & Appointments.

| PR | Scope | Status |
|----|-------|--------|
| #1 `pr-01-scaffold` | Solution scaffold, CA projects, EF Core, frontend scaffold | ✅ Complete |
| #2 `pr-02-identity` | Identity, roles, JWT, audit interceptor | 🚧 In progress |
| #3–#6 | Catalog, patients, agenda, scheduling | Planned |
| #7–#9 | Frontend slices (auth, patients/professionals, booking) | Planned |

## Disclaimer

This is a fictional demo project. The domain takes functional inspiration from real sanatorios, but **Sanatorio Los Ceibos** is an invented name and no real institution, data, or branding is represented.
