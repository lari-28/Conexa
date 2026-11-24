import { useNavigate } from 'react-router-dom';
import { notificationsApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Badge, Button, Card, EmptyState } from '../components/ui';
import { AsyncView, PageHeader } from '../components/common';
import { timeAgo } from '../utils/format';

export const NOTIFICATIONS_CHANGED = 'conexa:notifications-changed';

export function NotificationsPage() {
  const { data, loading, error, reload } = useAsync(() => notificationsApi.list(), []);
  const navigate = useNavigate();
  const changed = () => { reload(); window.dispatchEvent(new Event(NOTIFICATIONS_CHANGED)); };

  return (
    <>
      <PageHeader title="Avisos" subtitle="Novidades sobre recomendações e leads."
        actions={<Button variant="secondary" onClick={async () => { await notificationsApi.readAll(); changed(); }}>Marcar todos como lidos</Button>} />
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data && data.length === 0 && <EmptyState title="Nenhum aviso por enquanto" />}
        <div className="stack">
          {data?.map((n) => (
            <Card key={n.id} onClick={async () => { await notificationsApi.read(n.id); changed(); navigate(n.link ?? '/evento'); }}>
              <div className="row wrap"><strong className="grow">{n.title}</strong>
                {!n.read && <Badge tone="brand">Novo</Badge>}<span className="muted small">{timeAgo(n.createdAt)}</span></div>
              <span className="muted">{n.body}</span>
            </Card>
          ))}
        </div>
      </AsyncView>
    </>
  );
}
