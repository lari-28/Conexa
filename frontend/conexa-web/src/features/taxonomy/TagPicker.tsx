import { useState } from 'react';
import { taxonomyApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Button, Input, Loading } from '../../components/ui';
import type { Tag } from '../../types';

interface Props {
  eventId: string;
  value: string[];
  onChange: (ids: string[]) => void;
  max: number;
}

/** Escolha de temas (chips) agrupados por macro-área, com limite e sugestão de tema novo. */
export function TagPicker({ eventId, value, onChange, max }: Props) {
  const { data: tags, loading, error, reload } = useAsync(() => taxonomyApi.list(eventId), [eventId]);
  const [name, setName] = useState('');
  const [msg, setMsg] = useState<{ tone: 'success' | 'error' | 'info'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  if (loading && !tags) return <Loading />;
  if (error) return <Alert>{error.message}</Alert>;
  const all = tags ?? [];
  const areas = all.filter((t) => t.parentId === null);
  const themesOf = (id: string) => all.filter((t) => t.parentId === id);
  const loose = areas.filter((a) => themesOf(a.id).length === 0); // macro-área sem temas é usada como tema
  const groups = areas.filter((a) => themesOf(a.id).length > 0);

  const toggle = (id: string) => {
    if (value.includes(id)) onChange(value.filter((v) => v !== id));
    else if (value.length < max) onChange([...value, id]);
  };

  async function suggest() {
    if (name.trim().length < 2) return;
    setBusy(true); setMsg(null);
    try {
      const t = await taxonomyApi.suggest(eventId, { name: name.trim(), description: null, parentId: null });
      setName('');
      if (t.status === 'Approved') {
        reload();
        if (!value.includes(t.id) && value.length < max) onChange([...value, t.id]);
        setMsg({ tone: 'info', text: `Já existe o tema "${t.name}". Ele foi selecionado.` });
      } else {
        setMsg({ tone: 'success', text: 'Sugestão enviada para a organização aprovar.' });
      }
    } catch (e) {
      setMsg({ tone: 'error', text: (e as Error).message });
    } finally { setBusy(false); }
  }

  const chip = (t: Tag) => (
    <button key={t.id} type="button" className={`chip ${value.includes(t.id) ? 'chip-on' : ''}`}
      aria-pressed={value.includes(t.id)} title={t.description ?? undefined}
      disabled={!value.includes(t.id) && value.length >= max} onClick={() => toggle(t.id)}>{t.name}</button>
  );

  return (
    <div className="stack">
      <small className="muted">Selecionados: {value.length} de {max}</small>
      {all.length === 0 && <p className="muted">A organização ainda não publicou os temas deste evento.</p>}
      {groups.map((a) => (
        <div key={a.id} className="stack" style={{ gap: '.35rem' }}>
          <strong className="small">{a.name}</strong>
          <div className="chips">{themesOf(a.id).map(chip)}</div>
        </div>
      ))}
      {loose.length > 0 && <div className="chips">{loose.map(chip)}</div>}
      <div className="row" style={{ alignItems: 'flex-end' }}>
        <div className="grow"><Input label="Não achou? Sugira um tema" value={name} maxLength={100} onChange={(e) => setName(e.target.value)} /></div>
        <Button type="button" variant="secondary" loading={busy} onClick={suggest}>Sugerir</Button>
      </div>
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
    </div>
  );
}
