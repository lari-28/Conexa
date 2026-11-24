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
  const [pending, setPending] = useState<string[]>([]); // sugestões desta sessão, aguardando aprovação

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
    const wanted = name.trim();
    if (wanted.length < 2) { setMsg({ tone: 'error', text: 'Digite pelo menos 2 letras para sugerir um tema.' }); return; }
    setBusy(true); setMsg(null);
    try {
      const t = await taxonomyApi.suggest(eventId, { name: wanted, description: null, parentId: null });
      setName('');
      if (t.status === 'Approved') {
        reload();
        if (!value.includes(t.id) && value.length < max) onChange([...value, t.id]);
        setMsg({ tone: 'info', text: `O tema "${t.name}" já existe e foi selecionado para você.` });
      } else {
        setPending((p) => (p.includes(t.name) ? p : [...p, t.name]));
        setMsg({ tone: 'success', text: `Tema "${t.name}" sugerido com sucesso! Ele aparece na lista assim que a organização aprovar.` });
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
      {pending.length > 0 && (
        <div className="chips" aria-label="Sugestões aguardando aprovação">
          {pending.map((p) => <span key={p} className="chip chip-pending" title="Aguardando aprovação da organização">{p} · aguardando aprovação</span>)}
        </div>
      )}
      <div className="row" style={{ alignItems: 'flex-end' }}>
        <div className="grow">
          {/* Enter aqui sugere o tema; sem isso, o Enter enviaria o formulário inteiro (estande ou perfil). */}
          <Input label="Não achou? Sugira um tema" value={name} maxLength={100} onChange={(e) => setName(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); void suggest(); } }} />
        </div>
        <Button type="button" variant="secondary" loading={busy} onClick={suggest}>Sugerir</Button>
      </div>
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
    </div>
  );
}
