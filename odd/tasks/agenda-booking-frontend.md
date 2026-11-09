# agenda-booking-frontend — core-clinical Phase 9 (tasks 9.1–9.4)

ODD feature document for Phase 9 of the OpenSpec change `core-clinical`. Master checklist: `openspec/changes/core-clinical/tasks.md` (Phase 9). Full scenario text: `openspec/changes/core-clinical/specs/agenda-management/spec.md` and `.../appointment-scheduling/spec.md` — workers read those files by reference; the quotes below are verbatim.

## Specs

Quoted verbatim from `openspec/changes/core-clinical/specs/agenda-management/spec.md`:

- **S1** (Weekly availability template): "The system MUST allow a weekly template per professional, composed of availability blocks (day of week, start time, end time). Bookings are only allowed within these blocks." — scenarios: "Define weekly template" ("GIVEN a professional / WHEN an admin defines blocks (e.g. Mon–Fri 08:00–12:00 and 14:00–18:00) / THEN the professional is bookable within those windows on the corresponding weekdays"), "Booking outside template rejected" ("GIVEN a professional has no block covering a requested time / WHEN a receptionist attempts to book that time / THEN the booking is rejected").
- **S2** (Per-date exceptions): "The system MUST support per-date exceptions that override the weekly template for a specific date (holiday, vacation, leave)." — scenarios: "Full-day exception" ("GIVEN a professional normally works Monday / WHEN an admin marks a specific Monday as a holiday (full-day exception) / THEN the professional is not bookable that date"), "Partial-day exception" ("GIVEN a professional normally works 08:00–18:00 / WHEN an admin sets a partial exception of 08:00–12:00 for a date / THEN only the remaining hours are bookable that date").
- **S3** (Sobreturno daily cap): "The system MUST allow a configurable daily sobreturno cap per agenda, and SHALL enforce it during scheduling." — scenario: "Cap configured and enforced" ("GIVEN an agenda with a daily sobreturno cap of 2 / WHEN two sobreturnos are already booked for that day / THEN a third sobreturno for that day is rejected").

Quoted verbatim from `openspec/changes/core-clinical/specs/appointment-scheduling/spec.md`:

- **S4** (Specialty validity): "The system MUST reject an appointment whose specialty is not held by the chosen professional." — scenario: "Unheld specialty rejected" ("GIVEN a professional holds only Cardiología / WHEN a receptionist books that professional for Pediatría / THEN the booking is rejected").
- **S5** (Booking window): "The system MUST reject a booking whose start is less than 2 hours from now or more than 90 days ahead." — scenarios: "Too soon rejected" (1 hour from now), "Too far rejected" (91 days), "Within window accepted" (3 days ahead).
- **S6** (Sobreturno): "The system MUST allow flagged sobreturno appointments only for Receptionist/Admin, only beyond normal block capacity, and within the agenda's daily sobreturno cap." — scenarios: "Sobreturno by authorized role", "Sobreturno by unauthorized role rejected", "Sobreturno cap exceeded rejected".
- **S7** (Reschedule): "The system MUST allow rescheduling an appointment, revalidating window, overlap, and specialty rules against the new time." — scenario: "Reschedule to free slot" (status stays Confirmed).
- **S8** (Cancellation and late cancellation): "The system MUST allow cancellation up to 24 hours before start. A cancellation within 24 hours SHALL be recorded as a late cancellation." — scenarios: "Normal cancellation", "Late cancellation recorded".
- **S9** (Completion and no-show): "The system MUST allow a professional to mark only their own appointments as Completed or NoShow. Marking NoShow SHALL increment the patient's no-show counter." — scenarios: "Professional completes own appointment", "Professional marks another's appointment" (rejected), "No-show increments counter".

## Tasks

| ID | Task (openspec 9.x) | Specs | Route | Commit |
|---|---|---|---|---|
| T1 | 9.1 RED: Test agenda block editor and exception UI | S1, S2, S3 | worker (own unit) | — |
| T2 | 9.2 GREEN: Add `features/agenda/` | S1, S2, S3 | worker (same unit as T1) | — |
| T3 | 9.3 RED: Test booking form validates window/overlap/specialty via mocked API | S4, S5, S6 | worker (own unit, same writer as T1/T2 is NOT allowed: dependent deliveries are not independent) | — |
| T4 | 9.4 GREEN: Add `features/booking/` with availability query and appointment lifecycle actions | S5–S9 | same unit as T3 | — |

Route note: T1/T2 (agenda editor) and T3/T4 (booking + lifecycle) are two independent units, each several files and over 100 changed lines → Writer rule fires; background subagents are off, so the two units run sequentially, each in its own commit and PR slice.

## Blockers and open decisions (found by exploration before writing any code)

The backend contracts phase 9 needs are **incomplete**. Evidence: `src/Api/Endpoints/AgendaEndpoints.cs`, `src/Api/Endpoints/AppointmentEndpoints.cs`, `src/Application/Agendas/GetAvailability.cs`.

1. **No read path for the agenda template or its exceptions.** Only `PUT /api/professionals/{id}/agenda`, `POST .../agenda/exceptions` and `DELETE .../agenda/exceptions/{exceptionId}` exist. A block editor cannot preload current blocks, `MaxSobreturnosPerDay`, or list exceptions to delete (no `exceptionId` source). `GET .../availability?date=` returns computed windows for one date, not raw blocks.
2. **No appointment list or detail endpoint.** Lifecycle actions (`reschedule`, `cancel`, `complete`, `no-show`) all need an appointment id, and nothing lets the UI discover ids.
3. **`cancel` does not return the late-cancellation flag** (`{ id, status }` only), while S8 requires recording it; the UI cannot display what the API does not return.
4. **`GET .../availability` returns availability windows, not free slots** — it does not subtract existing appointments (`GetAvailability.cs:69-79`), so a booking form cannot show real availability without either a new contract or client-side filtering against an appointment list (which is missing, see 2).
5. **Route guard mismatch**: `/agenda` is guarded by `CLINICAL_STAFF` (`frontend/src/routes/index.tsx:49-56`) but every agenda write is `AdminOnly` — a Professional opens the page and gets 403 on save.

Decision needed before T1: either (a) extend the backend with `GET .../agenda`, `GET .../appointments` (+ `isLate` on cancel, optionally free-slot availability) as a small backend unit before the frontend, or (b) scope T1/T2 to write-only editors and T3/T4 to actions over ids captured from the create response, deferring the read gap. Option (a) matches the specs' intent (an editable template implies reading it) but adds scope beyond tasks 9.1–9.4 and therefore beyond the quoted spec tasks.

## Log

- **L1** (user request, verbatim): "antes de arrancar la fase 9, quería saber si todo lo previo ya estaría concluido"
- **L2** (user request, verbatim): "hace lo que consideres mejor primero, lo mas ordenado y optimo y despues explicame el porque"
- **L3** (2026-11-08): Exploration of the phase 9 contracts completed before any code (read-only, delegated). Frontend conventions captured: features under `frontend/src/features/<domain>/{api.ts,types.ts,components/,pages/}`, feature tests under `frontend/src/test/<Page>.test.tsx` (phase-8 precedent), TanStack Query with array keys and `invalidateQueries`, error mapping in `onError` by `error.response.status` + `data.code`, RHF + zod with a standalone schema module plus pure exported helpers, `renderWithProviders` and `vi.spyOn(apiClient, ...)` for tests, `buildJwt(payload)` for role plumbing, `tsc -b` inside `npm run build` (type errors break the build), oxlint with rules-of-hooks as error. Stub pages already exist and are routed: `features/agenda/pages/AgendaPage.tsx` (CLINICAL_STAFF) and `features/booking/pages/BookingPage.tsx` (RECEPTION_STAFF). Backend endpoint and error-code inventory captured; five contract gaps listed in "Blockers and open decisions". No code written yet, by design — the gaps must be decided before T1.
