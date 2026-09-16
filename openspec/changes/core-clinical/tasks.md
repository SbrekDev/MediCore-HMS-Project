# Tasks: core-clinical — Patients, Professionals, Agendas & Appointments

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated total changed lines | ~6,000–7,500 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR #1 Scaffold → PR #2 Identity+Audit → PR #3 Catalog+Professionals → PR #4 Patients → PR #5 Agenda → PR #6 Scheduling → PR #7–#9 Frontend |
| Delivery strategy | auto-forecast |
| Chain strategy | feature-branch-chain (recommended assumption; confirm before apply) |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Scaffold solution, CA projects, EF Core/SQL Server, docker-compose, Vite, base domain primitives | PR #1 | `dotnet test tests/Architecture.Tests` | `docker compose up sqlserver` + `dotnet run --project src/Api` | Delete `src/`, `tests/`, `frontend/`, `docker-compose.yml` |
| 2 | Identity, roles, JWT+refresh, policies, audit interceptor | PR #2 | `dotnet test tests/Api.IntegrationTests --filter Auth` | Login via `/api/auth/login` returns JWT + cookie | Revert auth endpoints, Identity config, `AuditInterceptor` |
| 3 | Specialty catalog seed, professional registration, licenses, specialty assignments | PR #3 | `dotnet test --filter Professional", ""Specialty` | `POST /api/professionals` then `PUT /specialties/{id}` | Revert catalog/professional entities, handlers, endpoints |
| 4 | Patient registration, document uniqueness, coverage, minor responsibility, consent | PR #4 | `dotnet test --filter Patient` | `POST /api/patients` + duplicate-document 409 | Revert patient/coverage entities and endpoints |
| 5 | Weekly agenda blocks, exceptions, sobreturno cap, availability query | PR #5 | `dotnet test --filter Agenda` | `PUT /api/professionals/{id}/agenda` + `GET /availability` | Revert agenda/exception entities and endpoints |
| 6 | Appointment scheduler domain rules, booking handler with locking, lifecycle endpoints, concurrency test | PR #6 | `dotnet test tests/Api.IntegrationTests --filter Concurrency` | Parallel `POST /api/appointments` → one 201, one 409 | Revert appointment entity, scheduler, booking handler |
| 7–9 | Frontend auth, patients/professionals, agenda/booking slices | PR #7–#9 | `npm test` | `npm run dev` + manual login→book flow | Revert `frontend/src/features/{domain}` |

## Phase 1: PR #1 — Scaffold & Foundation (~500 lines)

- [x] 1.1 RED: Add `Architecture.Tests` failing when Domain references non-zero dependencies or Api bypasses Application.
- [x] 1.2 GREEN: Create solution, `src/Domain`, `Application`, `Infrastructure`, `Api`, `tests/*`, `frontend/` projects.
- [x] 1.3 REFACTOR: Enforce CA dependency rule `Api → Application → Domain` via project references and NetArchTest.
- [x] 1.4 RED: Add failing test that `AppDbContext` resolves with SQL Server provider.
- [x] 1.5 GREEN: Add `AppDbContext`, EF Core SQL Server config, `docker-compose.yml` with SQL Server, initial migration scaffold.
- [x] 1.6 GREEN: Add Vite + React 19 + TS + Vitest + Testing Library + React Router scaffold.
- [x] 1.7 GREEN: Add base `Entity<T>`, `Result<T>`, `DomainError`, `AuditableEntity`, `IAuditService`/`ICurrentUserService` abstractions.

## Phase 2: PR #2 — Identity, Access & Audit (~700 lines)

- [ ] 2.1 RED: Test role seed creates Admin, Receptionist, Professional [Spec: identity-access/Role catalog/Roles seeded].
- [ ] 2.2 GREEN: Add `ApplicationUser : IdentityUser<Guid>`, Identity config, `DbSeeder` idempotent role seed.
- [ ] 2.3 RED: Test successful login returns JWT with role claims and httpOnly refresh cookie [Spec: identity-access/JWT auth/Successful login].
- [ ] 2.4 GREEN: Implement `IJwtTokenService`, rotating refresh-token store, `/api/auth/{login,refresh,logout,me}`.
- [ ] 2.5 RED: Test failed login returns 401 and anonymous requests to protected endpoints return 401 [Spec: identity-access/JWT auth/Failed login, Anonymous rejected].
- [ ] 2.6 RED: Test role matrix allows Admin/Receptionist and forbids Professional on booking [Spec: identity-access/Role-based access].
- [ ] 2.7 GREEN: Add `AdminOnly`, `ReceptionStaff`, `ClinicalStaff` policies and authorization handlers.
- [ ] 2.8 RED: Test patient mutation writes `AuditEntry` [Spec: audit-compliance/Audit trail].
- [ ] 2.9 GREEN: Add `AuditEntry` entity and `AuditInterceptor` capturing actor/action/target/timestamp/changes.

## Phase 3: PR #3 — Specialty Catalog & Professional Management (~900 lines)

- [ ] 3.1 RED: Test specialty seed is idempotent and creates 30-min defaults [Spec: specialty-catalog/Seeded catalog].
- [ ] 3.2 GREEN: Add `Specialty` entity, config, idempotent seed, `GET /api/specialties`, `PATCH /specialties/{id}/active`.
- [ ] 3.3 RED: Test inactive specialty excluded from booking options [Spec: specialty-catalog/Active flag].
- [ ] 3.4 RED: Test professional registration requires MN and persists MPs [Spec: professional-management/Registration].
- [ ] 3.5 GREEN: Add `Professional`, `ProvincialLicenseMP` entities, validators, handlers, `POST/GET/PUT /api/professionals`.
- [ ] 3.6 RED: Test specialty assignment records multiple specialties with primary flag [Spec: professional-management/Specialty assignment].
- [ ] 3.7 GREEN: Add `ProfessionalSpecialty` join, `PUT /api/professionals/{id}/specialties/{specialtyId}`.
- [ ] 3.8 RED: Test duration default inherits 30 min and override applies 45 min [Spec: professional-management/Duration override].

## Phase 4: PR #4 — Patient Management (~900 lines)

- [ ] 4.1 RED: Test patient registration captures document type/number, names, birth date, contact, address [Spec: patient-management/Registration].
- [ ] 4.2 GREEN: Add `Patient`, `Address`, `Contact`, `CoverageEntry`, `Insurer` entities and EF configs.
- [ ] 4.3 RED: Test duplicate (DNI, number) returns 409; same number different type is accepted [Spec: patient-management/Unique document].
- [ ] 4.4 GREEN: Add unique index `(DocumentType, DocumentNumber)`, handlers, `GET/POST /api/patients`, `GET/PUT /api/patients/{id}`.
- [ ] 4.5 RED: Test minor without responsible rejected, minor with responsible accepted, adult accepted [Spec: patient-management/Minor requires responsible].
- [ ] 4.6 GREEN: Add age-derived validation and `ResponsiblePatientId` self-FK.
- [ ] 4.7 RED: Test coverage history with exactly one active, Particular requires no insurer [Spec: patient-management/Coverage history].
- [ ] 4.8 GREEN: Add filtered unique index `(PatientId) WHERE IsActive=1`, `POST /api/patients/{id}/coverages`.
- [ ] 4.9 RED: Test search by document number and name [Spec: patient-management/Patient search].
- [ ] 4.10 RED: Test consent recorded at registration and pending flag surfaced [Spec: audit-compliance/Consent capture].
- [ ] 4.11 GREEN: Add `ConsentStatus`/`ConsentGivenAt`, `POST /api/patients/{id}/consent`, patient DTO exposes `consentPending`.

## Phase 5: PR #5 — Agenda Management (~600 lines)

- [ ] 5.1 RED: Test weekly availability blocks define bookable weekday windows [Spec: agenda-management/Weekly template].
- [ ] 5.2 GREEN: Add `Agenda`, `AvailabilityBlock` entities, `PUT /api/professionals/{id}/agenda`.
- [ ] 5.3 RED: Test full-day exception blocks booking and partial exception leaves remaining hours [Spec: agenda-management/Exceptions].
- [ ] 5.4 GREEN: Add `AgendaException`, `POST/DELETE /api/professionals/{id}/agenda/exceptions`.
- [ ] 5.5 RED: Test daily sobreturno cap enforced [Spec: agenda-management/Sobreturno cap].
- [ ] 5.6 GREEN: Add `MaxSobreturnosPerDay` and `GET /api/professionals/{id}/availability?date=`.

## Phase 6: PR #6 — Appointment Scheduling (~1,100 lines)

- [ ] 6.1 RED: Test `AppointmentScheduler` rejects professional overlap and allows adjacent appointments [Spec: appointment-scheduling/No double-booking].
- [ ] 6.2 GREEN: Implement pure domain `AppointmentScheduler.Schedule(SchedulingRequest, ScheduleContext)`.
- [ ] 6.3 RED: Test patient overlap across professionals rejected [Spec: appointment-scheduling/No patient overlap].
- [ ] 6.4 RED: Test unheld specialty rejected [Spec: appointment-scheduling/Specialty validity].
- [ ] 6.5 RED: Test 2-hour minimum and 90-day maximum booking window [Spec: appointment-scheduling/Booking window].
- [ ] 6.6 RED: Test sobreturno allowed only for Receptionist/Admin, beyond capacity, within cap [Spec: appointment-scheduling/Sobreturno].
- [ ] 6.7 GREEN: Add `Appointment` entity, `BookAppointmentHandler` with `UPDLOCK,HOLDLOCK` anchor locks, `POST /api/appointments`.
- [ ] 6.8 RED: Integration test concurrent double-booking → exactly one 201, one 409, one row [Spec: appointment-scheduling/Concurrent double-booking].
- [ ] 6.9 GREEN: Add reschedule endpoint revalidating all rules [Spec: appointment-scheduling/Reschedule].
- [ ] 6.10 RED: Test cancellation flags late when <24h, completion allowed only for own professional [Spec: appointment-scheduling/Cancellation, Completion].
- [ ] 6.11 GREEN: Add `POST /api/appointments/{id}/{reschedule,cancel,complete,no-show}` and no-show counter increment.

## Phase 7: PR #7 — Frontend Auth (~400 lines)

- [ ] 7.1 RED: Test login form submits credentials and stores access token [Spec: identity-access/JWT auth].
- [ ] 7.2 GREEN: Add `api/axios.ts` with in-memory token and 401 refresh single-flight interceptor.
- [ ] 7.3 GREEN: Add `<RequireRole>` guard and `/login` route.

## Phase 8: PR #8 — Frontend Patients & Professionals (~700 lines)

- [ ] 8.1 RED: Test patient search/list/form with validation [Spec: patient-management].
- [ ] 8.2 GREEN: Add `features/patients/` with RHF + zod, TanStack Query.
- [ ] 8.3 RED: Test professional list/form and specialty assignment UI [Spec: professional-management].
- [ ] 8.4 GREEN: Add `features/professionals/`.

## Phase 9: PR #9 — Frontend Agenda & Booking (~700 lines)

- [ ] 9.1 RED: Test agenda block editor and exception UI [Spec: agenda-management].
- [ ] 9.2 GREEN: Add `features/agenda/`.
- [ ] 9.3 RED: Test booking form validates window/overlap/specialty via mocked API [Spec: appointment-scheduling].
- [ ] 9.4 GREEN: Add `features/booking/` with availability query and appointment lifecycle actions.
