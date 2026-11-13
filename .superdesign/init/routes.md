# Frontend routes

## Framework and routing

- React 19 + TypeScript + Vite; React Router 7 (`BrowserRouter`, declarative `Routes`).
- Root entry: `src/main.tsx` → `src/App.tsx` → `src/routes/AppRoutes.tsx`.
- `App.tsx` wraps the router in `AuthProvider` and `EventProvider`.
- `ProtectedRoute` redirects unauthenticated users to `/login`, and role failures to `/evento`.
- `AppLayout` wraps authenticated pages and renders the shared header, event state, bottom navigation, and `<Outlet />`.

## Route map

| URL | Page | Layout / authorization |
|---|---|---|
| `/login` | `src/pages/LoginPage.tsx` | Public |
| `/cadastro` | `src/pages/RegisterPage.tsx` | Public |
| `/evento` | `src/pages/EventPage.tsx` | `AppLayout`, authenticated |
| `/participantes` | `src/pages/ParticipantsPage.tsx` | `AppLayout`, authenticated |
| `/participantes/:id` | `src/pages/ParticipantDetailPage.tsx` | `AppLayout`, authenticated |
| `/empresas` | `src/pages/CompaniesPage.tsx` | `AppLayout`, authenticated |
| `/empresas/:id` | `src/pages/CompanyDetailPage.tsx` | `AppLayout`, authenticated |
| `/forum` | `src/pages/ForumPage.tsx` | `AppLayout`, authenticated |
| `/perfil` | `src/pages/ProfilePage.tsx` | `AppLayout`, authenticated |
| `/organizador/evento` | `src/pages/organizer/OrganizerEventPage.tsx` | `AppLayout`, Organizer or PlatformAdmin |
| `/organizador/participantes` | `src/pages/organizer/OrganizerMembersPage.tsx` | `AppLayout`, Organizer or PlatformAdmin |
| `/organizador/moderacao` | `src/pages/organizer/OrganizerModerationPage.tsx` | `AppLayout`, Organizer or PlatformAdmin |
| `/admin/eventos` | `src/pages/admin/AdminEventsPage.tsx` | `AppLayout`, PlatformAdmin |
| `/admin/usuarios` | `src/pages/admin/AdminUsersPage.tsx` | `AppLayout`, PlatformAdmin |
| `*` | Redirect to `/evento` | — |

## Router implementation: `src/routes/AppRoutes.tsx`

```tsx
import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from '../components/layout/AppLayout';
import { ProtectedRoute } from './ProtectedRoute';
import { LoginPage } from '../pages/LoginPage';
import { RegisterPage } from '../pages/RegisterPage';
import { EventPage } from '../pages/EventPage';
import { ParticipantsPage } from '../pages/ParticipantsPage';
import { ParticipantDetailPage } from '../pages/ParticipantDetailPage';
import { CompaniesPage } from '../pages/CompaniesPage';
import { CompanyDetailPage } from '../pages/CompanyDetailPage';
import { ForumPage } from '../pages/ForumPage';
import { ProfilePage } from '../pages/ProfilePage';
import { OrganizerEventPage } from '../pages/organizer/OrganizerEventPage';
import { OrganizerMembersPage } from '../pages/organizer/OrganizerMembersPage';
import { OrganizerModerationPage } from '../pages/organizer/OrganizerModerationPage';
import { AdminEventsPage } from '../pages/admin/AdminEventsPage';
import { AdminUsersPage } from '../pages/admin/AdminUsersPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/cadastro" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/evento" element={<EventPage />} />
          <Route path="/participantes" element={<ParticipantsPage />} />
          <Route path="/participantes/:id" element={<ParticipantDetailPage />} />
          <Route path="/empresas" element={<CompaniesPage />} />
          <Route path="/empresas/:id" element={<CompanyDetailPage />} />
          <Route path="/forum" element={<ForumPage />} />
          <Route path="/perfil" element={<ProfilePage />} />

          <Route element={<ProtectedRoute roles={['Organizer', 'PlatformAdmin']} />}>
            <Route path="/organizador/evento" element={<OrganizerEventPage />} />
            <Route path="/organizador/participantes" element={<OrganizerMembersPage />} />
            <Route path="/organizador/moderacao" element={<OrganizerModerationPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['PlatformAdmin']} />}>
            <Route path="/admin/eventos" element={<AdminEventsPage />} />
            <Route path="/admin/usuarios" element={<AdminUsersPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/evento" replace />} />
    </Routes>
  );
}
```
