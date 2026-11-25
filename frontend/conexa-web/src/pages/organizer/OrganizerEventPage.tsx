import { useState } from 'react';
import type { FormEvent } from 'react';
import { useCurrentEvent, useEvent } from '../../features/events/EventContext';
import { organizerApi } from '../../services/api/endpoints';
import { Alert, Button, Input, Select, Textarea } from '../../components/ui';
import { PageHeader } from '../../components/common';
import { emptyToNull, formatDate, fromLocalInput, toLocalInput } from '../../utils/format';
import { EventMaps } from '../../components/EventMaps';

export function OrganizerEventPage() {
  const ev = useCurrentEvent();
  const { refresh } = useEvent();
  const [f, setF] = useState({
    name: ev.name, description: ev.description ?? '', location: ev.location ?? '',
    startsAt: toLocalInput(ev.startsAt), endsAt: toLocalInput(ev.endsAt), status: ev.status,
  });
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF({ ...f, [k]: e.target.value } as typeof f);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setMsg(null);
    try {
      await organizerApi.updateEvent(ev.id, {
        name: f.name.trim(), description: emptyToNull(f.description), location: emptyToNull(f.location),
        startsAt: fromLocalInput(f.startsAt), endsAt: fromLocalInput(f.endsAt), status: f.status,
      });
      await refresh();
      setMsg({ tone: 'success', text: 'Evento atualizado.' });
    } catch (err) { setMsg({ tone: 'error', text: (err as Error).message }); } finally { setBusy(false); }
  }

  return (
    <>
      <PageHeader title="Dados do evento" subtitle={`Início em ${formatDate(ev.startsAt)}`} />
      <form className="card stack" onSubmit={submit}>
        {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
        <Input label="Nome" required value={f.name} onChange={set('name')} />
        <Textarea label="Descrição" maxLength={2000} value={f.description} onChange={set('description')} />
        <Input label="Local" value={f.location} onChange={set('location')} />
        <Input label="Início" type="datetime-local" required value={f.startsAt} onChange={set('startsAt')} />
        <Input label="Término" type="datetime-local" required value={f.endsAt} onChange={set('endsAt')} />
        <Select label="Situação" value={f.status} onChange={set('status')}
          options={[{ value: 'Draft', label: 'Rascunho' }, { value: 'Active', label: 'Ativo' }, { value: 'Finished', label: 'Encerrado' }]} />
        <Button type="submit" loading={busy}>Salvar</Button>
      </form>
      <EventMaps eventId={ev.id} organizer />
    </>
  );
}
