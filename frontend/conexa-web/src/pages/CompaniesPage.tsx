import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCurrentEvent } from '../features/events/EventContext';
import { companiesApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { useDebounce } from '../hooks/useDebounce';
import { Badge, Card, EmptyState, Input } from '../components/ui';
import { AsyncView, PageHeader, Pagination } from '../components/common';
import { EventMaps } from '../components/EventMaps';

export function CompaniesPage() {
  const ev = useCurrentEvent();
  const navigate = useNavigate();
  const [q, setQ] = useState('');
  const [page, setPage] = useState(1);
  const dq = useDebounce(q);
  const { data, loading, error } = useAsync(() => companiesApi.list(ev.id, { q: dq, page }), [ev.id, dq, page]);

  return (
    <>
      <PageHeader title="Empresas e expositores" />
      <EventMaps eventId={ev.id} />
      <Input label="Buscar" placeholder="Nome ou segmento" value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }} />
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data && data.items.length === 0 ? <EmptyState title="Nenhuma empresa encontrada" /> : (
          <div className="stack">
            {data?.items.map((c) => (
              <Card key={c.id} onClick={() => navigate(`/empresas/${c.id}`)}>
                <strong>{c.name}</strong>
                {c.segment && <div><Badge tone="brand">{c.segment}</Badge></div>}
                {c.description && <p className="muted">{c.description}</p>}
                <div className="muted small">
                  {c.boothLocation && <>📍 {c.boothLocation} · </>}{c.representativeCount} representante(s)
                </div>
              </Card>
            ))}
          </div>
        )}
        {data && <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />}
      </AsyncView>
    </>
  );
}
