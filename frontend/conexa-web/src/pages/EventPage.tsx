import { Link } from 'react-router-dom';
import { useCurrentEvent, useEvent } from '../features/events/EventContext';
import { Badge, Card } from '../components/ui';
import { PageHeader } from '../components/common';
import { profileByRole } from '../content/profiles';
import { formatDateTime, roleLabel } from '../utils/format';

export function EventPage() {
  const ev = useCurrentEvent();
  const { roleInEvent } = useEvent();
  const profile = profileByRole(roleInEvent);
  const shortcuts = [
    { to: '/empresas', title: 'Estandes', text: 'Conheça os expositores e o que eles oferecem.' },
    { to: '/participantes', title: 'Pessoas', text: 'Busque participantes por nome, empresa ou interesse.' },
    { to: '/forum', title: 'Comunidade', text: 'Tire dúvidas e compartilhe opiniões.' },
  ];
  return (
    <>
      <PageHeader title={ev.name} subtitle={ev.location ?? undefined}
        actions={roleInEvent ? <Badge tone="brand">{roleLabel[roleInEvent]}</Badge> : undefined} />
      <Card>
        {ev.description && <p>{ev.description}</p>}
        <p className="muted">{formatDateTime(ev.startsAt)} — {formatDateTime(ev.endsAt)}</p>
      </Card>

      <Card>
        <h3>Próximo passo</h3>
        <p className="muted" style={{ margin: 0 }}>{profile.nextStep}</p>
        <div><Link className="btn btn-primary" to={profile.nextPath}>Continuar</Link></div>
      </Card>

      <div className="ai-block">
        <span className="ai-tag">Em breve</span>
        <h3>{profile.aiSoon.title}</h3>
        <p className="muted" style={{ margin: 0 }}>{profile.aiSoon.text}</p>
      </div>

      <div className="grid">
        {shortcuts.map((s) => (
          <Link key={s.to} to={s.to} className="card card-click link-card"><strong>{s.title}</strong><span className="muted">{s.text}</span></Link>
        ))}
      </div>
    </>
  );
}
