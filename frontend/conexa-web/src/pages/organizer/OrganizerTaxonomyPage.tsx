import { useState } from 'react';
import type { FormEvent } from 'react';
import { useCurrentEvent } from '../../features/events/EventContext';
import { taxonomyApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Badge, Button, Card, EmptyState, Input, Select } from '../../components/ui';
import { AsyncView, PageHeader } from '../../components/common';
import { emptyToNull, splitList } from '../../utils/format';
import type { SuggestionGroup, Tag } from '../../types';

/** Taxonomia do evento: biblioteca-base, cadastro direto e fila de aprovação em lote. */
export function OrganizerTaxonomyPage() {
  const ev = useCurrentEvent();
  const tags = useAsync(() => taxonomyApi.list(ev.id), [ev.id]);
  const queue = useAsync(() => taxonomyApi.suggestions(ev.id), [ev.id]);
  const library = useAsync(() => taxonomyApi.library(ev.id), [ev.id]);
  const [msg, setMsg] = useState<{ tone: 'success' | 'error' | 'info'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);
  const refresh = () => { tags.reload(); queue.reload(); };

  async function run(fn: () => Promise<string>) {
    setBusy(true); setMsg(null);
    try { setMsg({ tone: 'success', text: await fn() }); refresh(); }
    catch (e) { setMsg({ tone: 'error', text: (e as Error).message }); }
    finally { setBusy(false); }
  }

  return (
    <>
      <PageHeader title="Taxonomia do evento" subtitle="Os temas que conectam visitantes e estandes. Você aprova e ajusta; não precisa cadastrar tudo." />
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}

      <Card>
        <h2 style={{ marginTop: 0 }}>Começar por uma biblioteca</h2>
        <p className="muted small" style={{ margin: 0 }}>Importa temas prontos de um setor. O que já existe não é duplicado.</p>
        <div className="chips">
          {(library.data ?? []).map((s) => (
            <Button key={s.key} variant="secondary" disabled={busy}
              onClick={() => run(async () => `${(await taxonomyApi.importLibrary(ev.id, s.key)).length} tema(s) importado(s).`)}>
              {s.name} ({s.themes})
            </Button>
          ))}
        </div>
      </Card>

      <h2>Fila de aprovação</h2>
      <AsyncView loading={queue.loading} error={queue.error} hasData={!!queue.data}>
        {queue.data && <Queue evId={ev.id} groups={queue.data} approved={tags.data ?? []} busy={busy} run={run} />}
      </AsyncView>

      <h2>Temas aprovados</h2>
      <AsyncView loading={tags.loading} error={tags.error} hasData={!!tags.data}>
        {tags.data && <Approved evId={ev.id} tags={tags.data} busy={busy} run={run} />}
      </AsyncView>
    </>
  );
}

function Queue({ evId, groups, approved, busy, run }: {
  evId: string; groups: SuggestionGroup[]; approved: Tag[]; busy: boolean;
  run: (fn: () => Promise<string>) => Promise<void>;
}) {
  const [sel, setSel] = useState<string[]>([]);
  if (groups.length === 0) return <EmptyState title="Nenhuma sugestão pendente" description="Quando visitantes e expositores sugerirem temas, eles aparecem aqui." />;

  const idsOf = (g: SuggestionGroup) => g.items.map((i) => i.id);
  const toggle = (g: SuggestionGroup) => {
    const ids = idsOf(g);
    setSel(ids.every((i) => sel.includes(i)) ? sel.filter((i) => !ids.includes(i)) : [...new Set([...sel, ...ids])]);
  };
  const batch = (action: 'Approve' | 'Reject') => run(async () => {
    const r = await taxonomyApi.review(evId, action, sel);
    setSel([]);
    return action === 'Approve' ? `${r.approved} aprovado(s).` : `${r.rejected} rejeitado(s).`;
  });
  const merge = (g: SuggestionGroup) => run(async () => {
    await taxonomyApi.review(evId, 'Merge', idsOf(g), g.matchesTagId!);
    setSel((s) => s.filter((i) => !idsOf(g).includes(i)));
    return `"${g.name}" fundido em "${g.matchesTagName}".`;
  });

  return (
    <div className="stack">
      <div className="row wrap">
        <Button disabled={busy || sel.length === 0} onClick={() => batch('Approve')}>Aprovar selecionados ({sel.length})</Button>
        <Button variant="danger" disabled={busy || sel.length === 0} onClick={() => batch('Reject')}>Rejeitar selecionados</Button>
        <Button variant="ghost" onClick={() => setSel(groups.flatMap(idsOf))}>Selecionar todos</Button>
      </div>
      {groups.map((g) => (
        <Card key={g.key}>
          <div className="row">
            <input type="checkbox" aria-label={`Selecionar ${g.name}`} checked={idsOf(g).every((i) => sel.includes(i))} onChange={() => toggle(g)} />
            <div className="grow">
              <strong>{g.name}</strong>{' '}
              {g.items.length > 1 && <Badge>{g.items.length} sugestões iguais</Badge>}
            </div>
          </div>
          {g.matchesTagId && (
            <div className="row wrap">
              <Badge tone="warning">Parece o tema "{g.matchesTagName}"</Badge>
              <Button variant="secondary" disabled={busy} onClick={() => merge(g)}>Fundir em "{g.matchesTagName}"</Button>
            </div>
          )}
          {g.items[0]?.description && <small className="muted">{g.items[0].description}</small>}
        </Card>
      ))}
      <small className="muted">Dica: para fundir com outro tema, aprove a sugestão e edite o tema existente adicionando o nome como sinônimo. {approved.length} tema(s) aprovados.</small>
    </div>
  );
}

function Approved({ evId, tags, busy, run }: {
  evId: string; tags: Tag[]; busy: boolean; run: (fn: () => Promise<string>) => Promise<void>;
}) {
  const areas = tags.filter((t) => t.parentId === null);
  const [f, setF] = useState({ name: '', parentId: '', synonyms: '', description: '' });
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF({ ...f, [k]: e.target.value });

  async function create(e: FormEvent) {
    e.preventDefault();
    await run(async () => {
      await taxonomyApi.create(evId, {
        name: f.name.trim(), parentId: f.parentId || null,
        synonyms: splitList(f.synonyms), description: emptyToNull(f.description),
      });
      setF({ name: '', parentId: '', synonyms: '', description: '' });
      return 'Tema criado.';
    });
  }

  return (
    <div className="stack">
      {tags.length === 0 && <EmptyState title="Nenhum tema ainda" description="Importe uma biblioteca ou crie o primeiro tema abaixo." />}
      {areas.map((a) => (
        <Card key={a.id}>
          <div className="row"><strong className="grow">{a.name}</strong>
            <Button variant="ghost" disabled={busy} onClick={() => run(async () => { await taxonomyApi.archive(evId, a.id); return 'Arquivado.'; })}>Arquivar</Button></div>
          {tags.filter((t) => t.parentId === a.id).map((t) => (
            <div key={t.id} className="row" style={{ paddingLeft: '1rem' }}>
              <div className="grow">{t.name}
                {t.synonyms.length > 0 && <div className="muted small">Sinônimos: {t.synonyms.join(', ')}</div>}</div>
              <Button variant="ghost" disabled={busy} onClick={() => run(async () => { await taxonomyApi.archive(evId, t.id); return 'Arquivado.'; })}>Arquivar</Button>
            </div>
          ))}
        </Card>
      ))}
      <form className="card stack" onSubmit={create}>
        <h3>Novo tema ou macro-área</h3>
        <Input label="Nome" required value={f.name} onChange={set('name')} />
        <Select label="Macro-área" value={f.parentId} onChange={set('parentId')} placeholder="(é uma macro-área)"
          options={areas.map((a) => ({ value: a.id, label: a.name }))} />
        <Input label="Sinônimos (separados por vírgula)" value={f.synonyms} onChange={set('synonyms')}
          hint='Ex.: "IA, inteligência artificial, machine learning"' />
        <Input label="Descrição curta" value={f.description} onChange={set('description')} />
        <div><Button type="submit" loading={busy}>Criar</Button></div>
      </form>
    </div>
  );
}
