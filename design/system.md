# MediCore HMS — design system

Status: **synchronized from a scaffold, not a brand.** The only source in the project is
`frontend/src/index.css`, which is untouched Vite starter CSS (a purple accent and a centred
1140 px column). Nothing here was chosen for a hospital product, so this document records the
truth of today rather than a design intent. The redesign artboards in `design/hms-redesign/`
propose replacement tokens; adoption happens through code, and this file must be re-synced
when that lands.

## Colour

| Token | Value | Usage | Source |
|---|---|---|---|
| `--text` | `#6b6375` | body copy (light) | `frontend/src/index.css` |
| `--text-h` | `#08060d` | headings (light) | `frontend/src/index.css` |
| `--bg` | `#fff` | app background (light) | `frontend/src/index.css` |
| `--border` | `#e5e4e7` | dividers, the `#root` inline border | `frontend/src/index.css` |
| `--code-bg` | `#f4f3ec` | inline code | `frontend/src/index.css` |
| `--accent` | `#aa3bff` | links and accents (light) | `frontend/src/index.css` |
| `--accent-bg` | `rgba(170,59,255,.1)` | accent wash | `frontend/src/index.css` |
| `--accent-border` | `rgba(170,59,255,.5)` | accent outline | `frontend/src/index.css` |
| `--shadow` | `0 10px 15px -3px rgba(0,0,0,.1), 0 4px 6px -2px rgba(0,0,0,.05)` | the scaffold's only elevation | `frontend/src/index.css` |
| dark: `--text` / `--text-h` / `--bg` | `#9ca3af` / `#f3f4f6` / `#16171d` | `prefers-color-scheme: dark` | `frontend/src/index.css` |
| dark: `--accent` | `#c084fc` | dark accent | `frontend/src/index.css` |

There is **no** success, warning, danger, or info colour in the project, which is why status
badges and validation messages currently have no visual language. There is no brand colour.

## Typography

| Token | Value | Usage | Source |
|---|---|---|---|
| `--sans` | `system-ui, 'Segoe UI', Roboto, sans-serif` | everything | `frontend/src/index.css` |
| `--heading` | same stack | h1/h2 | `frontend/src/index.css` |
| `--mono` | `ui-monospace, Consolas, monospace` | code, `.counter` | `frontend/src/index.css` |

Scale in use: `h1` 56px / -1.68px tracking (36px under 1024px), `h2` 24px / -0.24px (20px under
1024px), body 18px at 145% line-height with 0.18px letter-spacing (16px under 1024px). No small,
caption, or numeric scale exists beyond these.

## Spacing, radius, shadow

- Spacing: **no scale defined.** Only ad-hoc `margin: 32px 0` on `h1` and `8px` under `h2`.
- Radius: **none defined** (`4px` only on `code`).
- Shadows: the single `--shadow` token above.

## Component rules
- Layout: `#root` is `width: 1126px; max-width: 100%; margin: 0 auto; border-inline: 1px solid
  var(--border); text-align: center`. Pages therefore render as a centred column with centred
  text — the main reason the product reads as a scaffold rather than a dashboard.
- Buttons, inputs, selects, tables and cards: **no rules exist**; every screen inherits browser
  defaults, so tables have no borders or alignment and forms have no field styling.
- Feedback: errors are plain `<p role="alert">` and `<span>` text; there is no toast, badge, or
  inline-field treatment.
- Navigation: one horizontal `<nav>` of plain `<Link>`s inside `frontend/src/components/Layout.tsx`.

## Gaps the redesign must close
1. A real layout shell (sidebar or topbar) replacing the centred column.
2. Semantic colour set: brand, surface, border, and the four status colours.
3. A type scale with a numeric/tabular style for times and figures.
4. Field, table, badge, card and empty/error state components.
5. Consistent focus-visible styling and a dark-scheme decision (today it follows the OS).
