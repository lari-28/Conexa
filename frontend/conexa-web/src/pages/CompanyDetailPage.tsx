import { Link, useParams } from 'react-router-dom';
import { useCurrentEvent } from '../features/events/EventContext';
import { companiesApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Avatar, Badge, Card } from '../components/ui';
import { AsyncView } from '../components/common';

export function CompanyDetailPage() {
  const { id = '' } = useParams();
  const ev = useCurrentEvent();
  const { data: c, loading, error } = useAsync(() => companiesApi.get(ev.id, id), [ev.id, id]);

  return (
    <>
      <Link to="/empresas" className="back">← Voltar</Link>
      <AsyncView loading={loading} error={error} hasData={!!c}>
        {c && (
          <>
            <Card>
              <h1>{c.name}</h1>
              {c.segment && <Badge tone="brand">{c.segment}</Badge>}
              {c.description && <p>{c.description}</p>}
              {c.productsServices && <p><strong>Produtos e serviços:</strong> {c.productsServices}</p>}
              {c.boothLocation && <p>📍 {c.boothLocation}</p>}
              {c.website && <p>🔗 <a href={c.website} target="_blank" rel="noreferrer noopener">{c.website}</a></p>}
            </Card>
            <h2>Representantes</h2>
            {c.representatives.length === 0 ? <p className="muted">Nenhum representante vinculado.</p> : (
              <div className="stack">
                {c.representatives.map((r) => (
                  <Card key={r.userId}>
                    <div className="row">
                      <Avatar name={r.displayName} url={r.photoUrl} />
                      <div className="grow">
                        <Link to={`/participantes/${r.userId}`}><strong>{r.displayName}</strong></Link>
                        {r.title && <div className="muted">{r.title}</div>}
                      </div>
                    </div>
                    {r.email && <div className="small">✉️ <a href={`mailto:${r.email}`}>{r.email}</a></div>}
                    {r.phone && <div className="small">📞 {r.phone}</div>}
                  </Card>
                ))}
              </div>
            )}
          </>
        )}
      </AsyncView>
    </>
  );
}
