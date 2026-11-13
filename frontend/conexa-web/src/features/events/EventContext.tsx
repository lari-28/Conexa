import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { eventsApi } from '../../services/api/endpoints';
import { useAuth } from '../auth/AuthContext';
import type { EventInfo, EventRole, MyEvent } from '../../types';

interface EventState {
  events: MyEvent[];
  current: EventInfo | null;
  roleInEvent: EventRole | null;
  loading: boolean;
  select: (id: string) => void;
  refresh: () => Promise<void>;
}

const EventContext = createContext<EventState | null>(null);
const KEY = 'conexa.eventId';

export function EventProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const [events, setEvents] = useState<MyEvent[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(() => {
    try { return localStorage.getItem(KEY); } catch { return null; }
  });
  const [loading, setLoading] = useState(false);

  const refresh = useCallback(async () => {
    setLoading(true);
    try { setEvents(await eventsApi.mine()); } finally { setLoading(false); }
  }, []);

  useEffect(() => {
    if (user) refresh().catch(() => setEvents([]));
    else setEvents([]);
  }, [user, refresh]);

  const value = useMemo<EventState>(() => {
    const active = events.find((e) => e.event.id === selectedId)
      ?? events.find((e) => e.event.status === 'Active')
      ?? events[0] ?? null;
    return {
      events,
      current: active?.event ?? null,
      roleInEvent: active?.roleInEvent ?? null,
      loading,
      select: (id) => {
        setSelectedId(id);
        try { localStorage.setItem(KEY, id); } catch { /* ignore */ }
      },
      refresh,
    };
  }, [events, selectedId, loading, refresh]);

  return <EventContext.Provider value={value}>{children}</EventContext.Provider>;
}

export function useEvent() {
  const ctx = useContext(EventContext);
  if (!ctx) throw new Error('useEvent deve ser usado dentro de EventProvider');
  return ctx;
}

/** Evento atual garantido (use dentro de telas que exigem evento). */
export function useCurrentEvent(): EventInfo {
  const { current } = useEvent();
  if (!current) throw new Error('Sem evento atual');
  return current;
}
