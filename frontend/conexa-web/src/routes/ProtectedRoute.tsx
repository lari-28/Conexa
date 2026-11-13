import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { Loading } from '../components/ui';
import type { Role } from '../types';

/** Exige login e, opcionalmente, um dos papéis. A autorização real continua no backend. */
export function ProtectedRoute({ roles }: { roles?: Role[] }) {
  const { user, loading, hasRole } = useAuth();
  const location = useLocation();
  if (loading) return <Loading />;
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (roles && !hasRole(...roles)) return <Navigate to="/evento" replace />;
  return <Outlet />;
}
