import { useState } from 'react';
import { useCurrentEvent } from '../../features/events/EventContext';
import { insightsApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Badge, Button, Card } from '../../components/ui';
import { AsyncView, PageHeader } from '../../components/common';
import type { EventInsights } from '../../types';

const pct = (v: number | null) => (v === null ? '—' : `${Math.round(v * 100)}%`);

function Stat({ label, value, hint }: { label: string; value: string | number; hint?: string }) {
  return (
    <div className="card" style={{ gap: '.15rem' }}>
      <span className="muted small">{label}</span>
      <strong style={{ fontSize: '1.6rem', color: 'var(--indigo)' }}>{value}</strong>
      {hint && <small className="muted">{hint}</small>}
    </div>
  );
}

/** Painel do evento para o organizador. Impresso pelo navegador, vira o relatório em PDF. */
export function OrganizerDashboardPage() {
  const ev = useCurrentEvent();
  const { data, loading, error } = useAsync(() => insightsApi.event(ev.id), [ev.id]);
  const [msg, setMsg] = useState<string | null>(null);
  const exp = async (fn: () => Promise<void>) => { setMsg(null); try { await fn(); } catch (e) { setMsg((e as Error).message); } };

  return (
    <>
      <PageHeader title="Painel do evento" subtitle={ev.name}
        actions={<div className="row wrap">
          <Button variant="secondary" onClick={() => exp(() => insightsApi.exportBooths(ev.id))}>Exportar estandes (CSV)</Button>
          <Button variant="secondary" onClick={() => exp(() => insightsApi.exportThemes(ev.id))}>Exportar temas (CSV)</Button>
          <Button variant="secondary" onClick={() => window.print()}>Imprimir / PDF</Button>
        </div>} />
      {msg && <Alert>{msg}</Alert>}
      <AsyncView loading={loading} error={error} hasData={!!data}>{data && <Body d={data} />}</AsyncView>
    </>
  );
}

function Body({ d }: { d: EventInsights }) {
  const t = d.totals;
  const maxDemand = Math.max(1, ...d.themes.map((x) => Math.max(x.demand, x.supply)));
  return (
    <>
      {d.notes.map((n) => <Alert key={n} tone="info">{n}</Alert>)}

      <div className="grid">
        <Stat label="Visitantes com perfil" value={t.visitorsWithProfile} hint={`${t.consentedMatching} autorizaram o matching, ${t.consentedSharing} o compartilhamento`} />
        <Stat label="Estandes" value={t.booths} hint={`${t.boothsWithThemes} com temas definidos`} />
        <Stat label="Check-ins por QR" value={t.checkIns} hint={`${t.checkedInVisitors} visitante(s) diferente(s)`} />
        <Stat label="Recomendações úteis" value={pct(t.usefulRate)} hint={`${t.feedbackUseful} sim · ${t.feedbackNotUseful} não`} />
        <Stat label="Estandes nunca recomendados" value={d.exposure.boothsNeverRecommended} hint={`${d.exposure.boothsRecommended} apareceram para alguém`} />
        <Stat label="Concentração de exposição" value={d.exposure.gini.toFixed(2)}
          hint={`0 = igual para todos. Os 20% mais recomendados ficam com ${pct(d.exposure.top20Share)}`} />
      </div>
      {d.exposure.mostRecommended && (
        <p className="muted small" style={{ margin: 0 }}>Estande mais recomendado: <strong>{d.exposure.mostRecommended}</strong> ({d.exposure.mostRecommendedCount} visitante(s)).</p>
      )}

      <h2>Lacunas: o que os visitantes procuram e ninguém oferece</h2>
      {d.gaps.length === 0
        ? <p className="muted">Nenhuma lacuna encontrada com os dados atuais.</p>
        : <div className="chips">{d.gaps.map((g) => <Badge key={g.tagId} tone="warning">{g.name} · {g.demand} visitante(s)</Badge>)}</div>}

      <h2>Demanda x oferta por tema</h2>
      <Card>
        {d.themes.length === 0 && <p className="muted" style={{ margin: 0 }}>Defina a taxonomia do evento para ver este quadro.</p>}
        {d.themes.map((x) => (
          <div key={x.tagId} className="stack" style={{ gap: '.2rem' }}>
            <div className="row"><strong className="grow">{x.name}{x.area && <span className="muted small"> · {x.area}</span>}</strong>
              <span className="small muted">demanda {x.demand} · oferta {x.supply}</span></div>
            <div aria-hidden style={{ display: 'grid', gap: 2 }}>
              <div style={{ height: 6, width: `${(x.demand / maxDemand) * 100}%`, background: 'var(--indigo)', borderRadius: 3 }} />
              <div style={{ height: 6, width: `${(x.supply / maxDemand) * 100}%`, background: 'var(--chrome-shadow)', borderRadius: 3 }} />
            </div>
          </div>
        ))}
        {d.themes.length > 0 && <small className="muted">Índigo = visitantes que escolheram ou mencionaram o tema · Teal = estandes com o tema.</small>}
      </Card>

      <h2>Perfis em comum</h2>
      <div className="grid">
        {d.profileGroups.map((g) => (
          <Card key={g.area}><strong>{g.area}</strong><span className="muted">{g.visitors} visitante(s)</span>
            {g.topThemes.length > 0 && <div className="chips">{g.topThemes.map((x) => <Badge key={x}>{x}</Badge>)}</div>}</Card>
        ))}
      </div>

      <h2>O que mais aparece nos objetivos</h2>
      {d.topTerms.length === 0
        ? <p className="muted">Ainda não há termos repetidos por pelo menos dois visitantes.</p>
        : <div className="chips">{d.topTerms.map((x) => <Badge key={x.term} tone="brand">{x.term} · {x.count}</Badge>)}</div>}

      <small className="muted">Leitura automática dos dados cadastrados, sem inteligência artificial. Só considera visitantes que autorizaram o uso do perfil.</small>
    </>
  );
}
