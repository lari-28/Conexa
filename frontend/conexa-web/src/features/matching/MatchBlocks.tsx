import { useState } from 'react';
import { Link } from 'react-router-dom';
import { companiesApi, engagementApi, matchingApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { Alert, Avatar, Loading } from '../../components/ui';
import type { MatchBand } from '../../types';

const bandLabel: Record<MatchBand, string> = { High: 'Afinidade alta', Medium: 'Afinidade média', Low: 'Afinidade baixa', None: '' };

function Band({ band }: { band: MatchBand }) {
  return <span className="badge badge-match">{bandLabel[band]}</span>;
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

/** "Leads para sua empresa": só visitantes que autorizaram o compartilhamento. */
export function LeadsBlock({ eventId }: { eventId: string }) {
  const { data, loading, error } = useAsync(async () => {
    const mine = await companiesApi.mine(eventId);
    if (!mine[0]) return null;
    return { company: mine[0], leads: await matchingApi.leads(eventId, mine[0].id) };
  }, [eventId]);

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
      {data && data.leads.items.length === 0 && (
        <p className="muted" style={{ margin: 0 }}>
          Nenhum visitante com afinidade por enquanto. Só aparecem visitantes que autorizaram o compartilhamento
          ({data.leads.consentingVisitors} até agora).
        </p>
      )}
      {data && data.leads.items.map((l) => (
        <div key={l.userId} className="example-item" style={{ alignItems: 'flex-start' }}>
          <Avatar name={l.displayName} size={36} />
          <div className="grow">
            <Link to={`/participantes/${l.userId}`}><strong>{l.displayName}</strong></Link>
            <span className="muted small">{[l.jobTitle, l.companyName].filter(Boolean).map((s) => ` · ${s}`).join('')}</span>
            {l.goal && <p className="small" style={{ margin: '.25rem 0' }}>“{l.goal}”</p>}
            <ul className="plain small muted">{l.reasons.map((r) => <li key={r}>{r}</li>)}</ul>
            {(l.email || l.phone) && <div className="small">{l.email && <a href={`mailto:${l.email}`}>{l.email}</a>} {l.phone}</div>}
          </div>
          <Band band={l.band} />
        </div>
      ))}
      {data && data.leads.items.length > 0 && <small className="muted">{note}</small>}
    </div>
  );
}
