import type { ReactNode } from 'react';
import { Alert, Button, Loading } from '../ui';

export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: string; actions?: ReactNode }) {
  return (
    <div className="page-header">
      <div><h1>{title}</h1>{subtitle && <p className="muted">{subtitle}</p>}</div>
      {actions}
    </div>
  );
}

export function Pagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (p: number) => void }) {
  if (totalPages <= 1) return null;
  return (
    <div className="pagination">
      <Button variant="secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>Anterior</Button>
      <span className="muted">Página {page} de {totalPages}</span>
      <Button variant="secondary" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>Próxima</Button>
    </div>
  );
}

/** Renderiza loading/erro/conteúdo de forma padronizada. */
export function AsyncView({ loading, error, hasData, children }: { loading: boolean; error?: Error; hasData: boolean; children: ReactNode }) {
  if (error) return <Alert>{error.message}</Alert>;
  if (loading && !hasData) return <Loading />;
  return <>{children}</>;
}
