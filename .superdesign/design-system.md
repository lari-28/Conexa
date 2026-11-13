# CONEXA Design System — Circuit Black Racing Chrome

## Product and audience

CONEXA is a responsive event-networking product for attendees, exhibitors, organizers, and platform administrators. Core experiences include event overview, participant and company discovery, profiles, event discussion, moderation, and event administration. Keep the interface professional, social, and easy to scan; motorsport styling is a visual language, not a literal race-control console.

## Visual direction

Use the **Circuit Black Racing Chrome** direction: a precise, high-contrast dark interface with carbon-black surfaces, polished cool-chrome details, restrained racing-red signals, and fine circuit-inspired linework. The result should feel engineered and premium, balancing trackside energy with practical product usability.

- Use deep black and graphite as the dominant surfaces; distinguish layers with subtle tonal shifts and hairline borders.
- Use silver/chrome for structural labels, dividers, metallic edge details, and quiet iconography. Metallic gradients are allowed only as subtle highlights, never as large glowing backgrounds.
- Reserve one vivid racing red for primary actions, selected states, and a few live/event indicators. Do not use competing neon accents.
- Use sparse technical motifs: thin circuit traces, small telemetry labels, registration marks, and restrained check patterns. Keep decoration out of reading and input areas.
- Favor crisp geometry, compact labels, clear alignment, deliberate negative space, and one strong editorial heading.
- Do not use glassmorphism, purple/blue glows, gradients as background fills, distressed textures, stock photography, or oversized motorsport imagery.
- Keep the product identity as the text wordmark “CONEXA”; do not invent a pictorial logo or imitate any racing team, championship, or automotive brand.

The target is a polished, desktop-first component-library page that documents the actual shared UI in Portuguese. Organize it as a product UI kit with a compact category rail and a spacious catalog canvas. Show all reusable patterns together: navigation shell, buttons and states, fields, avatars, badges, cards, alerts, loading/empty states, pagination, modal, participant card, and forum post card. Use realistic CONEXA event content. Examples should look like functional product UI rather than code listings. Keep the page responsive and readable on mobile.

## Tokens

### Color

- `canvas`: `#0B0D0F`
- `surface`: `#111417`
- `surface-raised`: `#191D21`
- `surface-inset`: `#080A0C`
- `ink`: `#F2F4F5`
- `muted`: `#A1A8AE`
- `subtle`: `#747C83`
- `border`: `#343A40`
- `chrome`: `#D7DCE0`
- `chrome-shadow`: `#858D94`
- `brand`: `#E23B32`
- `brand-strong`: `#BE2C26`
- `brand-soft`: `#3A1918`
- `danger`: `#F0645A`
- `danger-soft`: `#351716`
- `success`: `#71C895`
- `success-soft`: `#15291D`
- `warning`: `#E6B45A`
- `warning-soft`: `#302718`
- `info`: `#79BBD1`
- `info-soft`: `#14262C`

Use red as a selective signal, not as a large surface. Use chrome or white for text and neutral borders. Keep muted text readable against its surface.

### Typography

- Use `IBM Plex Sans` (fallback: `Inter`, `ui-sans-serif`, `system-ui`) for interface text.
- Use `IBM Plex Mono` (fallback: `ui-monospace`, `SFMono-Regular`, `monospace`) for small telemetry labels, IDs, timestamps, and technical metadata only.
- Page title: 30–34px, semibold or bold; section title: 18–22px, semibold.
- Body: 14–16px, regular; labels and metadata: 11–13px, medium, with restrained letter spacing.
- Use tabular numerals for counts, event times, and IDs.

### Spacing, shape, and elevation

- Use a 4px base spacing scale: 4, 8, 12, 16, 24, 32, 48, 64px.
- Main desktop content max width: 1280px; use a 12-column grid for the catalog.
- Control height: 44px minimum; compact controls may be 36px when clearly secondary.
- Inputs and buttons: 8px radius; cards and panels: 12px; status pills: fully rounded.
- Use crisp hairline chrome/graphite borders as the default separation. Reserve subtle shadows for elevated menus and modals; avoid diffuse glows.
- Focus state: a visible 2px chrome or racing-red ring with offset, chosen for contrast against the current surface.

## Component behavior and accessibility

- Buttons: primary, secondary, quiet, destructive, disabled, and loading examples. Primary may use racing red; ensure clear hover/focus/disabled states.
- Fields: labeled text input, search, select, and textarea; show focus, helper, and validation examples.
- Status: semantic success, warning, info, and error tones; never communicate status by color alone.
- Cards: distinguish selectable directory cards from static content cards using border, typography, and interaction state rather than glow.
- Modal: centered on desktop and bottom-sheet-like on mobile, with a clear close affordance.
- Preserve keyboard focus visibility, semantic headings, labeled controls, and touch targets of at least 44px.
- Use 120–180ms transitions sparingly; respect reduced-motion preferences. No flashing or fast animation.

## Responsive layout

- Desktop reference: 1440px wide; compact left rail for library sections and a fluid content canvas.
- Mobile reference: 390px wide; collapse the library rail into a top selector, stack examples, and keep app navigation touch-friendly.
- The source app uses a 768px layout breakpoint; keep responsive behavior clear around that width.

## Source context precedence

The repository's `src/index.css` and existing React components document content, semantics, and behaviors, but their styling tokens are not the target style. This document is authoritative for the new visual direction. Keep source labels and product concepts accurate while restyling the visual system.
