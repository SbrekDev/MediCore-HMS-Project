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

## Status

8/8 tasks complete for Phase 3 (3.1–3.8). Ready for Phase 4 (Patient Management).
