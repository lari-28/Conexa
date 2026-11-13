import { useState } from 'react';
import { useCurrentEvent } from '../features/events/EventContext';
import { ParticipantCard } from '../features/participants/ParticipantCard';
import { participantsApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { useDebounce } from '../hooks/useDebounce';
import { EmptyState, Input, Select } from '../components/ui';
import { AsyncView, PageHeader, Pagination } from '../components/common';

export function ParticipantsPage() {
  const ev = useCurrentEvent();
  const [q, setQ] = useState('');
  const [area, setArea] = useState('');
  const [interest, setInterest] = useState('');
  const [page, setPage] = useState(1);
  const dq = useDebounce(q);

  const filters = useAsync(() => participantsApi.filters(ev.id), [ev.id]);
  const list = useAsync(
    () => participantsApi.list(ev.id, { q: dq, area, interest, page }),
    [ev.id, dq, area, interest, page],
  );
  const reset = (fn: () => void) => { fn(); setPage(1); };

  return (
    <>
      <PageHeader title="Pessoas" subtitle="Encontre quem você quer conhecer no evento." />
      <div className="filters">
        <Input label="Buscar" placeholder="Nome, empresa ou área" value={q} onChange={(e) => reset(() => setQ(e.target.value))} />
        <Select label="Área" placeholder="Todas" value={area} onChange={(e) => reset(() => setArea(e.target.value))}
          options={(filters.data?.areas ?? []).map((a) => ({ value: a, label: a }))} />
        <Select label="Interesse" placeholder="Todos" value={interest} onChange={(e) => reset(() => setInterest(e.target.value))}
          options={(filters.data?.interests ?? []).map((i) => ({ value: i, label: i }))} />
      </div>
      <AsyncView loading={list.loading} error={list.error} hasData={!!list.data}>
        {list.data && list.data.items.length === 0
          ? <EmptyState title="Nenhuma pessoa encontrada" description="Ajuste a busca ou os filtros." />
          : <div className="stack">{list.data?.items.map((p) => <ParticipantCard key={p.userId} p={p} />)}</div>}
        {list.data && <Pagination page={list.data.page} totalPages={list.data.totalPages} onChange={setPage} />}
      </AsyncView>
    </>
  );
}
