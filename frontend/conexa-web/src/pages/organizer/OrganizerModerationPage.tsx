import { useState } from 'react';
import { useCurrentEvent } from '../../features/events/EventContext';
import { organizerApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Badge, Button, Card, EmptyState, Select } from '../../components/ui';
import { AsyncView, PageHeader } from '../../components/common';
import { timeAgo } from '../../utils/format';
import type { ReportStatus } from '../../types';

const targetLabel = { Post: 'Publicação', Comment: 'Comentário', Profile: 'Perfil' } as const;

export function OrganizerModerationPage() {
  const ev = useCurrentEvent();
  const [status, setStatus] = useState<ReportStatus>('Open');
  const { data, loading, error, reload } = useAsync(() => organizerApi.reports(ev.id, status), [ev.id, status]);
  const [msg, setMsg] = useState<string | null>(null);

  const review = async (id: string, st: ReportStatus, hide: boolean, disable: boolean) => {
    setMsg(null);
    try { await organizerApi.reviewReport(id, { status: st, hideContent: hide, disableAuthorAccess: disable }); reload(); }
    catch (e) { setMsg((e as Error).message); }
  };

  return (
    <>
      <PageHeader title="Moderação" subtitle="Analise denúncias de conteúdo e perfis." />
      <Select label="Situação" value={status} onChange={(e) => setStatus(e.target.value as ReportStatus)}
        options={[{ value: 'Open', label: 'Abertas' }, { value: 'ActionTaken', label: 'Com providência' }, { value: 'Dismissed', label: 'Descartadas' }]} />
      {msg && <Alert>{msg}</Alert>}
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data && data.items.length === 0 ? <EmptyState title="Nenhuma denúncia nesta situação" /> : (
          <div className="stack">
            {data?.items.map((r) => (
              <Card key={r.id}>
                <div className="row wrap"><Badge tone="warning">{targetLabel[r.targetType]}</Badge>
                  <span className="muted small">por {r.reporterName} · {timeAgo(r.createdAt)}</span></div>
                {r.targetPreview && <blockquote>{r.targetPreview}</blockquote>}
                <p><strong>Motivo:</strong> {r.reason}</p>
                {r.status === 'Open' && (
                  <div className="row wrap">
                    <Button variant="secondary" onClick={() => void review(r.id, 'Dismissed', false, false)}>Descartar</Button>
                    {r.targetType !== 'Profile' && <Button variant="danger" onClick={() => void review(r.id, 'ActionTaken', true, false)}>Ocultar conteúdo</Button>}
                    <Button variant="danger" onClick={() => { if (window.confirm('Suspender o acesso do autor a este evento?')) void review(r.id, 'ActionTaken', r.targetType !== 'Profile', true); }}>Ocultar e suspender autor</Button>
                  </div>
                )}
              </Card>
            ))}
          </div>
        )}
      </AsyncView>
    </>
  );
}
