import { useMemo, useState } from 'react';
import { useCurrentEvent } from '../../features/events/EventContext';
import { insightsApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Badge } from '../../components/ui';
import { AsyncView, PageHeader } from '../../components/common';
import type { EventInsights, ThemeInsight } from '../../types';

type ThemeFilter = 'all' | 'demand' | 'gaps';
type IconName = 'people' | 'booths' | 'checkins' | 'useful' | 'hidden' | 'gauge' | 'trophy' | 'download' | 'info' | 'check';

const iconPaths: Record<IconName, string> = {
  people: 'M16 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2M10 11a4 4 0 100-8 4 4 0 000 8zM20 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75',
  booths: 'M3 9l1.5-5h15L21 9M3 9h18M3 9v11h18V9M9 20v-6h6v6',
  checkins: 'M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h2v2h-2zM18 14h2v6h-2zM14 18h2v2h-2z',
  useful: 'M7 10v11H4V10h3zm0 0l4-7a3 3 0 012 3v4h5a2 2 0 012 2l-2 7a2 2 0 01-2 2H7',
  hidden: 'M3 3l18 18M10.6 10.6a2 2 0 002.8 2.8M9.9 5.2A10.8 10.8 0 0112 5c5 0 9 7 9 7a17 17 0 01-3.1 3.9M6.2 6.2C4.2 7.5 3 10 3 12c0 0 4 7 9 7 1.1 0 2.2-.3 3.2-.8',
  gauge: 'M4.9 19a9 9 0 1114.2 0M12 14l4-5M12 21h.01',
  trophy: 'M8 21h8M12 17v4M7 4h10v5a5 5 0 01-10 0V4zM7 6H4v2a4 4 0 004 4M17 6h3v2a4 4 0 01-4 4',
  download: 'M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3',
  info: 'M12 16v-4m0-4h.01M22 12a10 10 0 11-20 0 10 10 0 0120 0z',
  check: 'M5 12l5 5L20 7',
};

function Icon({ name, size = 20 }: { name: IconName; size?: number }) {
  return <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d={iconPaths[name]} /></svg>;
}

const pct = (v: number | null) => (v === null ? '—' : `${Math.round(v * 100)}%`);

function ExportMenu({ eventId, onError }: { eventId: string; onError: (message: string) => void }) {
  const run = async (action: () => Promise<void>) => {
    try { await action(); } catch (error) { onError((error as Error).message); }
  };
  return (
    <details className="organizer-export">
      <summary><Icon name="download" size={19} /><span>Exportar</span><span className="organizer-export-chevron" aria-hidden="true">⌄</span></summary>
      <div className="organizer-export-menu">
        <button type="button" onClick={() => void run(() => insightsApi.exportBooths(eventId))}>Exportar estandes (CSV)</button>
        <button type="button" onClick={() => void run(() => insightsApi.exportThemes(eventId))}>Exportar temas (CSV)</button>
        <button type="button" onClick={() => window.print()}>Imprimir / salvar como PDF</button>
      </div>
    </details>
  );
}

function Metric({ icon, label, value, hint, className = '' }: {
  icon: IconName; label: string; value: string | number; hint: string; className?: string;
}) {
  return (
    <article className={`organizer-metric ${className}`}>
      <div className={`organizer-metric-icon is-${icon}`}><Icon name={icon} size={23} /></div>
      <span className="organizer-metric-label">{label}</span>
      <strong className="organizer-metric-value">{value}</strong>
      <small className="organizer-metric-hint">{hint}</small>
    </article>
  );
}

function SectionHeading({ title, description }: { title: string; description?: string }) {
  return <header className="organizer-section-heading"><h2>{title}</h2>{description && <p>{description}</p>}</header>;
}

/** O painel do evento também é a página inicial de quem organiza o evento. */
export function OrganizerDashboardPage() {
  const event = useCurrentEvent();
  const { data, loading, error } = useAsync(() => insightsApi.event(event.id), [event.id]);
  const [message, setMessage] = useState<string | null>(null);

  return (
    <div className="organizer-dashboard-page">
      <PageHeader title="Painel do evento" subtitle={event.name}
        actions={<ExportMenu eventId={event.id} onError={setMessage} />} />
      {message && <Alert>{message}</Alert>}
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data && <Dashboard data={data} />}
      </AsyncView>
    </div>
  );
}

function Dashboard({ data }: { data: EventInsights }) {
  const [filter, setFilter] = useState<ThemeFilter>('all');
  const totalWithProfile = data.totals.visitorsWithProfile;
  const themesWithDemand = data.themes.filter((theme) => theme.demand > 0).length;
  const gapIds = useMemo(() => new Set(data.gaps.map((theme) => theme.tagId)), [data.gaps]);
  const visibleThemes = data.themes.filter((theme) => {
    if (filter === 'demand') return theme.demand > 0;
    if (filter === 'gaps') return gapIds.has(theme.tagId);
    return true;
  });
  const groupedThemes = visibleThemes.reduce<Record<string, ThemeInsight[]>>((groups, theme) => {
    const area = theme.area?.trim() || 'Outros temas';
    (groups[area] ??= []).push(theme);
    return groups;
  }, {});
  const maxValue = Math.max(1, ...data.themes.map((theme) => Math.max(theme.demand, theme.supply)));
  const concentration = Math.max(0, Math.min(1, data.exposure.gini));
  const concentrationLabel = concentration >= .65 ? 'Alta' : concentration >= .35 ? 'Média' : 'Baixa';

  return (
    <>
      <div className="organizer-dashboard-notes" aria-live="polite">
        {totalWithProfile < 10 && <div className="organizer-sample-note"><Icon name="info" size={22} /><span>Poucos visitantes com perfil ({totalWithProfile}): os números abaixo são <strong>indicativos, não conclusivos.</strong></span></div>}
        {data.notes.filter((note) => !note.startsWith('Poucos visitantes com perfil')).map((note) => <Alert key={note} tone="info">{note}</Alert>)}
      </div>

      <div className="organizer-dashboard-columns">
        <main className="organizer-dashboard-main">
          <section className="organizer-dashboard-section organizer-overview">
            <SectionHeading title="Visão geral" />
            <div className="organizer-metrics-grid">
              <Metric icon="people" label="Visitantes com perfil" value={totalWithProfile}
                hint={`${data.totals.consentedMatching} autorizaram o matching · ${data.totals.consentedSharing} o compartilhamento`} className="organizer-metric-profile" />
              <Metric icon="booths" label="Estandes" value={data.totals.booths} hint={`${data.totals.boothsWithThemes} com temas definidos`} />
              <Metric icon="checkins" label="Check-ins por QR" value={data.totals.checkIns} hint={`${data.totals.checkedInVisitors} visitantes diferentes`} />
            </div>
          </section>

          <section className="organizer-dashboard-section organizer-quality">
            <SectionHeading title="Qualidade das recomendações" description="Mostra se as sugestões estão sendo úteis e bem distribuídas entre os estandes." />
            <div className="organizer-quality-grid">
              <Metric icon="useful" label="Recomendações úteis" value={pct(data.totals.usefulRate)} hint={`${data.totals.feedbackUseful} sim · ${data.totals.feedbackNotUseful} não`} />
              <Metric icon="hidden" label="Estandes nunca recomendados" value={data.exposure.boothsNeverRecommended} hint={`${data.exposure.boothsRecommended} estande(s) apareceram para alguém`} />
              <article className="organizer-metric organizer-concentration">
                <div className="organizer-metric-icon is-gauge"><Icon name="gauge" size={23} /></div>
                <span className="organizer-metric-label">Concentração de exposição</span>
                <div className="organizer-concentration-value"><strong className="organizer-metric-value">{data.exposure.gini.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</strong><Badge tone={concentration >= .65 ? 'warning' : 'success'}>{concentrationLabel}</Badge></div>
                <div className="organizer-concentration-meter" role="img" aria-label={`Concentração de exposição: ${Math.round(concentration * 100)}%`}><span style={{ width: `${concentration * 100}%` }} /></div>
                <div className="organizer-concentration-scale"><span>Igual para todos</span><span>Concentrada</span></div>
                <small className="organizer-metric-hint">Os 20% mais recomendados ficam com {pct(data.exposure.top20Share)} das recomendações.</small>
              </article>
            </div>
          </section>

          {data.exposure.mostRecommended && (
            <section className="organizer-most-recommended">
              <div className="organizer-most-icon"><Icon name="trophy" size={24} /></div>
              <div><span>Estande mais recomendado</span><p><strong>{data.exposure.mostRecommended}</strong><span> · {data.exposure.mostRecommendedCount} visitante(s)</span></p></div>
            </section>
          )}

          <section className="organizer-dashboard-section organizer-gaps">
            <SectionHeading title="Lacunas" description="Temas que os visitantes procuram e nenhum estande oferece." />
            {data.gaps.length === 0
              ? <div className="organizer-empty-gap"><span><Icon name="check" size={20} /></span><div><strong>Nenhuma lacuna encontrada</strong><p>Todo tema procurado tem ao menos um estande que oferece.</p></div></div>
              : <div className="organizer-gap-list">{data.gaps.map((gap) => <Badge key={gap.tagId} tone="warning">{gap.name} · {gap.demand}</Badge>)}</div>}
          </section>

          <section className="organizer-dashboard-section organizer-themes">
            <div className="organizer-themes-heading">
              <SectionHeading title="Demanda × oferta por tema" />
              <div className="organizer-theme-filters" role="group" aria-label="Filtrar temas">
                <button type="button" className={filter === 'all' ? 'is-active' : ''} onClick={() => setFilter('all')}>Todos ({data.themes.length})</button>
                <button type="button" className={filter === 'demand' ? 'is-active' : ''} onClick={() => setFilter('demand')}>Com demanda ({themesWithDemand})</button>
                <button type="button" className={filter === 'gaps' ? 'is-active' : ''} onClick={() => setFilter('gaps')}>Lacunas ({data.gaps.length})</button>
              </div>
            </div>
            <div className="organizer-theme-legend"><span><i className="demand-dot" />Demanda (o que visitantes procuram)</span><span><i className="supply-dot" />Oferta (o que estandes oferecem)</span></div>
            <div className="organizer-theme-card">
              {visibleThemes.length === 0 && <p className="muted">Nenhum tema nesta categoria.</p>}
              {Object.entries(groupedThemes).map(([area, themes]) => (
                <section className="organizer-theme-group" key={area}>
                  <h3>{area}</h3>
                  {themes.map((theme) => {
                    const status = theme.demand > 0 && theme.supply > 0 ? ['Atendido', 'success'] as const
                      : theme.demand > 0 ? ['Lacuna', 'warning'] as const
                        : theme.supply > 0 ? ['Só oferta', 'brand'] as const : ['Sem dados', 'neutral'] as const;
                    return <article className="organizer-theme-row" key={theme.tagId}>
                      <div className="organizer-theme-row-heading"><strong>{theme.name}</strong><Badge tone={status[1]}>{status[0]}</Badge></div>
                      <div className="organizer-theme-counts"><span><strong>{theme.demand}</strong> demanda</span><span><strong>{theme.supply}</strong> oferta</span></div>
                      <div className="organizer-theme-bars" aria-hidden="true">
                        <div className="organizer-theme-track"><span className="demand-bar" style={{ width: `${(theme.demand / maxValue) * 100}%` }} /></div>
                        <div className="organizer-theme-track"><span className="supply-bar" style={{ width: `${(theme.supply / maxValue) * 100}%` }} /></div>
                      </div>
                    </article>;
                  })}
                </section>
              ))}
            </div>
          </section>
        </main>

        <aside className="organizer-dashboard-aside">
          <section className="organizer-dashboard-section organizer-profiles">
            <SectionHeading title="Perfis em comum" description="Segmentos mais frequentes entre os visitantes." />
            {data.profileGroups.length === 0 ? <p className="muted">Ainda não há segmentos suficientes para agrupar.</p> : (
              <div className="organizer-profile-list">
                {data.profileGroups.map((group) => <article className="organizer-profile-group" key={group.area}>
                  <div><strong>{group.area}</strong><span>{group.visitors} {group.visitors === 1 ? 'visitante' : 'visitantes'}</span></div>
                  {group.topThemes.length > 0 && <div className="organizer-profile-themes">{group.topThemes.map((theme) => <Badge key={theme}>{theme}</Badge>)}</div>}
                </article>)}
              </div>
            )}
          </section>

          <section className="organizer-dashboard-section organizer-top-terms">
            <SectionHeading title="O que mais aparece nos objetivos" />
            {data.topTerms.length === 0 ? <p className="muted">Ainda não há termos repetidos por pelo menos dois visitantes.</p> : (
              <div className="organizer-top-term-list">{data.topTerms.map((term) => <Badge key={term.term} tone="brand">{term.term} <strong>{term.count}</strong></Badge>)}</div>
            )}
          </section>
          <small className="organizer-dashboard-footnote">Leitura automática dos dados cadastrados, sem inteligência artificial. Só considera visitantes que autorizaram o uso do perfil.</small>
        </aside>
      </div>
    </>
  );
}
