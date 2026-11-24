import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useCurrentEvent } from '../features/events/EventContext';
import { TagPicker } from '../features/taxonomy/TagPicker';
import { BoothQr } from '../features/matching/BoothQr';
import { companiesApi } from '../services/api/endpoints';
import { ApiError } from '../services/api/client';
import { useAsync } from '../hooks/useAsync';
import { Alert, Button, Input, Textarea } from '../components/ui';
import { AsyncView, PageHeader } from '../components/common';
import { emptyToNull } from '../utils/format';
import type { CompanyDetail } from '../types';

/** Cadastro e edição do estande do expositor: oferta, cliente ideal e até 3 temas. */
export function ExhibitorBoothPage() {
  const ev = useCurrentEvent();
  const { data, loading, error, reload } = useAsync(async () => {
    const mine = await companiesApi.mine(ev.id);
    return mine[0] ? await companiesApi.get(ev.id, mine[0].id) : null;
  }, [ev.id]);

  return (
    <>
      <PageHeader title="Meu estande" subtitle={ev.name} />
      <AsyncView loading={loading} error={error} hasData={data !== undefined}>
        {data !== undefined && <BoothForm key={data?.id ?? 'novo'} eventId={ev.id} initial={data} onSaved={reload} />}
      </AsyncView>
    </>
  );
}

function BoothForm({ eventId, initial, onSaved }: { eventId: string; initial: CompanyDetail | null; onSaved: () => void }) {
  const [f, setF] = useState({
    name: initial?.name ?? '', segment: initial?.segment ?? '', description: initial?.description ?? '',
    productsServices: initial?.productsServices ?? '', idealCustomer: initial?.idealCustomer ?? '',
    boothLocation: initial?.boothLocation ?? '', website: initial?.website ?? '',
  });
  const [tagIds, setTagIds] = useState(initial?.tags.map((t) => t.id) ?? []);
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const [fields, setFields] = useState<Record<string, string[]>>({});
  const [busy, setBusy] = useState(false);
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF({ ...f, [k]: e.target.value });
  const fe = (k: string) => fields[k]?.[0] ?? fields[k.charAt(0).toUpperCase() + k.slice(1)]?.[0];

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setMsg(null); setFields({});
    try {
      const input = {
        name: f.name.trim(), segment: emptyToNull(f.segment), description: emptyToNull(f.description),
        productsServices: emptyToNull(f.productsServices), idealCustomer: emptyToNull(f.idealCustomer),
        boothLocation: emptyToNull(f.boothLocation), website: emptyToNull(f.website),
      };
      const saved = initial
        ? await companiesApi.update(eventId, initial.id, input)
        : await companiesApi.create(eventId, input);
      await companiesApi.setTags(eventId, saved.id, tagIds);
      setMsg({ tone: 'success', text: 'Estande salvo.' });
      onSaved();
    } catch (err) {
      setMsg({ tone: 'error', text: (err as Error).message });
      if (err instanceof ApiError) setFields(err.fieldErrors);
    } finally { setBusy(false); }
  }

  return (
    <form className="stack" onSubmit={submit}>
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
      {!initial && <Alert tone="info">Cadastre seu estande para aparecer aos visitantes com afinidade.</Alert>}

      <div className="card stack">
        <h2>1. Sua empresa</h2>
        <Input label="Nome do estande / empresa" required value={f.name} onChange={set('name')} error={fe('name')} />
        <Input label="Segmento" value={f.segment} onChange={set('segment')} />
        <Textarea label="Sobre a empresa" rows={3} maxLength={2000} value={f.description} onChange={set('description')} />
        <Input label="Localização no evento (ex.: B-06)" value={f.boothLocation} onChange={set('boothLocation')} />
        <Input label="Site" type="url" value={f.website} onChange={set('website')} error={fe('website')} />
      </div>

      <div className="card stack">
        <h2>2. O que você oferece e para quem</h2>
        <Textarea label="Produtos e serviços" rows={4} maxLength={2000} value={f.productsServices} onChange={set('productsServices')}
          hint="Descreva com suas palavras. Quanto mais específico, melhor o encontro com quem procura." />
        <Textarea label="Cliente ideal" rows={3} maxLength={1000} value={f.idealCustomer} onChange={set('idealCustomer')}
          hint="Ex.: indústrias de médio porte que querem controlar a produção em tempo real." />
      </div>

      <div className="card stack">
        <h2>3. Temas do estande <span className="muted small">(até 3)</span></h2>
        {initial
          ? <TagPicker eventId={eventId} value={tagIds} onChange={setTagIds} max={3} />
          : <p className="muted" style={{ margin: 0 }}>Salve o estande para escolher os temas.</p>}
      </div>

      <div className="row wrap">
        <Button type="submit" loading={busy}>Salvar estande</Button>
        {initial && <Link className="btn" to={`/empresas/${initial.id}`}>Ver como visitante</Link>}
      </div>
      {initial && <BoothQr eventId={eventId} companyId={initial.id} name={initial.name} />}
    </form>
  );
}
