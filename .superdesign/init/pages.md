# Key page dependency trees

Application-wide wrapper for protected pages:

```text
src/main.tsx
└── src/App.tsx
    ├── BrowserRouter
    ├── features/auth/AuthContext.tsx
    │   ├── services/api/endpoints.ts → services/api/client.ts, services/auth/tokenStorage.ts, types/index.ts
    │   └── services/api/client.ts
    ├── features/events/EventContext.tsx
    │   ├── features/auth/AuthContext.tsx
    │   └── services/api/endpoints.ts
    └── routes/AppRoutes.tsx
        ├── routes/ProtectedRoute.tsx → AuthContext.tsx, components/ui/index.tsx, types/index.ts
        └── components/layout/AppLayout.tsx → AuthContext.tsx, EventContext.tsx, components/ui/index.tsx
```

## `/login` — Sign in

Entry: `src/pages/LoginPage.tsx`

```text
LoginPage
├── features/auth/AuthContext.tsx
│   ├── services/api/endpoints.ts → services/api/client.ts, services/auth/tokenStorage.ts, types/index.ts
│   └── services/api/client.ts
└── components/ui/index.tsx (Alert, Button, Input)
```

## `/cadastro` — Registration

Entry: `src/pages/RegisterPage.tsx`

```text
RegisterPage
├── features/auth/AuthContext.tsx → services/api/endpoints.ts, services/api/client.ts, services/auth/tokenStorage.ts, types/index.ts
├── services/api/client.ts (ApiError)
└── components/ui/index.tsx (Alert, Button, Input, Select)
```

## `/evento` — Event overview

Entry: `src/pages/EventPage.tsx`

```text
EventPage
├── features/events/EventContext.tsx → features/auth/AuthContext.tsx, services/api/endpoints.ts
├── components/ui/index.tsx (Badge, Card)
├── components/common/index.tsx (PageHeader) → components/ui/index.tsx (Button)
└── utils/format.ts (formatDateTime, roleLabel)
```

## `/participantes` — Participant directory

Entry: `src/pages/ParticipantsPage.tsx`

```text
ParticipantsPage
├── features/events/EventContext.tsx
├── features/participants/ParticipantCard.tsx
│   ├── components/ui/index.tsx (Avatar, Badge, Card)
│   ├── types/index.ts
│   └── utils/format.ts (roleLabel)
├── services/api/endpoints.ts
├── hooks/useAsync.ts
├── hooks/useDebounce.ts
├── components/ui/index.tsx (EmptyState, Input, Select)
└── components/common/index.tsx (AsyncView, PageHeader, Pagination) → components/ui/index.tsx
```

## `/participantes/:id` — Participant detail

Entry: `src/pages/ParticipantDetailPage.tsx`

```text
ParticipantDetailPage
├── features/events/EventContext.tsx
├── services/api/endpoints.ts
├── hooks/useAsync.ts
├── components/ui/index.tsx (Avatar, Badge, Card)
└── components/common/index.tsx (AsyncView) → components/ui/index.tsx (Alert, Loading)
```

## `/empresas` — Company directory

Entry: `src/pages/CompaniesPage.tsx`

```text
CompaniesPage
├── features/events/EventContext.tsx
├── services/api/endpoints.ts
├── hooks/useAsync.ts
├── hooks/useDebounce.ts
├── components/ui/index.tsx (Badge, Card, EmptyState, Input)
└── components/common/index.tsx (AsyncView, PageHeader, Pagination) → components/ui/index.tsx
```

## `/forum` — Event forum

Entry: `src/pages/ForumPage.tsx`

```text
ForumPage
├── features/events/EventContext.tsx
├── features/forum/PostCard.tsx
│   ├── services/api/endpoints.ts
│   ├── types/index.ts
│   ├── components/ui/index.tsx (Alert, Avatar, Badge, Button, Card, Modal, Textarea)
│   └── utils/format.ts (timeAgo)
├── services/api/endpoints.ts
├── types/index.ts
├── components/ui/index.tsx (Alert, Button, Card, EmptyState, Loading, Select, Textarea)
└── components/common/index.tsx (PageHeader) → components/ui/index.tsx
```

## `/perfil` — Profile

Entry: `src/pages/ProfilePage.tsx`

```text
ProfilePage
├── services/api/endpoints.ts
├── services/api/client.ts (ApiError)
├── hooks/useAsync.ts
├── components/ui/index.tsx (Alert, Button, Input, Textarea)
├── components/common/index.tsx (AsyncView, PageHeader) → components/ui/index.tsx
├── utils/format.ts (emptyToNull, splitList)
└── types/index.ts (Profile)
```

## `/organizador/evento` — Organizer event editor

Entry: `src/pages/organizer/OrganizerEventPage.tsx`

```text
OrganizerEventPage
├── features/events/EventContext.tsx
├── services/api/endpoints.ts
├── components/ui/index.tsx (Alert, Button, Input, Select, Textarea)
├── components/common/index.tsx (PageHeader) → components/ui/index.tsx
└── utils/format.ts (emptyToNull, formatDate, fromLocalInput, toLocalInput)
```

## `/admin/eventos` — Event administration

Entry: `src/pages/admin/AdminEventsPage.tsx`

```text
AdminEventsPage
├── features/events/EventContext.tsx
├── services/api/endpoints.ts
├── hooks/useAsync.ts
├── components/ui/index.tsx (Alert, Badge, Button, Card, Input, Textarea)
├── components/common/index.tsx (AsyncView, PageHeader) → components/ui/index.tsx
└── utils/format.ts (emptyToNull, formatDateTime, fromLocalInput)
```

Data/API imports generally terminate at `services/api/endpoints.ts` and `services/api/client.ts`; shared types are declared in `types/index.ts`. Components not shown under a page are package imports or application-level providers.
