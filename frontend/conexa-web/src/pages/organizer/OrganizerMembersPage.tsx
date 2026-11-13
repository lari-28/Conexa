import { useState } from 'react';
import type { FormEvent } from 'react';
import { useCurrentEvent } from '../../features/events/EventContext';
import { organizerApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Badge, Button, Card, Input, Select } from '../../components/ui';
import { AsyncView, PageHeader } from '../../components/common';
import { roleLabel } from '../../utils/format';
import type { EventRole, MembershipStatus } from '../../types';

const statusTone = { Active: 'success', Disabled: 'warning', Removed: 'danger' } as const;
const statusLabel = { Active: 'Ativo', Disabled: 'Desabilitado', Removed: 'Removido' } as const;

export function OrganizerMembersPage() {
  const ev = useCurrentEvent();
  const { data, loading, error, reload } = useAsync(() => organizerApi.members(ev.id), [ev.id]);
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<EventRole>('Participant');
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);

  const act = async (fn: () => Promise<void>, ok: string) => {
    setMsg(null);
    try { await fn(); setMsg({ tone: 'success', text: ok }); reload(); } catch (e) { setMsg({ tone: 'error', text: (e as Error).message }); }
  };
  const setStatus = (userId: string, status: MembershipStatus) =>
    act(() => organizerApi.updateMember(ev.id, userId, { status }), 'Acesso atualizado.');
  const add = (e: FormEvent) => { e.preventDefault(); void act(async () => { await organizerApi.addMember(ev.id, email.trim(), role); setEmail(''); }, 'Usuário vinculado ao evento.'); };

  return (
    <>
      <PageHeader title="Participantes do evento" subtitle="Habilite, desabilite ou remova acessos." />
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
      <form className="card stack" onSubmit={add}>
        <h3>Vincular usuário já cadastrado</h3>
        <Input label="E-mail" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <Select label="Papel no evento" value={role} onChange={(e) => setRole(e.target.value as EventRole)}
          options={[{ value: 'Participant', label: 'Participante' }, { value: 'Exhibitor', label: 'Expositor' }]} />
        <Button type="submit">Vincular</Button>
      </form>
      <AsyncView loading={loading} error={error} hasData={!!data}>
        <div className="stack">
          {data?.items.map((m) => (
            <Card key={m.userId}>
              <div className="row wrap">
                <div className="grow"><strong>{m.displayName}</strong><div className="muted small">{m.email}</div></div>
                <Badge>{roleLabel[m.roleInEvent]}</Badge>
                <Badge tone={statusTone[m.status]}>{statusLabel[m.status]}</Badge>
              </div>
              {m.roleInEvent !== 'Organizer' && (
                <div className="row wrap">
                  {m.status !== 'Active' && <Button variant="secondary" onClick={() => void setStatus(m.userId, 'Active')}>Habilitar</Button>}
                  {m.status === 'Active' && <Button variant="secondary" onClick={() => void setStatus(m.userId, 'Disabled')}>Desabilitar</Button>}
                  {m.status !== 'Removed' && <Button variant="danger" onClick={() => void setStatus(m.userId, 'Removed')}>Remover</Button>}
                </div>
              )}
            </Card>
          ))}
        </div>
      </AsyncView>
    </>
  );
}
