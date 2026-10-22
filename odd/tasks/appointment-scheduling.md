# appointment-scheduling — core-clinical Phase 6 (tasks 6.1–6.11)

ODD feature document for Phase 6 of the OpenSpec change `core-clinical`. Master checklist: `openspec/changes/core-clinical/tasks.md` (Phase 6). Full scenario text: `openspec/changes/core-clinical/specs/appointment-scheduling/spec.md` — workers read it by reference; scenario titles below are verbatim.

## Specs

Quoted verbatim from `openspec/changes/core-clinical/specs/appointment-scheduling/spec.md`:

- **S1** (Booking by reception staff): "The system MUST allow booking appointments for authorized staff (Receptionist/Admin) only. A new appointment SHALL be born in the Confirmed state. States are Confirmed, Cancelled, Completed, NoShow." — scenario: "Receptionist books a confirmed appointment".
- **S2** (No professional double-booking): "The system MUST NOT allow a professional to hold two appointments whose time ranges overlap." — scenarios: "Overlapping professional appointment rejected" ("GIVEN a professional already has an appointment 10:00–10:30 WHEN a receptionist books the same professional at 10:15 THEN the booking is rejected"), "Adjacent appointments allowed" ("GIVEN a professional has an appointment 10:00–10:30 WHEN a receptionist books the same professional at 10:30 THEN the booking succeeds (no overlap)").
- **S3** (No patient overlap): "The system MUST NOT allow a patient to hold two appointments at the same time, across any professional." — scenario: "Patient overlap rejected".
- **S4** (Specialty validity): "The system MUST reject an appointment whose specialty is not held by the chosen professional." — scenario: "Unheld specialty rejected".
- **S5** (Booking window): "The system MUST reject a booking whose start is less than 2 hours from now or more than 90 days ahead." — scenarios: "Too soon rejected" (1 hour from now), "Too far rejected" (91 days), "Within window accepted" (3 days).
- **S6** (Sobreturno): "The system MUST allow flagged sobreturno appointments only for Receptionist/Admin, only beyond normal block capacity, and within the agenda's daily sobreturno cap." — scenarios: "Sobreturno by authorized role" (IsSobreturno=true), "Sobreturno by unauthorized role rejected", "Sobreturno cap exceeded rejected".
- **S7** (Duration resolution): "The system SHALL use the professional-specialty duration override when present, otherwise the specialty default." — scenarios: "Duration from specialty default" (30 min), "Duration from override" (45 min).
- **S8** (Concurrent double-booking prevented): "GIVEN two receptionists attempt to book the same professional's slot simultaneously WHEN both requests commit THEN exactly one succeeds and the other is rejected".
- **S9** (Reschedule): "The system MUST allow rescheduling an appointment, revalidating window, overlap, and specialty rules against the new time." — scenario: "Reschedule to free slot" (status stays Confirmed).
- **S10** (Cancellation and late cancellation): "The system MUST allow cancellation up to 24 hours before start. A cancellation within 24 hours SHALL be recorded as a late cancellation." — scenarios: "Normal cancellation", "Late cancellation recorded".
- **S11** (Completion and no-show): "The system MUST allow a professional to mark only their own appointments as Completed or NoShow. Marking NoShow SHALL increment the patient's no-show counter." — scenarios: "Professional completes own appointment", "Professional marks another's appointment" (rejected), "No-show increments counter".

## Tasks

| ID | Task (openspec 6.x) | Specs | Route | Commit |
|---|---|---|---|---|
| T1 | 6.1 RED: Test `AppointmentScheduler` rejects professional overlap and allows adjacent appointments | S2 | inline (parent) | `00fffc5` |
| T2 | 6.2 GREEN: Implement pure domain `AppointmentScheduler.Schedule(SchedulingRequest, ScheduleContext)` | S2, S7 | inline (parent) | `633a3e7` |
| T3 | 6.3 RED: Test patient overlap across professionals rejected | S3 | inline (parent) | `316b235` |
| T4 | 6.4 RED: Test unheld specialty rejected | S4 | inline (parent) | `4abc09d` |
| T5 | 6.5 RED: Test 2-hour minimum and 90-day maximum booking window | S5 | inline (parent) | `2d444b9` |
| T6 | 6.6 RED: Test sobreturno allowed only for Receptionist/Admin, beyond capacity, within cap | S6 | inline/worker | pending |
| T7 | 6.7 GREEN: Add `Appointment` entity, `BookAppointmentHandler` with `UPDLOCK,HOLDLOCK` anchor locks, `POST /api/appointments` | S1, S7 | worker + independent verify; own commit (high risk: concurrency) | pending |
| T8 | 6.8 RED: Integration test concurrent double-booking → exactly one 201, one 409, one row | S8 | worker + independent verify; same high-risk unit as T7 | pending |
| T9 | 6.9 GREEN: Add reschedule endpoint revalidating all rules | S9 | inline/worker | pending |
| T10 | 6.10 RED: Test cancellation flags late when <24h, completion allowed only for own professional | S10, S11 | inline/worker | pending |
| T11 | 6.11 GREEN: Add `POST /api/appointments/{id}/{reschedule,cancel,complete,no-show}` and no-show counter increment | S9, S10, S11 | inline/worker | pending |

Risk notes: T7/T8 hit high-risk item 4 (concurrency — UPDLOCK/HOLDLOCK booking) → independent `gentle-ai-verify` + native review candidate (lenses: risk, resilience, reliability). T9–T11 add lifecycle behavior with role rules (item 2) → native review candidate (lenses: risk, reliability). RDD is ON; the user decided consent per candidate (2026-10-13). EF migrations in T7 count toward the ~400-line slice budget — disclose, do not split the migration from its entity.

## Log

- **L1** (user request, verbatim): "hola buenas, tenemos que avanzar con la siguiente fase, pero previamente quiero que analices y veamos como seguir, ya que no estoy usando mas gentle ai en opencode con sdd, ahora se actualizo a gentle-shell pero con odd, entonces quería que lo veas y me recomiendes como seria mejor continuar y si los perfiles te parece que estan bien o no"
- **L2** (user decisions, 2026-10-13): RDD on, consent per candidate. Explore profile cheapened to deepseek-v4.1-flash; worker efforts revisited for frontend phases 7–9. Start Phase 6 but this session implement ONLY tasks 6.1 and 6.2 ("no hagas todo el slice a, si podes solo hace la primera task, o las 2 primeras si es posible, ya que no se si me van a alcanzar los tokens para la slice completa y no quisiera que se corte a la mitad del laburo").
