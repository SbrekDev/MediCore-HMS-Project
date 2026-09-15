# Design: core-clinical — Patients, Professionals, Agendas & Appointments

## Technical Approach

Clean Architecture (.NET 10) + EF Core/SQL Server, React 19/TS/Vite, strict TDD. Scheduling (proposal option B): `AvailabilityBlock` defines bookable time; `Appointment` carries Start + DurationMinutes; a pure domain `AppointmentScheduler` enforces every rule; a lock-guarded transaction prevents double-booking.

## Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Concurrency | Txn + `UPDLOCK, HOLDLOCK` anchor locks (Professionals row, then Patients — canonical order); overlap check + insert inside. Rejected: SERIALIZABLE ranges, `sp_getapplock`, app mutex | Serializes the critical section: no phantoms, deadlock-free, testable |
| Rule placement | Pure `AppointmentScheduler` (Domain); handler loads data under lock | Pure = unit-testable; DB only serializes |
| Clock | Injected `TimeProvider` | 2h/90d/24h rules testable |
| One active coverage | Domain invariant + filtered unique index `(PatientId) WHERE IsActive=1` | DB guarantees the invariant |
| Validation | FluentValidation (Application) | CA convention; clean entities |
| Forms | React Hook Form + zod | Type-safe, fewer re-renders |
| Refresh token | Rotating, hashed, httpOnly SameSite=Strict cookie | XSS-safe; rotation shrinks theft window |

## Data Flow (booking)

    React form → axios(JWT) → endpoint (policy) → BookAppointmentHandler
      → txn → lock Professional row → lock Patient row (UPDLOCK,HOLDLOCK)
      → load appointments/blocks/exceptions/sobreturno count
      → AppointmentScheduler.Schedule() → insert → AuditInterceptor → commit → 201 | 409

Concurrent requests block on the anchor until commit, then re-read and reject — exactly one wins.

## Clean Architecture Layout (all Create — greenfield)

| Path | Contents |
|---|---|
| `src/Domain` | Entities, enums, value objects, `AppointmentScheduler`, Result/DomainError; zero dependencies |
| `src/Application` | Handlers, DTOs, validators, abstractions (`IAppointmentRepository`, `ICurrentUserService`, `IJwtTokenService`, `IAuditService`) |
| `src/Infrastructure` | AppDbContext, configurations, migrations, repositories (locking SQL via `FromSqlRaw`), Identity/JWT, AuditInterceptor, DbSeeder |
| `src/Api` | Endpoints, auth + policies, ProblemDetails middleware, DI root |
| `tests/` | Domain.UnitTests, Application.UnitTests, Api.IntegrationTests, Architecture.Tests (NetArchTest) |
| `frontend/src` | `api/` (axios + interceptors), `features/{auth,patients,professionals,agenda,booking}`, `routes/`, `components/`, `auth/` (in-memory token) |

Dependency rule: Api → Application → Domain; Infrastructure implements Application abstractions (enforced by Architecture.Tests).

## EF Core Data Model

- **Patient**: DocumentType(enum)+DocumentNumber, names, BirthDate, owned Address/Contact, ResponsiblePatientId (self-FK), NoShowCount, ConsentStatus+ConsentGivenAt. Unique `(DocumentType,DocumentNumber)`.
- **CoverageEntry**: PatientId, Type (ObraSocial/Prepaga/Particular), InsurerId?, PlanName, AffiliateNumber, IsActive.
- **Insurer**(Name, Type — seeded fictional); **Professional**(names, NationalLicenseMN unique); **ProvincialLicenseMP**(Number, Province).
- **Specialty**: Code (unique), Name, DefaultDurationMinutes (30), IsActive. **ProfessionalSpecialty**: composite PK, IsPrimary, DurationOverrideMinutes?.
- **Agenda**: ProfessionalId (unique), MaxSobreturnosPerDay. **AvailabilityBlock**: AgendaId, DayOfWeek, Start, End. **AgendaException**: AgendaId, Date, Start?, End?, Reason.
- **Appointment**: PatientId, ProfessionalId, SpecialtyId, StartDateTime, DurationMinutes, Status, IsSobreturno, IsLateCancellation. Indexes `(ProfessionalId,StartDateTime)`, `(PatientId,StartDateTime)` — overlap scans + lock ranges.
- **AuditEntry**(ActorUserId, Action, EntityType, EntityId, TimestampUtc, ChangesJson); **ApplicationUser** : IdentityUser\<Guid\> (+ProfessionalId?); **RefreshToken**(UserId, TokenHash, ExpiresAt, RevokedAt).

RowVersion tokens on Patient/Professional/Appointment. Migrations: code-first, one per capability PR. Seeds: idempotent `DbSeeder` (existence checks; users need UserManager, so not `HasData`).

## Interfaces / Contracts

```csharp
public sealed record SchedulingRequest(Guid PatientId, Guid ProfessionalId, Guid SpecialtyId,
    DateTimeOffset Start, bool IsSobreturno, bool ActorIsReceptionStaff);
public sealed record ScheduleContext(IReadOnlyList<Appointment> ProfessionalDay, IReadOnlyList<Appointment> PatientDay,
    IReadOnlyList<AvailabilityBlock> Blocks, IReadOnlyList<AgendaException> Exceptions,
    int SobreturnosToday, int SobreturnoCap, int DurationMinutes, DateTimeOffset Now);
// Domain service: Result<Appointment> Schedule(SchedulingRequest req, ScheduleContext ctx)
```

**API** (REST; ProblemDetails + `code`; 409 conflicts; 401/403 auth):

- Auth: `POST /api/auth/{login,refresh,logout}`, `GET /api/auth/me`
- Patients: `GET/POST /api/patients`, `GET/PUT /api/patients/{id}`, `POST /{id}/coverages`, `POST /{id}/consent`
- Professionals: `GET/POST/PUT /api/professionals`, `PUT /{id}/specialties/{specialtyId}`
- Specialties: `GET /api/specialties`, `PATCH /api/specialties/{id}/active` (Admin)
- Agenda: `PUT /api/professionals/{id}/agenda`, `POST/DELETE .../agenda/exceptions`, `GET /api/professionals/{id}/availability?date=`
- Appointments: `POST /api/appointments`, `POST /api/appointments/{id}/{reschedule,cancel,complete,no-show}`, `GET /api/appointments?professionalId=&date=`

**Policies** (mirror identity-access matrix): `AdminOnly` (users/professionals/catalog/config); `ReceptionStaff`=Admin+Receptionist (patients/booking/reschedule/cancel/sobreturnos); `ClinicalStaff`=all roles (agenda view, complete/no-show) + handler ownership check `user.ProfessionalId == appointment.ProfessionalId`.

## Auth

Login → UserManager → 15-minute JWT (sub + role claims) in the body; rotating refresh token (hashed) as httpOnly, Secure, SameSite=Strict cookie scoped to `/api/auth`; `/refresh` rotates, `/logout` revokes. React holds the access token in memory; the axios interceptor single-flights a refresh on 401 and retries.

## Frontend

TanStack Query (server state, mutation invalidation); React Router: `/login /patients /patients/:id /professionals /agenda /booking`; RHF + zod forms; `<RequireRole>` guards shape the UI — server stays authoritative.

## Seed Strategy

Idempotent `DbSeeder` on dev startup: roles; ~10 specialties (code/name/30-min); 3 fictional insurers + Particular; demo users `admin|receptionist|professional@ceibos.demo`; demo professional with Mon–Fri blocks.

## Audit & Compliance

`AuditInterceptor : SaveChangesInterceptor` writes AuditEntry (actor, action, entity, timestamp, changed-values JSON) on every Patient/Professional/Agenda/Appointment mutation. Consent captured at registration (ConsentStatus, ConsentGivenAt); patient DTO exposes `consentPending` — Ley 25.326 / 26.529.

## Testing Strategy

| Layer | What | Approach |
|---|---|---|
| Domain unit | Every scheduler rule (overlap, containment, window, sobreturno cap, duration); minor/coverage invariants | xUnit + FluentAssertions, pure |
| Application | Handlers, authorization error paths | Mocked repositories |
| API integration | Endpoint × role matrix (401/403), duplicate-document 409; **concurrency**: two parallel POSTs, same slot → exactly one 201, one 409, one row | WebApplicationFactory + Testcontainers SQL Server (LocalDB fallback) |
| Frontend | Booking form, role guards, 401-refresh interceptor | Vitest + Testing Library + MSW |

Strict TDD: RED → GREEN → REFACTOR.

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary.

## Migration / Rollout

No migration required (greenfield). Chained PRs per capability (400-line budget): Identity → Catalog+Professionals → Patients → Agenda → Scheduling → frontend slices.

## Open Questions

- None blocking (refresh-token reuse detection: on).
