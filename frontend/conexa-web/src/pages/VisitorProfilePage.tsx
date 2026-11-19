import { useState } from 'react';
import type { FormEvent } from 'react';
import { useCurrentEvent } from '../features/events/EventContext';
import { TagPicker } from '../features/taxonomy/TagPicker';
import { visitorProfileApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Alert, Button, Select, Textarea } from '../components/ui';
import { AsyncView, PageHeader } from '../components/common';
import type { VisitTimeframe, VisitorProfile } from '../types';

const timeframes: { value: VisitTimeframe; label: string }[] = [
  { value: 'Exploring', label: 'Só estou explorando' },
  { value: 'ThisQuarter', label: 'Pretendo decidir neste trimestre' },
  { value: 'ThisMonth', label: 'Pretendo decidir neste mês' },
  { value: 'Now', label: 'Quero resolver agora' },
];

export function VisitorProfilePage() {
  const ev = useCurrentEvent();
  const { data, loading, error, reload } = useAsync(() => visitorProfileApi.get(ev.id), [ev.id]);
  return (
    <>
      <PageHeader title="O que você procura na feira" subtitle={ev.name} />
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data && <VisitorForm key={data.updatedAt ?? 'novo'} eventId={ev.id} initial={data} onSaved={reload} />}
      </AsyncView>
    </>
  );
}

function VisitorForm({ eventId, initial, onSaved }: { eventId: string; initial: VisitorProfile; onSaved: () => void }) {
  const [goal, setGoal] = useState(initial.goal ?? '');
  const [timeframe, setTimeframe] = useState<VisitTimeframe | ''>(initial.timeframe ?? '');
  const [tagIds, setTagIds] = useState(initial.tags.map((t) => t.id));
  const [matching, setMatching] = useState(initial.consents.matching);
  const [share, setShare] = useState(initial.consents.shareWithExhibitors);
  const [ai, setAi] = useState(initial.consents.externalAi);
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  // Revogar o matching revoga as outras finalidades (mesma regra do servidor).
  const onMatching = (v: boolean) => { setMatching(v); if (!v) { setShare(false); setAi(false); } };

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setMsg(null);
    try {
      await visitorProfileApi.save(eventId, {
        goal: goal.trim() || null, timeframe: timeframe || null, tagIds,
        consentMatching: matching, consentShareWithExhibitors: share, consentExternalAi: ai,
      });
      setMsg({ tone: 'success', text: 'Salvo.' });
      onSaved();
    } catch (err) {
      setMsg({ tone: 'error', text: (err as Error).message });
    } finally { setBusy(false); }
  }

  return (
    <form className="stack" onSubmit={submit}>
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
      {initial.readyForMatching
        ? <Alert tone="info">Seu perfil está pronto. As recomendações de estandes chegam quando o matching for ativado.</Alert>
        : <Alert tone="info">Descreva seu objetivo com suas palavras. É a informação mais útil para encontrar os estandes certos.</Alert>}

      <div className="card stack">
        <h2>1. Seu objetivo</h2>
        <Textarea label="O que você quer encontrar ou resolver?" rows={4} maxLength={1000} value={goal}
          onChange={(e) => setGoal(e.target.value)}
          hint="Ex.: quero automatizar o controle de produção da minha fábrica." />
        <Select label="Prazo para decidir" value={timeframe} placeholder="Prefiro não informar"
          onChange={(e) => setTimeframe(e.target.value as VisitTimeframe | '')} options={timeframes} />
      </div>

      <div className="card stack">
        <h2>2. Temas de interesse <span className="muted small">(opcional)</span></h2>
        <TagPicker eventId={eventId} value={tagIds} onChange={setTagIds} max={8} />
      </div>

      <div className="card stack">
        <h2>3. Uso dos seus dados</h2>
        <p className="muted small" style={{ margin: 0 }}>Cada autorização é separada e você pode mudar de ideia a qualquer momento.</p>
        <label className="check"><input type="checkbox" checked={matching} onChange={(e) => onMatching(e.target.checked)} />
          <span>Usar meu objetivo e meus temas para recomendar estandes a mim.</span></label>
        <label className="check"><input type="checkbox" checked={share} disabled={!matching} onChange={(e) => setShare(e.target.checked)} />
          <span>Permitir que expositores com afinidade vejam meu perfil neste evento e me contatem.</span></label>
        <label className="check"><input type="checkbox" checked={ai} disabled={!matching} onChange={(e) => setAi(e.target.checked)} />
          <span>Permitir o envio do meu objetivo (sem nome nem contato) a um serviço externo de IA para melhorar as recomendações.</span></label>
        <small className="muted">As duas últimas dependem da primeira.</small>
      </div>

      <div><Button type="submit" loading={busy}>Salvar</Button></div>
    </form>
  );
}
