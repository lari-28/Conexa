import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from '../components/layout/AppLayout';
import { ProtectedRoute } from './ProtectedRoute';
import { LoginPage } from '../pages/LoginPage';
import { RegisterPage } from '../pages/RegisterPage';
import { RegisterChoicePage } from '../pages/RegisterChoicePage';
import { HomePage } from '../pages/HomePage';
import { InvitePage } from '../pages/InvitePage';
import { OrganizerInvitesPage } from '../pages/organizer/OrganizerInvitesPage';
import { OrganizerImportPage } from '../pages/organizer/OrganizerImportPage';
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
import { OrganizerTaxonomyPage } from '../pages/organizer/OrganizerTaxonomyPage';
import { VisitorProfilePage } from '../pages/VisitorProfilePage';
import { ExhibitorBoothPage } from '../pages/ExhibitorBoothPage';
import { AdminEventsPage } from '../pages/admin/AdminEventsPage';
import { AdminUsersPage } from '../pages/admin/AdminUsersPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/" element={<HomePage />} />
      <Route path="/convite/:code" element={<InvitePage />} />
      <Route path="/cadastro" element={<RegisterChoicePage />} />
      <Route path="/cadastro/:tipo" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/evento" element={<EventPage />} />
          <Route path="/participantes" element={<ParticipantsPage />} />
          <Route path="/participantes/:id" element={<ParticipantDetailPage />} />
          <Route path="/evento/meu-perfil" element={<VisitorProfilePage />} />
          <Route path="/evento/meu-estande" element={<ExhibitorBoothPage />} />
          <Route path="/empresas" element={<CompaniesPage />} />
          <Route path="/empresas/:id" element={<CompanyDetailPage />} />
          <Route path="/forum" element={<ForumPage />} />
          <Route path="/perfil" element={<ProfilePage />} />

          <Route element={<ProtectedRoute roles={['Organizer', 'PlatformAdmin']} />}>
            <Route path="/organizador/evento" element={<OrganizerEventPage />} />
            <Route path="/organizador/participantes" element={<OrganizerMembersPage />} />
            <Route path="/organizador/taxonomia" element={<OrganizerTaxonomyPage />} />
            <Route path="/organizador/convites" element={<OrganizerInvitesPage />} />
            <Route path="/organizador/importacao" element={<OrganizerImportPage />} />
            <Route path="/organizador/moderacao" element={<OrganizerModerationPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['PlatformAdmin']} />}>
            <Route path="/admin/eventos" element={<AdminEventsPage />} />
            <Route path="/admin/usuarios" element={<AdminUsersPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
