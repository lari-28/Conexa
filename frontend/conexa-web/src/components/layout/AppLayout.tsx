import { useEffect, useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../../features/auth/AuthContext';
import { useEvent } from '../../features/events/EventContext';
import { Alert, Avatar, EmptyState, Loading } from '../ui';
import { notificationsApi } from '../../services/api/endpoints';
import { NOTIFICATIONS_CHANGED } from '../../pages/NotificationsPage';

const svg = (d: string) => (
  <svg className="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden>
    <path d={d} />
  </svg>
);

const mainLinks = [
  { to: '/evento', label: 'Início', icon: svg('M3 11l9-8 9 8M5 10v10h14V10') },
  { to: '/empresas', label: 'Estandes', icon: svg('M3 9l1.5-5h15L21 9M3 9h18M3 9v11h18V9M9 20v-6h6v6') },
  { to: '/participantes', label: 'Pessoas', icon: svg('M16 20v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2M10 10a3.5 3.5 0 100-7 3.5 3.5 0 000 7zM20 20v-2a3 3 0 00-2.5-3M15.5 3.2a3.5 3.5 0 010 6.6') },
  { to: '/perfil', label: 'Perfil', icon: svg('M20 21a8 8 0 10-16 0M12 12a4.5 4.5 0 100-9 4.5 4.5 0 000 9z') },
];

export function AppLayout() {
  const { user, logout, hasRole } = useAuth();
  const { events, current, select, loading } = useEvent();
  const [menu, setMenu] = useState(false);
  const navigate = useNavigate();
  const [unread, setUnread] = useState(0);

  // Contador de avisos: atualiza ao montar, a cada minuto e quando a página de avisos muda algo.
  useEffect(() => {
    if (!user) return;
    let alive = true;
    const load = () => { notificationsApi.count().then((n) => { if (alive) setUnread(n); }).catch(() => undefined); };
    load();
    const t = window.setInterval(load, 60_000);
    window.addEventListener(NOTIFICATIONS_CHANGED, load);
    return () => { alive = false; window.clearInterval(t); window.removeEventListener(NOTIFICATIONS_CHANGED, load); };
  }, [user]);

  if (!user) return null;

  const extra = [
    ...(hasRole('Organizer', 'PlatformAdmin') ? [
      { to: '/organizador/evento', label: 'Organizador · Evento' },
      { to: '/organizador/participantes', label: 'Organizador · Participantes' },
      { to: '/organizador/painel', label: 'Organizador · Painel do evento' },
      { to: '/organizador/taxonomia', label: 'Organizador · Taxonomia' },
      { to: '/organizador/importacao', label: 'Organizador · Importar expositores' },
      { to: '/organizador/convites', label: 'Organizador · Convites' },
      { to: '/organizador/moderacao', label: 'Organizador · Moderação' },
    ] : []),
    ...(hasRole('PlatformAdmin') ? [
      { to: '/admin/eventos', label: 'Admin · Eventos' },
      { to: '/admin/usuarios', label: 'Admin · Usuários' },
    ] : []),
  ];

  const go = (to: string) => { setMenu(false); navigate(to); };

  return (
    <div className="shell">
      <header className="topbar">
        <div className="topbar-inner">
          <span className="logo" onClick={() => go('/evento')}>CONEXA</span>
          {events.length > 1 ? (
            <select className="event-select" aria-label="Evento" value={current?.id ?? ''} onChange={(e) => select(e.target.value)}>
              {events.map((e) => <option key={e.event.id} value={e.event.id}>{e.event.name}</option>)}
            </select>
          ) : <span className="event-name">{current?.name ?? ''}</span>}
          <nav className="top-nav" aria-label="Principal">
            {mainLinks.map((l) => <NavLink key={l.to} to={l.to}>{l.icon}{l.label}</NavLink>)}
          </nav>
          <NavLink to="/avisos" className="bell" aria-label={unread > 0 ? `Avisos (${unread} novos)` : 'Avisos'}>
            <svg className="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden>
              <path d="M18 8a6 6 0 10-12 0c0 7-3 9-3 9h18s-3-2-3-9M13.7 21a2 2 0 01-3.4 0" />
            </svg>
            {unread > 0 && <span className="bell-count">{unread > 9 ? '9+' : unread}</span>}
          </NavLink>
          <button className="user-btn" onClick={() => setMenu((m) => !m)} aria-label="Menu do usuário" aria-expanded={menu}>
            <Avatar name={user.displayName} url={user.photoUrl} size={32} />
          </button>
        </div>
        {menu && (
          <div className="drawer" role="menu">
            <p className="muted">{user.displayName}<br /><small>{user.email}</small></p>
            {extra.map((l) => <button key={l.to} className="drawer-link" onClick={() => go(l.to)}>{l.label}</button>)}
            <button className="drawer-link" onClick={() => go('/forum')}>Comunidade</button>
            <button className="drawer-link" onClick={() => go('/perfil')}>Meu perfil e privacidade</button>
            <button className="drawer-link danger" onClick={() => { setMenu(false); void logout().then(() => navigate('/login')); }}>Sair</button>
          </div>
        )}
      </header>

      <main className="content">
        {loading && !current ? <Loading />
          : !current && !hasRole('PlatformAdmin', 'Organizer') ? (
            <EmptyState title="Você ainda não está vinculado a um evento"
              description="Peça à organização para habilitar o seu acesso ao evento." />
          ) : !current ? <Alert tone="info">Nenhum evento disponível. Crie um em Admin · Eventos.</Alert>
          : <Outlet />}
      </main>

      <nav className="bottom-nav" aria-label="Principal (celular)">
        {mainLinks.map((l) => (
          <NavLink key={l.to} to={l.to}>{l.icon}<small>{l.label}</small></NavLink>
        ))}
      </nav>
    </div>
  );
}
