# MediCore HMS — design system

Status: **current.** Synchronized from `frontend/src/index.css` after the redesign shell landed (Tailwind v4 `@theme`). The values below are the project's real tokens; the earlier Vite scaffold (purple `#aa3bff`, centred 1126 px column) is gone.

## Colour

| Token | Value | Usage |
|---|---|---|
| `--color-ink` / `--color-nav` | `#0c1517` | sidebar and app chrome |
| `--color-nav-hover` | `#16262a` | sidebar item hover |
| `--color-nav-active` | `#0d9488` | current sidebar item |
| `--color-canvas` | `#f2f5f4` | page background |
| `--color-card` | `#ffffff` | cards, tables, dialogs |
| `--color-line` / `--color-line-strong` | `#dde5e3` / `#ccd8d5` | dividers, input borders |
| `--color-text` / `--color-muted` / `--color-muted-2` | `#12221f` / `#5c6f6c` / `#8d9d9a` | body, secondary, tertiary text |
| `--color-brand` / `--color-brand-ink` / `--color-brand-wash` | `#0d9488` / `#0b7c72` / `#e3f3f1` | primary action, links, selection |
| `--color-ok` / `--color-ok-wash` | `#1a7f43` / `#e7f4ec` | completed, success |
| `--color-warn` / `--color-warn-wash` | `#a15c07` / `#fcf2e2` | sobreturno, warning |
| `--color-danger` / `--color-danger-wash` | `#b42318` / `#fdeceb` | no-show, destructive, errors |
| `--color-neutral` / `--color-neutral-wash` | `#5a6b68` / `#eceff0` | cancelled, muted status |

Status colours exist as a pair (ink on wash) and are always used together for badges and pills. There is no dark-scheme token set: the app is light-only by decision.

## Typography

`--font-sans`: `system-ui, 'Segoe UI', Roboto, sans-serif` — everything.
`--font-mono`: `ui-monospace, Consolas, monospace` — times, document numbers, counters (always with `tabular-nums`).

Scale in use (Tailwind defaults, no custom overrides): page title `text-[17px]`/`text-2xl` depending on screen, card headings `text-sm font-semibold`, body `text-sm`, captions `text-[11px]`. Uppercase micro-labels use `text-[11px] tracking-wide`.

## Radius, shadow, spacing

- `--radius-card` `14px` for cards and panels; `--radius-control` `9px` for buttons and inputs; `999px` for pills.
- `--shadow-card`: `0 1px 2px rgba(9,26,24,.05), 0 8px 24px -12px rgba(9,26,24,.20)`.
- Spacing: Tailwind default scale, in practice `gap-4`/`gap-5` between sections, `p-4`/`p-6` inside cards.

## Component rules
- Shell: fixed 248px sidebar (`lg:` breakpoint and up, 72px icon rail below), 64px topbar, content on `canvas` with `p-6`.
- Nav: icons are inline SVG 16–19px, stroke 1.7, `aria-hidden`; the active item uses `aria-current="page"` and the brand wash/underline.
- Cards: one `border border-line`, `rounded-card`, `bg-card`, `shadow-card`; never nested more than one level.
- Tables: uppercase `text-[11px]` headers on `bg-card`, rows separated by `border-line`, hover `bg-canvas`.
- Forms: label above field, `rounded-control`, `border-line`, focus ring in brand at 45% alpha, errors as `text-danger` plus `role="alert"`.
- Feedback: page-level problems use `role="alert"`; inline field errors sit under the field.

## Gaps this slice did not close
Page contents (patients, professionals, agenda, booking, my appointments, login) still use browser-default markup; tasks T3–T7 restyle them against these tokens.
