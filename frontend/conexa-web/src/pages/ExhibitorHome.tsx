import { Link } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import type { EventInfo } from '../types';
import { companiesApi, insightsApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Alert, Loading } from '../components/ui';
import { LeadsBlock } from '../features/matching/MatchBlocks';

const COMMENTS_PREVIEW = 2;

export function ExhibitorHome({ event }: { event: EventInfo }) {
  const { user } = useAuth();
  const { data, loading, error } = useAsync(async () => {
    const mine = await companiesApi.mine(event.id);
    if (!mine[0]) return null;
    const [company, report, comments] = await Promise.all([
      companiesApi.get(event.id, mine[0].id),
      insightsApi.booth(event.id, mine[0].id),
      companiesApi.comments(event.id, mine[0].id),
    ]);
    return { company, report, comments };
  }, [event.id]);
  const firstName = (user?.displayName ?? '').trim().split(/\s+/)[0];
  const dateRange = `${new Date(event.startsAt).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })} – ${new Date(event.endsAt).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' })}`;

  const company = data ? data.company : null;
  const setupSteps = [
    Boolean(company?.name.trim()),
    Boolean(company?.productsServices?.trim() && company?.idealCustomer?.trim()),
    Boolean(company?.tags.length),
  ];
  const setupDone = setupSteps.filter(Boolean).length;
  const recentComments = data?.comments.items
    .slice()
    .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
    .slice(0, COMMENTS_PREVIEW) ?? [];

  return <div className="exhibitor-home">
    <header className="participant-welcome">
      <div className="participant-greeting-copy">
        <h1>Olá{firstName ? `, ${firstName}` : ''}</h1>
        <p>{company?.name ?? event.name} · {dateRange}</p>
      </div>
      <span className="participant-role">Expositor</span>
    </header>

    {loading && !data && <Loading label="Carregando o painel do estande…" />}
    {error && !data && <Alert>Não foi possível carregar os dados do estande. Tente novamente mais tarde.</Alert>}
    {data === null && <section className="participant-next exhibitor-setup">
      <div className="participant-next-copy">
        <span className="participant-eyebrow">COMECE POR AQUI</span>
        <h2>Cadastre seu estande</h2>
        <p>Apresente sua oferta, o cliente que quer atender e os temas da sua empresa para aparecer nas recomendações.</p>
      </div>
      <Link className="btn btn-primary participant-next-action" to="/evento/meu-estande">Cadastrar meu estande</Link>
    </section>}

    {data && <>
      {/* 1. Desempenho do estande */}
      <section className="exhibitor-performance" aria-labelledby="exhibitor-performance-title">
        <div className="exhibitor-section-heading">
          <h2 id="exhibitor-performance-title">Desempenho do seu estande</h2>
          <span className="exhibitor-updated">Atualizado agora</span>
        </div>
        <div className="exhibitor-metrics">
          <article className="exhibitor-metric exhibitor-metric-checkins">
            <div><span>Check-ins por QR</span><strong>{data.report.checkIns}</strong><small>{data.report.checkedInVisitors} {data.report.checkedInVisitors === 1 ? 'visitante único' : 'visitantes únicos'}</small></div>
            <svg className="exhibitor-sparkline" viewBox="0 0 120 56" role="img" aria-label="Gráfico ilustrativo dos check-ins">
              <g fill="currentColor" opacity=".35"><rect x="0" y="40" width="10" height="16" rx="2"/><rect x="16" y="34" width="10" height="22" rx="2"/><rect x="32" y="38" width="10" height="18" rx="2"/><rect x="48" y="26" width="10" height="30" rx="2"/><rect x="64" y="30" width="10" height="26" rx="2"/><rect x="80" y="18" width="10" height="38" rx="2"/><rect x="96" y="22" width="10" height="34" rx="2"/></g>
              <rect x="110" y="6" width="10" height="50" rx="2" fill="currentColor"/>
            </svg>
          </article>
          <article className="exhibitor-metric"><span>Recomendações a visitantes</span><strong>{data.report.recommendedTo}</strong><small className="is-affinity">{data.report.highAffinity} com afinidade alta</small></article>
          <article className="exhibitor-metric"><span>Comentários dos visitantes</span><strong>{data.comments.totalCount}</strong><small>{data.comments.canView ? 'De visitantes do evento' : 'Disponíveis após check-in'}</small></article>
        </div>
      </section>

      {/* 2. Próximo passo */}
      <section className="participant-next exhibitor-setup">
        <div className="participant-next-copy">
          <div className="exhibitor-setup-kicker"><span className="participant-eyebrow">PRÓXIMO PASSO</span><span>{setupDone} de 3</span></div>
          <div className="exhibitor-progress" role="progressbar" aria-label="Cadastro do estande" aria-valuemin={0} aria-valuemax={3} aria-valuenow={setupDone}>
            {setupSteps.map((done, i) => <span key={i} className={done ? 'is-done' : undefined} />)}
          </div>
          <h2>{setupDone === 3 ? 'Seu estande está completo' : 'Cadastre seu estande'}</h2>
          <p>{setupDone === 3 ? 'Sua oferta e seus temas estão prontos para ajudar visitantes a encontrar sua empresa.' : 'O que oferece, quem quer atender e até 3 temas. Isso melhora as recomendações de visitantes.'}</p>
        </div>
        <Link className="btn btn-primary participant-next-action" to="/evento/meu-estande">{setupDone === 3 ? 'Editar meu estande' : 'Continuar'}</Link>
        {setupDone < 3 && <small className="muted exhibitor-setup-after">Depois: definir temas · adicionar representantes</small>}
      </section>

      {/* 3. Leads recomendados */}
      <section className="exhibitor-leads"><LeadsBlock eventId={event.id} /></section>

      {/* 4. Comentários dos visitantes */}
      <section className="exhibitor-comments" aria-labelledby="exhibitor-comments-title">
        <div className="exhibitor-section-heading compact">
          <h2 id="exhibitor-comments-title">Comentários dos visitantes</h2>
          {data.comments.totalCount > recentComments.length && <Link to={`/empresas/${data.company.id}#booth-comments`}>Ver todos ({data.comments.totalCount})</Link>}
        </div>
        {!data.comments.canView ? <p className="muted">Os comentários ficam disponíveis depois que um participante faz check-in no estande.</p>
          : recentComments.length === 0 ? <p className="muted">Ainda não há comentários. Eles aparecerão aqui quando visitantes compartilharem uma impressão.</p>
            : <div className="exhibitor-comment-list">{recentComments.map((comment) => <article className="exhibitor-comment" key={comment.id}>
              <div><div className="exhibitor-comment-meta"><strong>{comment.author.displayName}</strong><time dateTime={comment.createdAt}>{new Date(comment.createdAt).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })}</time></div><p>{comment.text}</p></div>
            </article>)}</div>}
      </section>
    </>}

    {/* 5. Sobre o evento (secundário) */}
    <details className="participant-about">
      <summary><strong>Sobre o evento</strong><span>Ver detalhes <span aria-hidden="true">⌄</span></span></summary>
      <div className="participant-about-content">{event.location && <p>{event.location}</p>}{event.description && <p>{event.description}</p>}</div>
    </details>
  </div>;
}
