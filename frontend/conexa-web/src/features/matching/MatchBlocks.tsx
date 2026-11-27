import { useState } from 'react';
import { Link } from 'react-router-dom';
import { companiesApi, engagementApi, insightsApi, matchingApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Avatar, Loading } from '../../components/ui';
import type { MatchBand } from '../../types';

const bandLabel: Record<MatchBand, string> = { High: 'Afinidade alta', Medium: 'Afinidade média', Low: 'Afinidade baixa', None: '' };

function Band({ band }: { band: MatchBand }) {
  return <span className={`badge badge-match band-${band.toLowerCase()}`}>{bandLabel[band]}</span>;
}

/** "Isso foi útil?": alimenta o reajuste dos pesos com dados reais. */
function Feedback({ eventId, companyId }: { eventId: string; companyId: string }) {
  const [sent, setSent] = useState<boolean | null>(null);
  const send = (useful: boolean) => { setSent(useful); engagementApi.feedback(eventId, companyId, useful).catch(() => setSent(null)); };
  return (
    <span className="fb small">
      <span className="muted">Isso foi útil?</span>
      <button type="button" className={`chip ${sent === true ? 'chip-on' : ''}`} aria-pressed={sent === true} onClick={() => send(true)}>Sim</button>
      <button type="button" className={`chip ${sent === false ? 'chip-on' : ''}`} aria-pressed={sent === false} onClick={() => send(false)}>Não</button>
    </span>
  );
}

const note ='Recomendação automática por afinidade com o que foi informado. Pode errar: use como ponto de partida.';

/** "Estandes para você": recomendações para o visitante, com o motivo de cada uma. */
export function BoothMatchesBlock({ eventId }: { eventId: string }) {
  const { data, loading, error } = useAsync(() => matchingApi.booths(eventId), [eventId]);
  return (
    <div className="ai-block">
      <span className="ai-tag">Recomendações</span>
      <h3>Estandes para você</h3>
      {loading && !data && <Loading />}
      {error && <Alert>{error.message}</Alert>}
      {data && !data.ready && (
        <>
          <p className="muted" style={{ margin: 0 }}>{data.message}</p>
          <div><Link className="btn btn-primary" to="/evento/meu-perfil">Completar meu perfil</Link></div>
        </>
      )}
      {data?.ready && data.items.length === 0 && <p className="muted" style={{ margin: 0 }}>{data.message}</p>}
      {data?.ready && data.items.map((m) => (
        <div key={m.companyId} className="example-item" style={{ alignItems: 'flex-start' }}>
          <div className="grow">
            <Link to={`/empresas/${m.companyId}`}><strong>{m.name}</strong></Link>
            {m.boothLocation && <span className="muted small"> · {m.boothLocation}</span>}
            <ul className="plain small muted" style={{ marginTop: '.25rem' }}>
              {m.reasons.map((r) => <li key={r}>{r}</li>)}
            </ul>
            <Feedback eventId={eventId} companyId={m.companyId} />
          </div>
          <Band band={m.band} />
        </div>
      ))}
      {data?.ready && data.items.length > 0 && <small className="muted">{note}</small>}
    </div>
  );
}

const LEADS_PREVIEW = 2;
const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`;

/** "Leads para sua empresa": só visitantes que autorizaram o compartilhamento. */
export function LeadsBlock({ eventId }: { eventId: string }) {
  const [filter, setFilter] = useState<'all' | 'high'>('all');
  const [showAll, setShowAll] = useState(false);
  const { data, loading, error } = useAsync(async () => {
    const mine = await companiesApi.mine(eventId);
    if (!mine[0]) return null;
    const [leads, report] = await Promise.all([matchingApi.leads(eventId, mine[0].id), insightsApi.booth(eventId, mine[0].id)]);
    return { company: mine[0], leads, report };
  }, [eventId]);

  const all = data?.leads.items ?? [];
  const high = all.filter((l) => l.band === 'High');
  const filtered = filter === 'high' ? high : all;
  const visible = showAll ? filtered : filtered.slice(0, LEADS_PREVIEW);
  const choose = (next: 'all' | 'high') => { setFilter(next); setShowAll(false); };

  return (
    <div className="ai-block">
      <span className="ai-tag">Recomendações</span>
      <h3>Leads para sua empresa</h3>
      {loading && !data && <Loading />}
      {error && <Alert>{error.message}</Alert>}
      {data === null && (
        <>
          <p className="muted" style={{ margin: 0 }}>Cadastre seu estande para ver os visitantes com afinidade.</p>
          <div><Link className="btn btn-primary" to="/evento/meu-estande">Cadastrar estande</Link></div>
        </>
      )}
      {data && (
        <p className="small muted exhibitor-leads-summary">
          {plural(data.report.recommendedTo, 'visitante recomendado', 'visitantes recomendados')} · {data.report.highAffinity} com afinidade alta
          · {data.report.feedbackUseful} {data.report.feedbackUseful === 1 ? 'útil' : 'úteis'} · {data.report.feedbackNotUseful} {data.report.feedbackNotUseful === 1 ? 'não útil' : 'não úteis'}
        </p>
      )}
      {data && all.length === 0 && (
        <p className="muted" style={{ margin: 0 }}>
          Nenhum visitante com afinidade por enquanto. Só aparecem visitantes que autorizaram o compartilhamento
          ({data.leads.consentingVisitors} até agora).
        </p>
      )}
      {data && all.length > 0 && (
        <div className="exhibitor-lead-filters" role="group" aria-label="Filtrar leads">
          <button type="button" className={`chip ${filter === 'all' ? 'chip-on' : ''}`} aria-pressed={filter === 'all'} onClick={() => choose('all')}>Todos ({all.length})</button>
          <button type="button" className={`chip ${filter === 'high' ? 'chip-on' : ''}`} aria-pressed={filter === 'high'} onClick={() => choose('high')}>Afinidade alta ({high.length})</button>
        </div>
      )}
      {data && all.length > 0 && filtered.length === 0 && <p className="muted" style={{ margin: 0 }}>Nenhum lead com afinidade alta por enquanto.</p>}
      {data && visible.map((l) => {
        const hasContact = Boolean(l.email || l.phone);
        return (
          <article key={l.userId} className="example-item exhibitor-lead">
            <div className="exhibitor-lead-heading">
              <Avatar name={l.displayName} size={40} />
              <div className="exhibitor-lead-identity">
                <Link to={`/participantes/${l.userId}`}><strong>{l.displayName}</strong></Link>
                <span className="muted small">{[l.jobTitle, l.companyName].filter(Boolean).join(' · ')}</span>
              </div>
              <Band band={l.band} />
            </div>
            {l.goal && <p className="small exhibitor-lead-goal">“{l.goal}”</p>}
            <ul className="plain small muted exhibitor-lead-interests">{l.reasons.map((r) => <li key={r}>{r}</li>)}</ul>
            <div className={`small exhibitor-lead-contact${hasContact ? '' : ' is-hidden'}`}>
              <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                {hasContact ? <><rect x="3" y="5" width="18" height="14" rx="2" /><path d="M3 7l9 6 9-6" /></> : <><rect x="5" y="11" width="14" height="10" rx="2" /><path d="M8 11V8a4 4 0 0 1 8 0v3" /></>}
              </svg>
              {l.email && <a href={`mailto:${l.email}`}>{l.email}</a>}
              {l.phone && <span>{l.phone}</span>}
              {!hasContact && <span>Contato oculto: o visitante não autorizou.</span>}
            </div>
            <div className="exhibitor-lead-actions">
              {l.email
                ? <a className="btn btn-primary" href={`mailto:${l.email}`}>Entrar em contato</a>
                : <Link className="btn" to={`/participantes/${l.userId}`}>Ver perfil</Link>}
            </div>
          </article>
        );
      })}
      {data && filtered.length > LEADS_PREVIEW && (
        <button type="button" className="btn exhibitor-leads-more" aria-expanded={showAll} onClick={() => setShowAll((v) => !v)}>
          {showAll ? 'Mostrar menos' : `Ver todos os ${filtered.length} leads`}
        </button>
      )}
      {data && all.length > 0 && (
        <>
          <button type="button" className="btn" onClick={() => insightsApi.exportLeads(eventId, data.company.id).catch(() => undefined)}>Exportar leads (CSV)</button>
          <small className="muted">{note}</small>
        </>
      )}
    </div>
  );
}
