import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { ApiError } from '../services/api/client';
import { Alert, Button, Input, Select } from '../components/ui';

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ name: '', email: '', password: '', accountType: 'Participant', inviteCode: '' });
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
        name: form.name, email: form.email, password: form.password,
        accountType: form.accountType as 'Participant' | 'Exhibitor',
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
        <h1 className="logo-lg">CONEXA</h1>
        <p className="muted">Crie sua conta para participar do evento.</p>
        {error && <Alert>{error}</Alert>}
        <Input label="Nome" required value={form.name} onChange={set('name')} error={fieldError('name')} />
        <Input label="E-mail" type="email" required value={form.email} onChange={set('email')} error={fieldError('email')} />
        <Input label="Senha" type="password" required minLength={8} value={form.password} onChange={set('password')}
          hint="Mínimo de 8 caracteres, com letras maiúsculas, minúsculas e números." error={fieldError('password')} />
        <Select label="Tipo de perfil" value={form.accountType} onChange={set('accountType')}
          options={[{ value: 'Participant', label: 'Participante' }, { value: 'Exhibitor', label: 'Expositor / empresa' }]} />
        <Input label="Código de convite (se houver)" value={form.inviteCode} onChange={set('inviteCode')} />
        <Button type="submit" loading={busy}>Criar conta</Button>
        <p className="muted center">Já tem conta? <Link to="/login">Entrar</Link></p>
      </form>
    </div>
  );
}
