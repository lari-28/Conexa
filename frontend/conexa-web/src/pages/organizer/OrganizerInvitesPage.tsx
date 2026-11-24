import { useState } from 'react';
import type { FormEvent } from 'react';
import { useCurrentEvent } from '../../features/events/EventContext';
import { invitesApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Badge, Button, Card, EmptyState, Input, Select } from '../../components/ui';
import { AsyncView, PageHeader } from '../../components/common';
import { formatDateTime } from '../../utils/format';
import type { EventRole } from '../../types';

const linkOf = (code: string) => `${window.location.origin}/convite/${code}`;

export function OrganizerInvitesPage() {
  const ev = useCurrentEvent();
  const { data, loading, error, reload } = useAsync(() => invitesApi.list(ev.id), [ev.id]);
  const [f, setF] = useState({ role: 'Participant' as EventRole, maxUses: '', days: '' });
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  async function create(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setMsg(null);
    try {
      await invitesApi.create(ev.id, {
        role: f.role, maxUses: f.maxUses ? Number(f.maxUses) : null, expiresInDays: f.days ? Number(f.days) : null,
      });
      setMsg({ tone: 'success', text: 'Convite criado.' });
      reload();
    } catch (err) { setMsg({ tone: 'error', text: (err as Error).message }); }
    finally { setBusy(false); }
  }

  async function copy(code: string) {
    try { await navigator.clipboard.writeText(linkOf(code)); setMsg({ tone: 'success', text: 'Link copiado.' }); }
    catch { setMsg({ tone: 'error', text: `Copie manualmente: ${linkOf(code)}` }); }
  }

  return (
    <>
      <PageHeader title="Convites" subtitle="Quem se cadastra pelo link entra neste evento com o papel escolhido." />
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
      <form className="card stack" onSubmit={create}>
        <h3>Novo convite</h3>
        <Select label="Papel" value={f.role} onChange={(e) => setF({ ...f, role: e.target.value as EventRole })}
          options={[{ value: 'Participant', label: 'Visitante' }, { value: 'Exhibitor', label: 'Expositor' }]} />
        <Input label="Limite de usos (opcional)" type="number" min={1} value={f.maxUses} onChange={(e) => setF({ ...f, maxUses: e.target.value })} />
        <Input label="Validade em dias (opcional)" type="number" min={1} max={365} value={f.days} onChange={(e) => setF({ ...f, days: e.target.value })} />
        <div><Button type="submit" loading={busy}>Gerar link</Button></div>
      </form>
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data && data.length === 0 && <EmptyState title="Nenhum convite ainda" />}
        {data?.map((i) => (
          <Card key={i.id}>
            <div className="row wrap">
              <strong>{i.role === 'Exhibitor' ? 'Expositor' : 'Visitante'}</strong>
              <Badge tone={i.valid ? 'success' : 'danger'}>{i.revokedAt ? 'Cancelado' : i.valid ? 'Ativo' : 'Encerrado'}</Badge>
              <span className="muted small">{i.uses}{i.maxUses ? ` / ${i.maxUses}` : ''} uso(s)
                {i.expiresAt ? ` · expira ${formatDateTime(i.expiresAt)}` : ''}</span>
            </div>
            <code className="small" style={{ wordBreak: 'break-all' }}>{linkOf(i.code)}</code>
            <div className="row wrap">
              <Button variant="secondary" disabled={!i.valid} onClick={() => copy(i.code)}>Copiar link</Button>
              {!i.revokedAt && <Button variant="danger" onClick={async () => { await invitesApi.revoke(ev.id, i.id); reload(); }}>Cancelar</Button>}
            </div>
          </Card>
        ))}
      </AsyncView>
    </>
  );
}
