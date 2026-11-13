import { useState } from 'react';
import { adminApi } from '../../services/api/endpoints';
import { useAsync } from '../../hooks/useAsync';
import { useDebounce } from '../../hooks/useDebounce';
import { useAuth } from '../../features/auth/AuthContext';
import { Alert, Badge, Button, Card, Input } from '../../components/ui';
import { AsyncView, PageHeader, Pagination } from '../../components/common';
import { roleLabel } from '../../utils/format';

export function AdminUsersPage() {
  const { user } = useAuth();
  const [q, setQ] = useState('');
  const [page, setPage] = useState(1);
  const dq = useDebounce(q);
  const { data, loading, error, reload } = useAsync(() => adminApi.users(dq, page), [dq, page]);
  const [msg, setMsg] = useState<string | null>(null);

  const toggle = async (id: string, isActive: boolean) => {
    setMsg(null);
    try { await adminApi.setActive(id, isActive); reload(); } catch (e) { setMsg((e as Error).message); }
  };

  return (
    <>
      <PageHeader title="Usuários (admin)" />
      <Input label="Buscar" placeholder="Nome ou e-mail" value={q} onChange={(e) => { setQ(e.target.value); setPage(1); }} />
      {msg && <Alert>{msg}</Alert>}
      <AsyncView loading={loading} error={error} hasData={!!data}>
        <div className="stack">
          {data?.items.map((u) => (
            <Card key={u.id}>
              <div className="row wrap">
                <div className="grow"><strong>{u.displayName}</strong><div className="muted small">{u.email}</div></div>
                {u.roles.map((r) => <Badge key={r} tone="brand">{roleLabel[r] ?? r}</Badge>)}
                <Badge tone={u.isActive ? 'success' : 'danger'}>{u.isActive ? 'Ativo' : 'Suspenso'}</Badge>
              </div>
              {u.id !== user?.id && (
                <Button variant={u.isActive ? 'danger' : 'secondary'} onClick={() => void toggle(u.id, !u.isActive)}>
                  {u.isActive ? 'Suspender conta' : 'Reativar conta'}
                </Button>
              )}
            </Card>
          ))}
        </div>
        {data && <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />}
      </AsyncView>
    </>
  );
}
