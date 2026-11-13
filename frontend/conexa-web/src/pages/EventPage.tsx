import { Link } from 'react-router-dom';
import { useCurrentEvent, useEvent } from '../features/events/EventContext';
import { Badge, Card } from '../components/ui';
import { PageHeader } from '../components/common';
import { formatDateTime, roleLabel } from '../utils/format';

export function EventPage() {
  const ev = useCurrentEvent();
  const { roleInEvent } = useEvent();
  const shortcuts = [
    { to: '/participantes', title: 'Pessoas', text: 'Busque participantes por nome, empresa ou interesse.' },
    { to: '/empresas', title: 'Empresas', text: 'Conheça expositores e seus representantes.' },
    { to: '/forum', title: 'Fórum', text: 'Tire dúvidas e compartilhe opiniões.' },
  ];
  return (
    <>
      <PageHeader title={ev.name} subtitle={ev.location ?? undefined}
        actions={roleInEvent ? <Badge tone="brand">{roleLabel[roleInEvent]}</Badge> : undefined} />
      <Card>
        {ev.description && <p>{ev.description}</p>}
        <p className="muted">📅 {formatDateTime(ev.startsAt)} — {formatDateTime(ev.endsAt)}</p>
      </Card>
      <div className="grid">
        {shortcuts.map((s) => (
          <Link key={s.to} to={s.to} className="card card-click link-card"><strong>{s.title}</strong><span className="muted">{s.text}</span></Link>
        ))}
      </div>
    </>
  );
}
