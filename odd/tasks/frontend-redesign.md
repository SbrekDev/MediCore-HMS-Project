# frontend-redesign — professional UI for MediCore HMS

The frontend renders as an untouched Vite scaffold: a 1126 px centred column with `text-align: center`, a purple `#aa3bff` accent and browser-default tables and fields. This feature replaces it with a clinical dashboard built on an explicit design system, following the chosen design direction.

## Specs

- **S1** (user request, verbatim): "bien, ahora necesitamos mejorar el front porque esta bastante feo y poco profesional, usa este template de ejemplo, https://themeforest.net/item/medlink-hospital-management-dashboard-ui-figma-template/62264190 para realizar algo similar. podrias usar tailwind, pero lo dejo a tu criterio"
- **S2** (user decision, verbatim answer "1"): "me gusta la opcion 1 pero con la paleta de colores de la op 3" — direction `01-clinical-sidebar` layout with the palette of `03-clinical-console` (ink `#0c1517`, teal `#0d9488`, canvas `#f2f5f4`). Reference artboard: `design/hms-redesign/04-chosen-teal-sidebar.html`.
- **S3** (user decision, verbatim answer "2"): "si" to Tailwind.
- **S4** (user decision, verbatim answer "3"): "si" to adding a today/metrics screen with real data.

## Tasks

| ID | Task | Specs | Route | Commit |
|---|---|---|---|---|
| T1 | Tailwind v4 + design tokens + app shell (sidebar, topbar, route-aware nav with role visibility) | S2, S3 | worker + independent verify | `c13009e` + follow-up |
| T2 | Metrics contract `GET /api/dashboard/summary?from=&to=` + the Today screen | S4 | worker (backend contract + page) | `9b9f5f2` |
| T3 | Patients screen restyle (list, form, states) | S2 | inline (parent) | — |
| T4 | Professionals screen restyle | S2 | inline (parent) | — |
| T5 | Agenda screen restyle (template editor, exceptions) | S2 | inline (parent) | — |
| T6 | Booking + My appointments restyle | S2 | inline (parent) | — |
| T7 | Login screen restyle | S2 | inline (parent) | — |

Constraint carried by every task, not a new requirement: this is a restyle, so every existing behaviour test must stay green (frontend 75, backend 109 at the start of the work). A visual change that breaks a behaviour test is a regression.

### Pinned contract for T2 (before any code)

`GET /api/dashboard/summary?from=&to=` — policy `ClinicalStaff`. `from` and `to` are **required instants with an explicit offset**, parsed by the same `InstantFormats` rule as the appointments list, so the client owns what "today" means and the server never guesses a zone. Missing or offset-less bound → `400 Dashboard.RangeRequired`; `to <= from` → `400 Dashboard.InvalidRange`. Response:

```json
{
  "from": "2026-11-12T00:00:00-03:00",
  "to": "2026-11-13T00:00:00-03:00",
  "appointments": { "total": 6, "confirmed": 3, "completed": 2, "noShow": 1, "cancelled": 0, "sobreturnos": 1 },
  "patients": { "total": 128, "addedInRange": 3 },
  "professionals": { "total": 9, "withTemplate": 7 }
}
```

Counts are scoped to `from <= start < to`. `professionals.withTemplate` counts professionals that have an `Agenda` with at least one block. No KPI may be invented client-side: if a number is not in this payload, the Today screen does not show it.

## Log

- **L1** (user request, verbatim): "bien, ahora necesitamos mejorar el front porque esta bastante feo y poco profesional, usa este template de ejemplo, https://themeforest.net/item/medlink-hospital-management-dashboard-ui-figma-template/62264190 para realizar algo similar. podrias usar tailwind, pero lo dejo a tu criterio"
- **L2** (user decisions, verbatim): "1 - me gusta la opcion 1 pero con la paleta de colores de la op 3 / 2- si / 3- si" → layout of `01`, palette of `03`, Tailwind yes, Today screen with real metrics yes.
- **L3** (2026-11-09, session 5, design phase): design system sync found only Vite scaffold tokens in `frontend/src/index.css` (documented in `design/system.md`, rendered in `design/system.html`). Four artboards written in `design/hms-redesign/`: `01-clinical-sidebar.html` (dark sidebar, KPI row, schedule table), `02-schedule-first.html` (light topbar, week grid as the hero), `03-clinical-console.html` (icon rail, master–detail), and `04-chosen-teal-sidebar.html` — the chosen handoff artboard, 01's structure with 03's ink/teal palette; the other three remain as alternatives. Decision recorded with the tradeoff of each. Tailwind v4 (`@tailwindcss/vite` + `@theme`) is the styling route, accepted as a build dependency.
- **L4** (2026-11-09, session 5, T1 done): Delegated to `gentle-ai-worker`; the shell is the artboard's structure in Tailwind utilities over the `@theme` tokens, with the old scaffold CSS deleted. Parent-verified: 75/75 then 82/82 after the fixes below, build OK, lint unchanged. **Independent verification: R1-R7 MET, no blockers**, and it raised four things worth acting on. Fixed in the follow-up commit: (A1) the "Today" nav item pointed at `/`, which redirected to `/patients` — a dead end for a Professional, who got the unauthorized screen; the index route is now role-aware (Admin/Receptionist → `/patients`, anyone else → `/my-appointments`), pinned by `frontend/src/test/ShellNavMatrix.test.tsx`, which also proves no visible link leads to a page the role cannot open; (A3) group labels rendered unconditionally, so a role saw empty "Front desk"/"Clinical"/"Administration" headings — they now render only when their group has items; the R1 advisory that `tailwindcss` and `@tailwindcss/vite` are build-time only (moved to `devDependencies`); and the scaffold leftovers (`src/App.css`, `src/assets/*`, `index.html` title "frontend"). One adopted test was dropped rather than shipped: `DesignTokens.test.ts` read `src/index.css` through `node:fs`, which `tsc -b` cannot type since the app tsconfig has no Node types — the navigation matrix is the test that carries the value. Remaining advisories accepted: `/my-appointments` is openable by Admin/Receptionist with no link for them (by design, the page explains the unlinked state), and nav visibility for Patients/Booking/Professionals is covered only indirectly by the matrix test's exact-link assertions.
- **L5** (2026-11-09, session 5, T1 follow-up verification): A second independent verifier ran on the delta only (`c13009e..ea90b56`) instead of repeating the whole slice: **D1-D5 MET, no blockers.** It proved the landing fix by replaying the motivating case at the parent commit (`expected '/patients' to be '/my-appointments'`, "Professional landed on unauthorized /patients"), mutation-checked the new matrix test (restoring the unconditional redirect, and making Agenda visible to roles that cannot open it, both make it fail), and proved the dependency move safe: `npm ci --offline` then `npm run build` produces byte-identical dist hashes, and a `--omit=dev` install already failed at `tsc` before the move, so no new deployment failure mode exists (and nothing in the repo does that). Its advisories were closed here: the `react(only-export-components)` lint warning my redirect introduced is gone (the component moved to `routes/IndexRedirect.tsx`), the role policy now lives in one module (`routes/rolePolicies.ts`) instead of being restated in the redirect, and its `RoleLandingMatrix` test replaced my weaker one-role reachability case so all four accounts plus the anonymous visitor are covered. Frontend now at **93 tests** in 15 files, build OK, lint back to the single pre-existing warning. Accepted as-is: `/my-appointments` is openable by Admin/Receptionist with no link for them (the page explains the unlinked state), and `PAGE_TITLES['/']` stays unreachable until T2 creates the real Today route.
- **L6** (2026-11-09, session 5, T2 done): Delegated to `gentle-ai-worker`. Contract implemented exactly as pinned: `GET /api/dashboard/summary?from=&to=`, `ClinicalStaff`, explicit-offset instants via the same parsing rule as the appointments list, `Dashboard.RangeRequired` / `Dashboard.InvalidRange`, and the six/one/two counts. Counts are database-side (`CountAsync`) — nine round trips per page load, acceptable for a dashboard and noted as an efficiency advisory rather than a defect. Frontend: `features/today/`, route `today` on `CLINICAL_STAFF`, the shell's Today item finally points at a real route, and `IndexRedirect` now lands everyone on `/today` instead of guessing between patients and my-appointments. KPIs render `—` when a value is missing instead of inventing one, and the cap is deliberately absent because the payload does not carry it. Parent-verified: dashboard filter 10/10, full backend **119/119**, frontend **98/98** in 16 files, build OK, lint at the single pre-existing warning. Deviations declared by the worker: `frontend/src/App.test.tsx` was edited although it sits outside the listed surfaces, because the landing change makes the old root-redirect assertion wrong; the change is a strict consequence and the suite is green. The dashboard's `from`/`to` echo ASP.NET's `DateTimeOffset` serialization (with fractional seconds) rather than the trimmed example in the contract; shape and names match.
