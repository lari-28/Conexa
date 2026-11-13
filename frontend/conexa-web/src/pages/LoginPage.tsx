import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { Alert, Button, Input } from '../components/ui';

// Atalhos de demonstração: só existem no build de desenvolvimento (removidos no build de produção).
const demoAccounts = import.meta.env.DEV
  ? [
      { label: 'Ana (participante)', email: 'ana@conexa.demo' },
      { label: 'Bruno (participante)', email: 'bruno@conexa.demo' },
      { label: 'Carla (expositora)', email: 'carla@conexa.demo' },
      { label: 'Organizadora', email: 'organizador@conexa.demo' },
      { label: 'Admin', email: 'admin@conexa.demo' },
    ]
  : [];

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/evento';
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(email, password);
      navigate(from, { replace: true });
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={submit}>
        <h1 className="logo-lg">CONEXA</h1>
        <p className="muted">Entre para se conectar com pessoas e empresas do evento.</p>
        {error && <Alert>{error}</Alert>}
        <Input label="E-mail" type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <Input label="Senha" type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        <Button type="submit" loading={busy}>Entrar</Button>
        <p className="muted center">Ainda não tem conta? <Link to="/cadastro">Cadastre-se</Link></p>
        {demoAccounts.length > 0 && (
          <div className="demo-box">
            <small className="muted">Demonstração (senha: Conexa@123)</small>
            <div className="chips">
              {demoAccounts.map((a) => (
                <button key={a.email} type="button" className="chip" onClick={() => { setEmail(a.email); setPassword('Conexa@123'); }}>{a.label}</button>
              ))}
            </div>
          </div>
        )}
      </form>
    </div>
  );
}
