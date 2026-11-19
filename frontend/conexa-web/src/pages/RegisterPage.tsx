import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { ApiError } from '../services/api/client';
import { Alert, Button, Input } from '../components/ui';
import { contactEmail, profiles } from '../content/profiles';
import type { ProfileSlug } from '../content/profiles';

export function RegisterPage() {
  const { tipo } = useParams<{ tipo: string }>();
  const profile = tipo && tipo in profiles ? profiles[tipo as ProfileSlug] : null;
  if (!profile) return <Navigate to="/cadastro" replace />;
  if (!profile.accountType) return <OrganizerContact />;
  return <RegisterForm slug={profile.slug} accountType={profile.accountType} />;
}

/** Organizador não tem cadastro público (regra do backend): leva ao contato comercial. */
function OrganizerContact() {
  const p = profiles.feira;
  const href = contactEmail ? `mailto:${contactEmail}?subject=${encodeURIComponent('Quero usar o Conexa na minha feira')}` : undefined;
  return (
    <div className="auth-page">
      <div className="card auth-card">
        <Link to="/" className="logo-lg">CONEXA</Link>
        <h1>{p.title}</h1>
        <p className="muted">{p.pitch}</p>
        {href
          ? <a className="btn btn-primary" href={href}>{p.cta}</a>
          : <Alert tone="info">O contato comercial ainda não foi configurado (VITE_CONTACT_EMAIL).</Alert>}
        <p className="muted center"><Link to="/cadastro">Voltar</Link> · <Link to="/login">Já tenho acesso</Link></p>
      </div>
    </div>
  );
}

function RegisterForm({ slug, accountType }: { slug: ProfileSlug; accountType: 'Participant' | 'Exhibitor' }) {
  const { register } = useAuth();
  const navigate = useNavigate();
  const profile = profiles[slug];
  const [form, setForm] = useState({ name: '', email: '', password: '', inviteCode: '' });
  const [showInvite, setShowInvite] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fields, setFields] = useState<Record<string, string[]>>({});
  const [busy, setBusy] = useState(false);
  const set = (k: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [k]: e.target.value });
  const fieldError = (k: string) => fields[k]?.[0] ?? fields[k.charAt(0).toUpperCase() + k.slice(1)]?.[0];

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    setFields({});
    try {
      await register({
        name: form.name, email: form.email, password: form.password, accountType,
        inviteCode: form.inviteCode.trim() || undefined,
      });
      navigate('/evento', { replace: true });
    } catch (err) {
      setError((err as Error).message);
      if (err instanceof ApiError) setFields(err.fieldErrors);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={submit}>
        <Link to="/" className="logo-lg">CONEXA</Link>
        <h1>{profile.title}</h1>
        <p className="muted">{profile.pitch}</p>
        {error && <Alert>{error}</Alert>}
        <Input label={accountType === 'Exhibitor' ? 'Seu nome (responsável)' : 'Nome'} required value={form.name} onChange={set('name')} error={fieldError('name')} />
        <Input label="E-mail" type="email" autoComplete="email" required value={form.email} onChange={set('email')} error={fieldError('email')} />
        <Input label="Senha" type="password" autoComplete="new-password" required minLength={8} value={form.password} onChange={set('password')}
          hint="Mínimo de 8 caracteres, com letras maiúsculas, minúsculas e números." error={fieldError('password')} />
        {showInvite
          ? <Input label="Código de convite" value={form.inviteCode} onChange={set('inviteCode')} />
          : <button type="button" className="invite-toggle" onClick={() => setShowInvite(true)}>Tenho um código de convite</button>}
        <Button type="submit" loading={busy}>Criar conta</Button>
        <p className="muted center"><Link to="/cadastro">Trocar perfil</Link> · <Link to="/login">Já tenho conta</Link></p>
      </form>
    </div>
  );
}
