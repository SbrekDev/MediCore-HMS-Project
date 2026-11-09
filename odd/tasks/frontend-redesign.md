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
| T1 | Tailwind v4 + design tokens + app shell (sidebar, topbar, route-aware nav with role visibility) | S2, S3 | inline (parent) | — |
| T2 | Metrics contract `GET /api/dashboard/summary?from=&to=` + the Today screen | S4 | worker (backend contract + page) | — |
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
