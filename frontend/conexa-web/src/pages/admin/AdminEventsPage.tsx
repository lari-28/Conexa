import { useState } from 'react';
import type { FormEvent } from 'react';
import { useEvent } from '../../features/events/EventContext';
import { adminApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Badge, Button, Card, Input, Textarea } from '../../components/ui';
import { AsyncView, PageHeader } from '../../components/common';
import { emptyToNull, formatDateTime, fromLocalInput } from '../../utils/format';

export function AdminEventsPage() {
  const { refresh } = useEvent();
  const { data, loading, error, reload } = useAsync(() => adminApi.events(), []);
  const [f, setF] = useState({ name: '', description: '', location: '', startsAt: '', endsAt: '' });
  const [orgEmail, setOrgEmail] = useState<Record<string, string>>({});
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF({ ...f, [k]: e.target.value });

  async function create(e: FormEvent) {
    e.preventDefault();
    setMsg(null);
    try {
      await adminApi.createEvent({ name: f.name.trim(), description: emptyToNull(f.description), location: emptyToNull(f.location),
        startsAt: fromLocalInput(f.startsAt), endsAt: fromLocalInput(f.endsAt), status: 'Active' });
      setF({ name: '', description: '', location: '', startsAt: '', endsAt: '' });
      setMsg({ tone: 'success', text: 'Evento criado.' });
      reload(); await refresh();
    } catch (err) { setMsg({ tone: 'error', text: (err as Error).message }); }
  }

  async function addOrganizer(eventId: string) {
    setMsg(null);
    try {
      await adminApi.addOrganizer(eventId, (orgEmail[eventId] ?? '').trim());
      setOrgEmail({ ...orgEmail, [eventId]: '' });
      setMsg({ tone: 'success', text: 'Organizador designado.' });
    } catch (err) { setMsg({ tone: 'error', text: (err as Error).message }); }
  }

  return (
    <>
      <PageHeader title="Eventos (admin)" />
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
      <form className="card stack" onSubmit={create}>
        <h3>Novo evento</h3>
        <Input label="Nome" required value={f.name} onChange={set('name')} />
        <Textarea label="Descrição" value={f.description} onChange={set('description')} />
        <Input label="Local" value={f.location} onChange={set('location')} />
        <Input label="Início" type="datetime-local" required value={f.startsAt} onChange={set('startsAt')} />
        <Input label="Término" type="datetime-local" required value={f.endsAt} onChange={set('endsAt')} />
        <Button type="submit">Criar evento</Button>
      </form>
      <AsyncView loading={loading} error={error} hasData={!!data}>
        <div className="stack">
          {data?.map((ev) => (
            <Card key={ev.id}>
              <div className="row wrap"><strong className="grow">{ev.name}</strong><Badge>{ev.status}</Badge></div>
              <div className="muted small">{formatDateTime(ev.startsAt)} — {formatDateTime(ev.endsAt)}</div>
              <div className="row wrap">
                <div className="grow"><Input label="Designar organizador (e-mail)" type="email" value={orgEmail[ev.id] ?? ''}
                  onChange={(e) => setOrgEmail({ ...orgEmail, [ev.id]: e.target.value })} /></div>
                <Button variant="secondary" disabled={!(orgEmail[ev.id] ?? '').trim()} onClick={() => void addOrganizer(ev.id)}>Designar</Button>
              </div>
            </Card>
          ))}
        </div>
      </AsyncView>
    </>
  );
}
