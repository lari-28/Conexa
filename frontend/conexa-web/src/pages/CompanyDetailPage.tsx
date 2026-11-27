import { Link, useLocation, useParams } from 'react-router-dom';
import { useEffect } from 'react';
import { useCurrentEvent } from '../features/events/EventContext';
import { companiesApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Avatar, Badge, Card } from '../components/ui';
import { AsyncView } from '../components/common';
import { EventMaps } from '../components/EventMaps';
import { BoothComments } from '../features/companies/BoothComments';

export function CompanyDetailPage() {
  const { id = '' } = useParams();
  const location = useLocation();
  const ev = useCurrentEvent();
  const { data: c, loading, error } = useAsync(() => companiesApi.get(ev.id, id), [ev.id, id]);

  useEffect(() => {
    if (!c || location.hash !== '#booth-comments') return;
    const frame = window.requestAnimationFrame(() => {
      document.getElementById('booth-comments')?.scrollIntoView({ block: 'start' });
    });
    return () => window.cancelAnimationFrame(frame);
  }, [c, location.hash]);

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
              {c.idealCustomer && <p><strong>Cliente ideal:</strong> {c.idealCustomer}</p>}
              {c.tags.length > 0 && <div className="chips">{c.tags.map((t) => <Badge key={t.id} tone="success">{t.name}</Badge>)}</div>}
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
      {c && <BoothComments eventId={ev.id} companyId={c.id} companyName={c.name} />}
      <EventMaps eventId={ev.id} />
    </>
  );
}
