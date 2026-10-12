# Apply Progress: core-clinical

## Phase 2 — Identity, Access & Audit (completed)

- [x] 2.1 RED: Test role seed creates Admin, Receptionist, Professional.
- [x] 2.2 GREEN: Add `ApplicationUser : IdentityUser<Guid>`, Identity config, `DbSeeder` idempotent role seed.
- [x] 2.3 RED: Test successful login returns JWT with role claims and httpOnly refresh cookie.
- [x] 2.4 GREEN: Implement `IJwtTokenService`, rotating refresh-token store, `/api/auth/{login,refresh,logout,me}`.
- [x] 2.5 RED: Test failed login returns 401 and anonymous requests to protected endpoints return 401.
- [x] 2.6 RED: Test role matrix allows Admin/Receptionist and forbids Professional on booking.
- [x] 2.7 GREEN: Add `AdminOnly`, `ReceptionStaff`, `ClinicalStaff` policies and authorization handlers.
- [x] 2.8 RED: Test patient mutation writes `AuditEntry`.
- [x] 2.9 GREEN: Add `AuditEntry` entity and `AuditInterceptor` capturing actor/action/target/timestamp/changes.

## Phase 3 / Slice A — Specialty Catalog (completed)

- [x] 3.1 RED: Test specialty seed is idempotent and creates 30-min defaults [Spec: specialty-catalog/Seeded catalog].
  - Evidence: `dotnet test tests/Domain.UnitTests` → 6 passed (includes `SpecialtyTests`).
  - Evidence: `dotnet test tests/Api.IntegrationTests --filter SpecialtySeedTests` → 1 passed (InMemory, no container).
- [x] 3.2 GREEN: Add `Specialty` entity, config, idempotent seed, `GET /api/specialties`, `PATCH /specialties/{id}/active`.
  - Evidence: `dotnet build MediCore.slnx` → 0 warnings, 0 errors.
  - Evidence: `dotnet ef migrations add AddSpecialtySchema` generated `20261012160629_AddSpecialtySchema`.
- [x] 3.3 RED: Test inactive specialty excluded from booking options [Spec: specialty-catalog/Active flag].
  - Evidence: `dotnet test tests/Application.UnitTests` → 2 passed (includes `SpecialtySelectionTests`).
  - Implementation: `ListSpecialties.Query(ActiveOnly: true)` filters `IsActive`; endpoint exposes `?activeOnly=true`.

## Work Unit Evidence

| Work unit | Focused test command | Result | Runtime harness | Rollback boundary |
|---|---|---|---|---|
| Domain entity + seed data | `dotnet test tests/Domain.UnitTests` | 6 passed | N/A (pure domain) | Remove `Specialty.cs`, `SpecialtySeedData.cs`, `SpecialtyTests.cs` |
| Active selection projection | `dotnet test tests/Application.UnitTests` | 2 passed | N/A (InMemory unit test) | Remove `ListSpecialties.cs`, `ToggleSpecialtyActive.cs`, `SpecialtySelectionTests.cs` |
| EF config + idempotent seed | `dotnet test tests/Api.IntegrationTests --filter SpecialtySeedTests` | 1 passed | N/A (InMemory unit test) | Remove `SpecialtyConfiguration.cs`, revert `DbSeeder.cs`, `AppDbContext.cs`, migration |
| API endpoints + startup wiring | `dotnet build MediCore.slnx` | 0 errors | `dotnet run --project src/Api` (not run due to no SQL Server locally) | Remove `SpecialtyEndpoints.cs`, revert `DependencyInjection.cs`, `Program.cs` |

## Verification summary

| Command | Result |
|---|---|
| `dotnet build MediCore.slnx` | Build succeeded (0 warnings, 0 errors) |
| `dotnet test tests/Domain.UnitTests` | 6 passed, 0 failed, 0 skipped |
| `dotnet test tests/Application.UnitTests` | 2 passed, 0 failed, 0 skipped |
| `dotnet test tests/Architecture.Tests` | 3 passed, 0 failed, 0 skipped |
| `dotnet test tests/Api.IntegrationTests` | 4 passed, 7 failed (environmental: SQL Server/Testcontainers unavailable), 1 skipped |

## Commits

- `662c92b` feat(specialty): add Specialty entity and seeded catalog with 30-min defaults (task 3.1)
- `2af2aa1` feat(specialty): add active-only specialty query and toggle handler (task 3.3)
- `57fa8ed` feat(specialty): add GET/PATCH endpoints and startup seed wiring (task 3.2)

## Phase 3 / Slice B — Professional Management (completed)

- [x] 3.4 RED: Test professional registration requires MN and persists MPs [Spec: professional-management/Registration].
  - Evidence: `dotnet test tests/Application.UnitTests --filter ProfessionalRegistrationTests` → 2 passed.
  - Evidence: `dotnet test tests/Domain.UnitTests --filter ProfessionalTests` → 1 passed.
- [x] 3.5 GREEN: Add `Professional`, `ProvincialLicenseMP` entities, handlers, `POST/GET/PUT /api/professionals`.
  - Evidence: `dotnet build MediCore.slnx` → 0 warnings, 0 errors.
  - Evidence: `dotnet ef migrations add AddProfessionalSchema` generated `20261012163642_AddProfessionalSchema`.
- [x] 3.6 RED: Test specialty assignment records multiple specialties with primary flag [Spec: professional-management/Specialty assignment].
  - Evidence: `dotnet test tests/Application.UnitTests --filter ProfessionalSpecialtyAssignmentTests` → 2 passed.
- [x] 3.7 GREEN: Add `ProfessionalSpecialty` join entity and `PUT /api/professionals/{id}/specialties/{specialtyId}`.
  - Evidence: `dotnet build MediCore.slnx` → 0 warnings, 0 errors.
  - Evidence: `dotnet ef migrations add AddProfessionalSpecialtySchema` generated `20261012163922_AddProfessionalSpecialtySchema`.
- [x] 3.8 RED: Test duration default inherits 30 min and override applies 45 min [Spec: professional-management/Duration override].
  - Evidence: `dotnet test tests/Domain.UnitTests --filter ProfessionalSpecialtyDurationTests` → 2 passed.
  - Implementation: `ProfessionalSpecialty.GetDurationMinutes(int specialtyDefaultDuration)` returns `DurationOverrideMinutes ?? specialtyDefaultDuration`.

## Work Unit Evidence

| Work unit | Focused test command | Result | Runtime harness | Rollback boundary |
|---|---|---|---|---|
| Domain entity + seed data | `dotnet test tests/Domain.UnitTests` | 6 passed | N/A (pure domain) | Remove `Specialty.cs`, `SpecialtySeedData.cs`, `SpecialtyTests.cs` |
| Active selection projection | `dotnet test tests/Application.UnitTests` | 2 passed | N/A (InMemory unit test) | Remove `ListSpecialties.cs`, `ToggleSpecialtyActive.cs`, `SpecialtySelectionTests.cs` |
| EF config + idempotent seed | `dotnet test tests/Api.IntegrationTests --filter SpecialtySeedTests` | 1 passed | N/A (InMemory unit test) | Remove `SpecialtyConfiguration.cs`, revert `DbSeeder.cs`, `AppDbContext.cs`, migration |
| API endpoints + startup wiring | `dotnet build MediCore.slnx` | 0 errors | `dotnet run --project src/Api` (not run due to no SQL Server locally) | Remove `SpecialtyEndpoints.cs`, revert `DependencyInjection.cs`, `Program.cs` |
| Professional registration (3.4/3.5) | `dotnet test tests/Application.UnitTests --filter ProfessionalRegistrationTests` | 2 passed | `dotnet test tests/Api.IntegrationTests` (container) | Remove `Professional.cs`, `ProvincialLicenseMP.cs`, `CreateProfessional.cs`, `ListProfessionals.cs`, `GetProfessional.cs`, `UpdateProfessional.cs`, configurations, migration `20261012163642_AddProfessionalSchema` |
| Specialty assignment (3.6/3.7) | `dotnet test tests/Application.UnitTests --filter ProfessionalSpecialtyAssignmentTests` | 2 passed | `dotnet test tests/Api.IntegrationTests` (container) | Remove `ProfessionalSpecialty.cs`, `AssignProfessionalSpecialty.cs`, configuration, migration `20261012163922_AddProfessionalSpecialtySchema` |
| Duration override resolution (3.8) | `dotnet test tests/Domain.UnitTests --filter ProfessionalSpecialtyDurationTests` | 2 passed | N/A (pure domain) | Remove `GetDurationMinutes` from `ProfessionalSpecialty.cs` |

## Verification summary

| Command | Result |
|---|---|
| `dotnet build MediCore.slnx` | Build succeeded (0 warnings, 0 errors) |
| `dotnet test tests/Domain.UnitTests` | 9 passed, 0 failed, 0 skipped |
| `dotnet test tests/Application.UnitTests` | 6 passed, 0 failed, 0 skipped |
| `dotnet test tests/Architecture.Tests` | 3 passed, 0 failed, 0 skipped |
| `dotnet test tests/Api.IntegrationTests` | 12 passed, 0 failed, 0 skipped |

## Commits

- `0ddd289` test(professional): add RED tests for professional registration with MN and MPs (task 3.4)
- `a8249eb` feat(professional): add Professional entity, handlers, endpoints and migration (task 3.5)
- `d981f44` test(professional): add RED tests for specialty assignment with primary flag (task 3.6)
- `d77e668` feat(professional): add ProfessionalSpecialty join entity and assignment endpoint (task 3.7)
- `4232a70` test(professional): add RED tests for duration default and override (task 3.8)
- `232d9a0` fix(api): remove direct Domain error reference from ProfessionalEndpoints

## Phase 4 / Slice A — Patient Core (tasks 4.1–4.4)

- [x] 4.1 RED: Test patient registration captures document type/number, names, birth date, contact, address [Spec: patient-management/Registration].
  - Evidence: `dotnet test tests/Application.UnitTests --filter PatientRegistrationTests` → 3 passed.
- [x] 4.2 GREEN: Add `Address`/`Contact` owned by `Patient`, `CoverageEntry`/`Insurer` entities with EF configs. Extend `Patient`. Add EF Core migration `AddPatientContactAddressCoverageSchema`.
  - Evidence: `dotnet build MediCore.slnx` → 0 warnings, 0 errors.
  - Evidence: `dotnet ef migrations add AddPatientContactAddressCoverageSchema` generated `20261012171651_AddPatientContactAddressCoverageSchema`.
- [x] 4.3 RED: Test duplicate (DocumentType, DocumentNumber) returns 409; same number different type is accepted [Spec: patient-management/Unique document].
  - Evidence: `dotnet test tests/Application.UnitTests --filter PatientRegistrationTests` → duplicate test passes (conflict result).
- [x] 4.4 GREEN: Rework `CreatePatient` to return `Result<Guid>` with conflict error on duplicate; add `GET /api/patients`, `GET /api/patients/{id}` and `PUT /api/patients/{id}` handlers/endpoints.
  - Evidence: `dotnet test tests/Api.IntegrationTests --filter PatientEndpointTests` → 6 passed.
  - Authorization: POST/PUT = `ReceptionStaff`; GET = `ClinicalStaff`.
  - Validation: inline validation following existing handler convention.

## Work Unit Evidence

| Work unit | Focused test command | Result | Runtime harness | Rollback boundary |
|---|---|---|---|---|
| Patient registration (4.1/4.2) | `dotnet test tests/Application.UnitTests --filter PatientRegistrationTests` | 3 passed | `dotnet test tests/Api.IntegrationTests --filter PatientEndpointTests` (container) | Remove `Address.cs`, `Contact.cs`, revert `Patient.cs`, `PatientConfiguration.cs`, `CreatePatient.cs`, migration `20261012171651_AddPatientContactAddressCoverageSchema` |
| Document uniqueness + CRUD (4.3/4.4) | `dotnet test tests/Application.UnitTests --filter PatientRegistrationTests` | 3 passed | `dotnet test tests/Api.IntegrationTests --filter PatientEndpointTests` (container) | Revert `CreatePatient.cs`, `ListPatients.cs`, `GetPatient.cs`, `UpdatePatient.cs`, `PatientEndpoints.cs`, `Api.DependencyInjection.cs` |
| Coverage schema (4.2) | N/A (model only) | N/A | N/A | Remove `CoverageEntry.cs`, `Insurer.cs`, configurations, update `AppDbContext.cs`, `IApplicationDbContext.cs` |

## Verification summary

| Command | Result |
|---|---|
| `dotnet build MediCore.slnx` | Build succeeded (0 warnings, 0 errors) |
| `dotnet test tests/Domain.UnitTests` | 9 passed, 0 failed, 0 skipped |
| `dotnet test tests/Application.UnitTests` | 9 passed, 0 failed, 0 skipped |
| `dotnet test tests/Architecture.Tests` | 3 passed, 0 failed, 0 skipped |
| `dotnet test tests/Api.IntegrationTests` | 18 passed, 0 failed, 0 skipped |

## Commits

- `f55dab0` test(patient): add RED tests for patient registration, contact/address and duplicate document (tasks 4.1, 4.3)
- `50e8778` feat(patient): add contact/address schema, coverage entities, document conflict detection and CRUD endpoints (tasks 4.2, 4.4)

## Phase 4 / Slice B — Minor Responsibility & Coverage History (tasks 4.5–4.8)

- [x] 4.5 RED: Test minor without responsible rejected, minor with responsible accepted, adult accepted [Spec: patient-management/Minor requires responsible].
  - Evidence: `dotnet test tests/Application.UnitTests --filter PatientRegistrationTests` → 6 passed.
  - Evidence: `dotnet test tests/Api.IntegrationTests --filter PatientEndpointTests` → 8 passed.
- [x] 4.6 GREEN: Add age-derived validation and `ResponsiblePatientId` self-FK.
  - Evidence: `dotnet build MediCore.slnx` → 0 warnings, 0 errors.
  - Evidence: `dotnet ef migrations add AddPatientResponsibleAndCoverageSchema` generated `20261012174353_AddPatientResponsibleAndCoverageSchema`.
  - Implementation: `Patient.ResponsiblePatientId` self-FK with `DeleteBehavior.Restrict`; `CreatePatient`/`UpdatePatient` validate minors have a responsible; age computed from `BirthDate` against `DateTimeOffset.UtcNow`.
- [x] 4.7 RED: Test coverage history with exactly one active, Particular requires no insurer [Spec: patient-management/Coverage history].
  - Evidence: `dotnet test tests/Application.UnitTests --filter CoverageHistoryTests` → 6 passed.
- [x] 4.8 GREEN: Add `POST /api/patients/{id}/coverages` enforcing single-active invariant and Particular self-pay rule; seed 3 fictional insurers idempotently via `DbSeeder`.
  - Evidence: `dotnet test tests/Api.IntegrationTests --filter "PatientEndpointTests|InsurerSeedTests"` → 13 passed.
  - Implementation: `AddCoverage.Handler` transactionally deactivates prior active entry; `CoverageEntry.AffiliateNumber` made nullable; `InsurerSeedData.Catalog` + `DbSeeder.SeedInsurersAsync` called from `Program.cs`.

## Work Unit Evidence

| Work unit | Focused test command | Result | Runtime harness | Rollback boundary |
|---|---|---|---|---|
| Minor requires responsible (4.5/4.6) | `dotnet test tests/Application.UnitTests --filter PatientRegistrationTests` | 6 passed | `dotnet test tests/Api.IntegrationTests --filter PatientEndpointTests` (container) | Remove `Patient.ResponsiblePatientId`, revert `CreatePatient.cs`, `UpdatePatient.cs`, `PatientConfiguration.cs`, `PatientEndpoints.cs`, migration `20261012174353_AddPatientResponsibleAndCoverageSchema` |
| Coverage history + insurer seed (4.7/4.8) | `dotnet test tests/Application.UnitTests --filter CoverageHistoryTests` | 6 passed | `dotnet test tests/Api.IntegrationTests --filter "PatientEndpointTests|InsurerSeedTests"` (container) | Remove `AddCoverage.cs`, `InsurerSeedData.cs`, revert `CoverageEntry.cs`, `CoverageEntryConfiguration.cs`, `PatientEndpoints.cs`, `DbSeeder.cs`, `Program.cs`, `Api.DependencyInjection.cs` |

## Verification summary

| Command | Result |
|---|---|
| `dotnet build MediCore.slnx` | Build succeeded (0 warnings, 0 errors) |
| `dotnet test tests/Domain.UnitTests` | 9 passed, 0 failed, 0 skipped |
| `dotnet test tests/Application.UnitTests` | 18 passed, 0 failed, 0 skipped |
| `dotnet test tests/Architecture.Tests` | 3 passed, 0 failed, 0 skipped |
| `dotnet test tests/Api.IntegrationTests` | 25 passed, 0 failed, 0 skipped |

## Commits

- `5a931ce` feat(patient): enforce minor requires responsible with self-FK and age validation (tasks 4.5-4.6)
- `0dc39b1` feat(patient): add coverage history endpoint with single-active invariant and insurer seed (tasks 4.7-4.8)

## Status

8/8 tasks complete for Phase 4 / Slices A and B (4.1–4.8). Ready for Phase 4 / Slice C (tasks 4.9–4.11).

## Phase 4 / Slice C — Patient Search & Consent (tasks 4.9–4.11)

- [x] 4.9 RED: Test search by document number and name [Spec: patient-management/Patient search].
  - Evidence: `dotnet test tests/Api.IntegrationTests --filter "FullyQualifiedName~Get_patients_by_document_number|FullyQualifiedName~Get_patients_by_name|FullyQualifiedName~Get_patients_by_unmatched"` → 3 failed as expected (endpoint ignores search).
- [x] 4.10 RED: Test consent recorded at registration and pending flag surfaced [Spec: audit-compliance/Consent capture].
  - Evidence: `dotnet test tests/Api.IntegrationTests --filter "FullyQualifiedName~Post_patient_without_consent|FullyQualifiedName~Post_patient_with_consent|FullyQualifiedName~Post_consent_records"` → 3 failed as expected (`consentPending` absent, consent endpoint 404).
- [x] 4.11 GREEN: Add `ConsentStatus`/`ConsentGivenAt` to `Patient`; expose `consentPending` on the patient DTO; add `POST /api/patients/{id}/consent`.
  - Evidence: `dotnet build MediCore.slnx` → 0 warnings, 0 errors.
  - Evidence: `dotnet ef migrations add AddPatientConsentColumns` generated `20261012182606_AddPatientConsentColumns`.
  - Evidence: `dotnet test tests/Api.IntegrationTests --filter PatientEndpointTests` → 18 passed.
  - Implementation: `ConsentStatus` enum (`Pending`, `Given`); `Patient.RecordConsent`; `CreatePatient` accepts optional `ConsentGiven`; `ListPatients` filters by document number or name; `RecordConsent` handler and endpoint with `ReceptionStaff` policy.

## Work Unit Evidence

| Work unit | Focused test command | Result | Runtime harness | Rollback boundary |
|---|---|---|---|---|
| Patient search (4.9/4.11) | `dotnet test tests/Api.IntegrationTests --filter "FullyQualifiedName~Get_patients_by_document_number|FullyQualifiedName~Get_patients_by_name|FullyQualifiedName~Get_patients_by_unmatched"` | 3 passed | `dotnet test tests/Api.IntegrationTests --filter PatientEndpointTests` | Revert `ListPatients.cs`, `PatientEndpoints.cs` |
| Consent model + endpoint (4.10/4.11) | `dotnet test tests/Api.IntegrationTests --filter "FullyQualifiedName~Post_patient_without_consent|FullyQualifiedName~Post_patient_with_consent|FullyQualifiedName~Post_consent_records"` | 3 passed | `dotnet test tests/Api.IntegrationTests --filter PatientEndpointTests` | Remove `ConsentStatus.cs`, `RecordConsent.cs`, revert `Patient.cs`, `CreatePatient.cs`, `ListPatients.cs`, `PatientConfiguration.cs`, `PatientEndpoints.cs`, `Api.DependencyInjection.cs`, migration `20261012182606_AddPatientConsentColumns` |

## Verification summary

| Command | Result |
|---|---|
| `dotnet build MediCore.slnx` | Build succeeded (0 warnings, 0 errors) |
| `dotnet test tests/Domain.UnitTests` | 9 passed, 0 failed, 0 skipped |
| `dotnet test tests/Application.UnitTests` | 18 passed, 0 failed, 0 skipped |
| `dotnet test tests/Architecture.Tests` | 3 passed, 0 failed, 0 skipped |
| `dotnet test tests/Api.IntegrationTests` | 31 passed, 0 failed, 0 skipped |

## Commits

- `530b189` test(patient): add RED tests for patient search and consent (tasks 4.9, 4.10)
- `418f9e9` feat(patient): implement search, consent model and consent endpoint (task 4.11)

## Status

11/11 tasks complete for Phase 4 (4.1–4.11). Ready for Phase 5 (Agenda Management).

## Phase 5 / Slice A — Weekly Agenda Template (tasks 5.1–5.2)

- [x] 5.1 RED: Test weekly availability blocks define bookable weekday windows [Spec: agenda-management/Weekly template].
  - Evidence: `dotnet test tests/Domain.UnitTests --filter AgendaTests` → 3 passed.
  - Implementation: `AgendaTests` verifies `Agenda` holds `AvailabilityBlock` collection and `AvailabilityBlock.Contains(TimeOnly)` is inclusive of start, exclusive of end.
- [x] 5.2 GREEN: Add `Agenda`, `AvailabilityBlock` entities, `PUT /api/professionals/{id}/agenda`.
  - Evidence: `dotnet build MediCore.slnx` → 0 warnings, 0 errors.
  - Evidence: `dotnet ef migrations add AddAgendaSchema` generated `20261012211828_AddAgendaSchema`.
  - Evidence: `dotnet test tests/Application.UnitTests --filter UpdateAgendaTests` → 5 passed (create, replace, overlap validation, invalid range, not-found).
  - Evidence: `dotnet test tests/Api.IntegrationTests --no-build` → 31 passed, 0 failed, 0 skipped (container startup ~2m 26s).
  - Implementation:
    - Domain: `Agenda` (unique `ProfessionalId`, `MaxSobreturnosPerDay`, `Blocks`) and `AvailabilityBlock` (`DayOfWeek`, `Start`, `End`, `Contains`).
    - Application: `UpdateAgenda.Handler` creates or replaces agenda, validates `Start < End`, rejects same-day overlapping blocks.
    - API: `PUT /api/professionals/{id}/agenda` under `AdminOnly` policy; endpoint maps string `DayOfWeek`/`Start`/`End` to `TimeOnly`/`DayOfWeek`.
    - Persistence: `AgendaConfiguration` + `AvailabilityBlockConfiguration`; unique index on `(AgendaId, DayOfWeek, Start, End)`; cascade delete blocks on agenda removal.

## Work Unit Evidence

| Work unit | Focused test command | Result | Runtime harness | Rollback boundary |
|---|---|---|---|---|
| Domain entity + block containment (5.1) | `dotnet test tests/Domain.UnitTests --filter AgendaTests` | 3 passed | N/A (pure domain) | Remove `Agenda.cs`, `AvailabilityBlock.cs`, `AgendaTests.cs` |
| Update agenda handler (5.2) | `dotnet test tests/Application.UnitTests --filter UpdateAgendaTests` | 5 passed | `dotnet test tests/Api.IntegrationTests --no-build` (container) | Remove `UpdateAgenda.cs`, `UpdateAgendaTests.cs` |
| EF config + migration (5.2) | `dotnet build MediCore.slnx` | 0 errors | `dotnet ef migrations add AddAgendaSchema` (design-time) | Remove `AgendaConfiguration.cs`, `AvailabilityBlockConfiguration.cs`, migration `20261012211828_AddAgendaSchema`, revert `AppDbContext.cs`, `IApplicationDbContext.cs`, `AppDbContextModelSnapshot.cs` |
| API endpoint + DI wiring (5.2) | `dotnet test tests/Api.IntegrationTests --no-build` | 31 passed | `dotnet run --project src/Api` (not run due to no SQL Server locally) | Remove `AgendaEndpoints.cs`, revert `Api.DependencyInjection.cs` |

## Verification summary

| Command | Result |
|---|---|
| `dotnet build MediCore.slnx` | Build succeeded (0 warnings, 0 errors) |
| `dotnet test tests/Domain.UnitTests` | 12 passed, 0 failed, 0 skipped |
| `dotnet test tests/Application.UnitTests` | 23 passed, 0 failed, 0 skipped |
| `dotnet test tests/Architecture.Tests` | 3 passed, 0 failed, 0 skipped |
| `dotnet test tests/Api.IntegrationTests` | 31 passed, 0 failed, 0 skipped |

## Commits

- (pending) test(agenda): add RED tests for weekly availability blocks (task 5.1)
- (pending) feat(agenda): add Agenda/AvailabilityBlock entities, EF config, migration and PUT endpoint (task 5.2)

## Status

2/2 tasks complete for Phase 5 / Slice A (5.1–5.2). Ready for Phase 5 / Slice B (tasks 5.3–5.4) or verify.

## Workload / PR Boundary

- Mode: auto-chain / feature-branch-chain
- Current PR: #5a — Weekly agenda template (targets `feature/core-clinical`)
- Boundary: adds `Agenda` + `AvailabilityBlock` domain model, EF schema, and `PUT /api/professionals/{id}/agenda`; excludes exceptions (5.3–5.4) and sobreturno/availability query (5.5–5.6)
- Review budget impact: ~1,500 authored lines; ~850 are generated EF migration Designer. Cannot split migration without losing schema integrity. Recommend `size:exception` for migration-generated bulk or accept the full slice as-is per auto-chain.
