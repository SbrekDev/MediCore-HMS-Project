# Proposal: core-clinical — Patients, Professionals, Agendas & Appointments

## Intent

MediCore HMS (demo: Sanatorio Los Ceibos, fictional) cannot manage its clinical front desk digitally: who patients are, which professionals practice which specialties, and when appointments happen. This change delivers that foundation — patient/professional administration, specialty catalog, agendas, and appointment scheduling with role-based access and health-data compliance basics. Every later module (EHR, billing, portal) builds on it.

## Confirmed Business Decisions

Binding inputs from the product owner (resolve exploration open questions 1–8):

1. Appointments booked by reception staff only; patient self-booking deferred to the portal module.
2. Appointment born Confirmed. States: Confirmed / Cancelled / Completed / NoShow. No "Requested".
3. Sobreturnos: Receptionist/Admin only, with configurable daily cap per agenda.
4. Single site; architecture must not block future multi-site.
5. Seed coverages: 3–4 fictional insurers + "Particular" (self-pay).
6. Consultation default 30 min per specialty, overridable per professional-specialty.
7. Booking window: ≥2h before start, ≤90 days ahead.
8. Cancellation allowed up to 24h before; later recorded as "late cancellation".
9. Weekly agenda template per professional + per-date exceptions (holidays, vacations, leave).
10. License: one national (MN) + list of provincial (MP).

## Scope

### In Scope
- Patients: identity documents (DNI/passport/etc.), contact, address, coverage history, minors with responsible.
- Professionals: multi-specialty, MN/MP licenses.
- Specialty catalog (seeded).
- Agendas: weekly templates + per-date exceptions.
- Appointments: book, reschedule, cancel (incl. late), complete, no-show, sobreturnos.
- Auth: ASP.NET Core Identity + JWT; roles Admin, Receptionist, Professional.
- Audit trail + consent basics (Ley 25.326 / 26.529).

### Out of Scope
EHR/clinical records · emergency/guardia · inpatient · diagnostics/lab/imaging · pharmacy · billing/invoicing · patient portal & self-booking · multi-site.

## Capabilities

### New Capabilities
- `patient-management`: registration, documents, coverages, minors/responsible.
- `professional-management`: professionals, MN/MP licenses, specialty assignments.
- `specialty-catalog`: seeded specialties with default durations.
- `agenda-management`: weekly templates + per-date exceptions.
- `appointment-scheduling`: booking lifecycle, sobreturnos, overlap/window rules.
- `identity-access`: Identity + JWT, role policies.
- `audit-compliance`: audit trail + consent capture.

### Modified Capabilities
None — greenfield; no existing specs.

## Approach

Clean Architecture (Domain/Application/Infrastructure/API), strict TDD. Scheduling = exploration option B: `AvailabilityBlock` defines bookable time; `Appointment` carries explicit Start + DurationMinutes; a pure domain `AppointmentScheduler` enforces overlap, block containment, and booking windows. Double-booking blocked by a serializable/UPDLOCK transaction on the professional's agenda window. Auth: Identity (SQL Server) + JWT — short-lived access token in memory, refresh token in httpOnly SameSite cookie; role claims enforced via authorization policies. Catalogs seeded idempotently.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Domain/` | New | Entities + `AppointmentScheduler` domain service |
| `src/Application/` | New | Use cases for all capabilities |
| `src/Infrastructure/` | New | EF Core + migrations, Identity, JWT |
| `src/Api/` | New | Endpoints, auth, role policies |
| `frontend/` | New | React: login, patients, professionals, agenda, booking |
| SQL Server schema | New | All tables via EF migrations |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Double-booking under concurrency (SQL Server lacks exclusion constraints) | High | Serializable txn/UPDLOCK on agenda window + dedicated concurrency tests; top design-phase focus |
| 7 capabilities vs 400-line review budget | High | Chained PRs per capability; sdd-tasks forecasts size |
| Non-DNI identities (foreigners) | Med | Composite DocumentType+Number uniqueness |
| Health-data compliance (Ley 25.326/26.529) | Med | Audit + consent as first-class requirements |
| Identity stack lock-in | Med | Rationale recorded in design; migrations from day one |

## Rollback Plan

Greenfield, no production data: revert the PR(s) and drop the dev database. Seeds are idempotent and re-runnable. Capabilities ship as separate PRs, so partial rollback = reverting only later PRs.

## Dependencies

- .NET 10 SDK, SQL Server, Node toolchain (stack already decided).
- Fictional seed data: specialty catalog + insurer names.

## Success Criteria

- [ ] Receptionist registers a patient (incl. minor with responsible) and books/reschedules/cancels an appointment end-to-end.
- [ ] Concurrent double-booking is impossible (proven by tests).
- [ ] Sobreturnos restricted to Receptionist/Admin and capped per agenda/day.
- [ ] Professionals mark only their own appointments Completed/NoShow.
- [ ] Role policies enforced per endpoint; JWT login + refresh works.
- [ ] Audit entries and consent recorded on patient/appointment mutations.
- [ ] `dotnet test` and `npm test` green.
