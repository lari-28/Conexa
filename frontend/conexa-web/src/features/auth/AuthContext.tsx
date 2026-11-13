import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { authApi } from '../../services/api/endpoints';
import { setUnauthorizedHandler } from '../../services/api/client';
import { tokenStorage } from '../../services/auth/tokenStorage';
import type { Me, Role } from '../../types';

interface AuthState {
  user: Me | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (data: Parameters<typeof authApi.register>[0]) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (...roles: Role[]) => boolean;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<Me | null>(null);
  const [loading, setLoading] = useState(() => tokenStorage.get() !== null);

  const clear = useCallback(() => { tokenStorage.clear(); setUser(null); }, []);

  useEffect(() => {
    setUnauthorizedHandler(clear);
    if (tokenStorage.get()) {
      authApi.me().then(setUser).catch(clear).finally(() => setLoading(false));
    }
    return () => setUnauthorizedHandler(null);
  }, [clear]);

  const value = useMemo<AuthState>(() => ({
    user,
    loading,
    login: async (email, password) => {
      const res = await authApi.login(email, password);
      tokenStorage.set(res.token);
      setUser(res.user);
    },
    register: async (data) => {
      const res = await authApi.register(data);
      tokenStorage.set(res.token);
      setUser(res.user);
    },
    logout: async () => {
      try { await authApi.logout(); } catch { /* logout é local para JWT */ }
      clear();
    },
    hasRole: (...roles) => !!user && roles.some((r) => user.roles.includes(r)),
  }), [user, loading, clear]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider');
  return ctx;
}
