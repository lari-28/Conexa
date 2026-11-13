# Shared UI components

Framework: React 19 with TypeScript. Components are custom React components styled with `src/index.css`; there is no external component library.

## `src/components/ui/index.tsx`

Exports button, form-field, avatar, card, badge, alert, loading, empty-state, and modal primitives.

```tsx
import { useEffect, useId } from 'react';
import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react';

export function Button({
  variant = 'primary', loading = false, className = '', children, disabled, ...rest
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'danger' | 'ghost'; loading?: boolean }) {
  return (
    <button className={`btn btn-${variant} ${className}`} disabled={disabled || loading} {...rest}>
      {loading ? 'Aguarde…' : children}
    </button>
  );
}

interface FieldProps { label: string; error?: string; hint?: string }

export function Input({ label, error, hint, ...rest }: FieldProps & InputHTMLAttributes<HTMLInputElement>) {
  const id = useId();
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input id={id} aria-invalid={!!error} {...rest} />
      {hint && !error && <small className="hint">{hint}</small>}
      {error && <small className="error-text">{error}</small>}
    </div>
  );
}

export function Textarea({ label, error, hint, ...rest }: FieldProps & TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const id = useId();
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <textarea id={id} rows={3} aria-invalid={!!error} {...rest} />
      {hint && !error && <small className="hint">{hint}</small>}
      {error && <small className="error-text">{error}</small>}
    </div>
  );
}

export function Select({
  label, error, options, placeholder, ...rest
}: FieldProps & SelectHTMLAttributes<HTMLSelectElement> & { options: { value: string; label: string }[]; placeholder?: string }) {
  const id = useId();
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <select id={id} {...rest}>
        {placeholder !== undefined && <option value="">{placeholder}</option>}
        {options.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
      {error && <small className="error-text">{error}</small>}
    </div>
  );
}

export function Avatar({ name, url, size = 40 }: { name: string; url?: string | null; size?: number }) {
  const initials = name.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase()).join('');
  const style = { width: size, height: size, fontSize: size * 0.4 };
  return url
    ? <img className="avatar" src={url} alt={name} style={style} referrerPolicy="no-referrer" />
    : <span className="avatar avatar-initials" style={style} aria-label={name}>{initials}</span>;
}

export function Card({ children, onClick, className = '' }: { children: ReactNode; onClick?: () => void; className?: string }) {
  return (
    <div className={`card ${onClick ? 'card-click' : ''} ${className}`} onClick={onClick}
      role={onClick ? 'button' : undefined} tabIndex={onClick ? 0 : undefined}
      onKeyDown={onClick ? (e) => { if (e.key === 'Enter') onClick(); } : undefined}>
      {children}
    </div>
  );
}

export function Badge({ children, tone = 'neutral' }: { children: ReactNode; tone?: 'neutral' | 'brand' | 'success' | 'warning' | 'danger' }) {
  return <span className={`badge badge-${tone}`}>{children}</span>;
}

export function Alert({ tone = 'error', children }: { tone?: 'error' | 'success' | 'info'; children: ReactNode }) {
  return <div className={`alert alert-${tone}`} role={tone === 'error' ? 'alert' : 'status'}>{children}</div>;
}

export function Loading({ label = 'Carregando…' }: { label?: string }) {
  return <div className="loading" role="status"><span className="spinner" />{label}</div>;
}

export function EmptyState({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="empty">
      <strong>{title}</strong>
      {description && <p>{description}</p>}
      {action}
    </div>
  );
}

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" role="dialog" aria-modal="true" aria-label={title} onClick={(e) => e.stopPropagation()}>
        <div className="modal-head"><h3>{title}</h3><button className="btn btn-ghost" onClick={onClose} aria-label="Fechar">✕</button></div>
        {children}
      </div>
    </div>
  );
}
```

## `src/components/common/index.tsx`

```tsx
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
```

## Feature-level shared cards

- `src/features/participants/ParticipantCard.tsx`: participant summary card with avatar, name, job/company, event-role badge, area, goals, and interests.
- `src/features/forum/PostCard.tsx`: forum post with reactions, comments, report modal, and moderation actions.
