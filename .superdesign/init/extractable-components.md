# Extractable components

## AppLayout

- Source: `src/components/layout/AppLayout.tsx`
- Category: layout
- Description: Authenticated shell with event switcher, desktop top navigation, mobile bottom navigation, account drawer, and routed content.
- Extractable props: active route/navigation state; selected event and user/role data when separating data from presentation.
- Hardcoded: CONEXA wordmark, Portuguese navigation labels, emoji icons, layout breakpoints and CSS classes.

## PageHeader

- Source: `src/components/common/index.tsx`
- Category: basic
- Description: Page title with optional subtitle and trailing action area.
- Extractable props: `title`, `subtitle`, `actions`.
- Hardcoded: heading structure and CSS classes.

## Button

- Source: `src/components/ui/index.tsx`
- Category: basic
- Description: Shared button with primary, secondary, danger, and ghost variants plus loading state.
- Extractable props: `variant`, `loading`, standard button attributes.
- Hardcoded: Portuguese loading label and CSS variant naming.

## Form fields

- Source: `src/components/ui/index.tsx`
- Category: basic
- Description: Labeled input, textarea, and select with optional hint/error text.
- Extractable props: labels, options, placeholder, errors/hints, standard HTML attributes.
- Hardcoded: field markup, default textarea rows, CSS classes.

## Avatar

- Source: `src/components/ui/index.tsx`
- Category: basic
- Description: Circular image or initials fallback.
- Extractable props: `name`, `url`, `size`.
- Hardcoded: initials generation and CSS classes.

## Card

- Source: `src/components/ui/index.tsx`
- Category: basic
- Description: Surface container optionally rendered as keyboard-accessible clickable card.
- Extractable props: `children`, `onClick`, `className`.
- Hardcoded: CSS classes and Enter key activation.

## Badge

- Source: `src/components/ui/index.tsx`
- Category: basic
- Description: Compact semantic label with neutral, brand, success, warning, or danger tone.
- Extractable props: `children`, `tone`.
- Hardcoded: CSS tone styles.

## Pagination

- Source: `src/components/common/index.tsx`
- Category: basic
- Description: Previous/next controls with current page count.
- Extractable props: `page`, `totalPages`, `onChange`.
- Hardcoded: Portuguese button and status text.

## ParticipantCard

- Source: `src/features/participants/ParticipantCard.tsx`
- Category: basic
- Description: Clickable participant summary with avatar, job/company, event role, area, goals, and interests.
- Extractable props: `ParticipantSummary`.
- Hardcoded: detail route format, Portuguese fallback label, CSS classes.

## PostCard

- Source: `src/features/forum/PostCard.tsx`
- Category: basic
- Description: Forum post with reaction actions, comments, reporting, and removal controls.
- Extractable props: `post`, `onChange`, `onRemoved`.
- Hardcoded: Portuguese action labels, reaction icons, report strings.
