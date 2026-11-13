import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../../features/auth/AuthContext';
import { useEvent } from '../../features/events/EventContext';
import { Alert, Avatar, EmptyState, Loading } from '../ui';

const mainLinks = [
  { to: '/evento', label: 'Evento', icon: '🏠' },
  { to: '/participantes', label: 'Pessoas', icon: '👥' },
  { to: '/empresas', label: 'Empresas', icon: '🏢' },
  { to: '/forum', label: 'Fórum', icon: '💬' },
  { to: '/perfil', label: 'Perfil', icon: '🙂' },
];

export function AppLayout() {
  const { user, logout, hasRole } = useAuth();
  const { events, current, select, loading } = useEvent();
  const [menu, setMenu] = useState(false);
  const navigate = useNavigate();
  if (!user) return null;

  const extra = [
    ...(hasRole('Organizer', 'PlatformAdmin') ? [
      { to: '/organizador/evento', label: 'Organizador · Evento' },
      { to: '/organizador/participantes', label: 'Organizador · Participantes' },
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
            {mainLinks.map((l) => <NavLink key={l.to} to={l.to}>{l.label}</NavLink>)}
          </nav>
          <button className="user-btn" onClick={() => setMenu((m) => !m)} aria-label="Menu do usuário" aria-expanded={menu}>
            <Avatar name={user.displayName} url={user.photoUrl} size={32} />
          </button>
        </div>
        {menu && (
          <div className="drawer" role="menu">
            <p className="muted">{user.displayName}<br /><small>{user.email}</small></p>
            {extra.map((l) => <button key={l.to} className="drawer-link" onClick={() => go(l.to)}>{l.label}</button>)}
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
          <NavLink key={l.to} to={l.to}><span aria-hidden>{l.icon}</span><small>{l.label}</small></NavLink>
        ))}
      </nav>
    </div>
  );
}
