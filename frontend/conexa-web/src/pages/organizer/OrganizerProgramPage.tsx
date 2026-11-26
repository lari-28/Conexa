import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { useCurrentEvent } from '../../features/events/EventContext';
import { organizerApi, eventsApi } from '../../services/api/endpoints';
import type { EventActivityInfo, EventActivityKind, Member } from '../../types';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Badge, Button, Card, EmptyState, Input, Loading, Select, Textarea } from '../../components/ui';
import { PageHeader } from '../../components/common';
import { fromLocalInput, roleLabel, toLocalInput } from '../../utils/format';

interface ActivityDraft {
  kind: EventActivityKind;
  title: string;
  description: string;
  location: string;
  startsAt: string;
  endsAt: string;
  personIds: string[];
}

function blankDraft(eventStart: string, eventEnd: string): ActivityDraft {
  const starts = new Date(eventStart);
  const endsAt = Math.min(starts.getTime() + 60 * 60 * 1000, new Date(eventEnd).getTime());
  return {
    kind: 'Activity', title: '', description: '', location: '',
    startsAt: toLocalInput(starts.toISOString()),
    endsAt: toLocalInput(new Date(endsAt).toISOString()), personIds: [],
  };
}

async function loadMembers(eventId: string): Promise<Member[]> {
  const first = await organizerApi.members(eventId, undefined, 1, 100);
  const members = [...first.items];
  for (let page = 2; page <= first.totalPages; page++) {
    members.push(...(await organizerApi.members(eventId, undefined, page, 100)).items);
  }
  return members.filter((member) => member.status === 'Active');
}

const clockRange = (activity: EventActivityInfo) => `${new Date(activity.startsAt).toLocaleString('pt-BR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}–${new Date(activity.endsAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`;

export function OrganizerProgramPage() {
  const event = useCurrentEvent();
  const activities = useAsync(() => eventsApi.activities(event.id), [event.id]);
  const members = useAsync(() => loadMembers(event.id), [event.id]);
  const [draft, setDraft] = useState<ActivityDraft>(() => blankDraft(event.startsAt, event.endsAt));
  const [editingId, setEditingId] = useState<string | null>(null);
  const [query, setQuery] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const filteredMembers = useMemo(() => (members.data ?? []).filter((member) =>
    member.displayName.toLocaleLowerCase('pt-BR').includes(query.toLocaleLowerCase('pt-BR'))), [members.data, query]);

  const set = (key: Exclude<keyof ActivityDraft, 'personIds' | 'kind'>) => (e: { target: { value: string } }) =>
    setDraft((current) => ({ ...current, [key]: e.target.value }));

  function edit(activity: EventActivityInfo) {
    setEditingId(activity.id);
    setDraft({ kind: activity.kind, title: activity.title, description: activity.description ?? '', location: activity.location,
      startsAt: toLocalInput(activity.startsAt), endsAt: toLocalInput(activity.endsAt), personIds: activity.people.map((person) => person.userId) });
    setMessage(null);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setMessage(null);
    const data = { ...draft, title: draft.title.trim(), description: draft.description.trim() || null,
      location: draft.location.trim(), startsAt: fromLocalInput(draft.startsAt), endsAt: fromLocalInput(draft.endsAt) };
    try {
      if (editingId) await organizerApi.updateActivity(event.id, editingId, data);
      else await organizerApi.createActivity(event.id, data);
      setMessage({ tone: 'success', text: editingId ? 'Atividade atualizada.' : 'Atividade adicionada à programação.' });
      setEditingId(null); setDraft(blankDraft(event.startsAt, event.endsAt));
      await activities.reload();
    } catch (err) { setMessage({ tone: 'error', text: (err as Error).message }); }
    finally { setBusy(false); }
  }

  async function remove(activity: EventActivityInfo) {
    if (!window.confirm(`Remover “${activity.title}” da programação?`)) return;
    setMessage(null);
    try { await organizerApi.deleteActivity(event.id, activity.id); setMessage({ tone: 'success', text: 'Atividade removida.' }); await activities.reload(); }
    catch (err) { setMessage({ tone: 'error', text: (err as Error).message }); }
  }

  return (
    <div className="organizer-program-page stack">
      <PageHeader title="Programação do evento" subtitle="Cadastre palestras e atividades. Horários simultâneos são permitidos." />
      {message && <Alert tone={message.tone}>{message.text}</Alert>}
      <form className="card stack" onSubmit={submit}>
        <h2>{editingId ? 'Editar atividade' : 'Adicionar à programação'}</h2>
        <Select label="Tipo" value={draft.kind} onChange={(e) => setDraft((current) => ({ ...current, kind: e.target.value as EventActivityKind }))}
          options={[{ value: 'Talk', label: 'Palestra' }, { value: 'Activity', label: 'Atividade' }]} />
        <Input label="Título" required minLength={2} maxLength={200} value={draft.title} onChange={set('title')} />
        <Textarea label="Descrição" maxLength={2000} value={draft.description} onChange={set('description')} />
        <Input label="Local" placeholder="Ex.: Auditório principal, sala 2" required minLength={2} maxLength={200} value={draft.location} onChange={set('location')} />
        <div className="program-time-fields">
          <Input label="Início" type="datetime-local" required value={draft.startsAt} onChange={set('startsAt')} />
          <Input label="Término" type="datetime-local" required value={draft.endsAt} onChange={set('endsAt')} />
        </div>
        <div className="program-people-field">
          <div className="program-people-header"><strong>Pessoas vinculadas</strong><span className="muted small">Opcional · {draft.personIds.length} selecionada(s)</span></div>
          <Input label="Buscar pessoa" placeholder="Nome" value={query} onChange={(e) => setQuery(e.target.value)} />
          {members.loading ? <Loading label="Carregando membros do evento…" /> : members.error ? <Alert>{members.error.message}</Alert> : filteredMembers.length === 0
            ? <p className="muted small">Nenhuma pessoa encontrada entre os membros ativos do evento.</p>
            : <div className="program-people-list">
              {filteredMembers.map((member) => <label key={member.userId} className="program-person-option">
                <input type="checkbox" checked={draft.personIds.includes(member.userId)}
                  onChange={(e) => setDraft((current) => ({ ...current, personIds: e.target.checked
                    ? [...current.personIds, member.userId] : current.personIds.filter((id) => id !== member.userId) }))} />
                <span>{member.displayName}</span><Badge>{roleLabel[member.roleInEvent]}</Badge>
              </label>)}
            </div>}
        </div>
        <div className="row wrap">
          <Button type="submit" loading={busy}>{editingId ? 'Salvar alterações' : 'Adicionar atividade'}</Button>
          {editingId && <Button type="button" variant="secondary" onClick={() => { setEditingId(null); setDraft(blankDraft(event.startsAt, event.endsAt)); setMessage(null); }}>Cancelar edição</Button>}
        </div>
      </form>

      <section className="stack" aria-labelledby="program-list-title">
        <h2 id="program-list-title">Itens cadastrados</h2>
        {activities.loading && !activities.data ? <Loading label="Carregando programação…" />
          : activities.error && !activities.data ? <Alert>{activities.error.message}</Alert>
          : activities.data?.length === 0 ? <Card><EmptyState title="Nenhuma atividade cadastrada" description="Use o formulário acima para publicar o primeiro item da programação." /></Card>
          : <div className="program-slot-items">{activities.data?.map((activity) => <Card className="program-activity" key={activity.id}>
            <div className="program-activity-head"><Badge tone={activity.kind === 'Talk' ? 'brand' : 'neutral'}>{activity.kind === 'Talk' ? 'Palestra' : 'Atividade'}</Badge><span className="muted small">{clockRange(activity)}</span></div>
            <h3>{activity.title}</h3>
            {activity.description && <p>{activity.description}</p>}
            <p className="program-location"><strong>Local</strong><span>{activity.location}</span></p>
            {!!activity.people.length && <p className="muted small">{activity.people.map((person) => person.displayName).join(' · ')}</p>}
            <div className="row wrap"><Button type="button" variant="secondary" onClick={() => edit(activity)}>Editar</Button><Button type="button" variant="danger" onClick={() => remove(activity)}>Remover</Button></div>
          </Card>)}</div>}
      </section>
    </div>
  );
}
