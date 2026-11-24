import { Link, useParams } from 'react-router-dom';
import { invitesApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Alert, Loading } from '../components/ui';

/** Página do link de convite: mostra o evento e leva ao cadastro com o código aplicado. */
export function InvitePage() {
  const { code = '' } = useParams();
  const { data, loading, error } = useAsync(() => invitesApi.preview(code), [code]);
  const slug = data?.role === 'Exhibitor' ? 'expositor' : 'visitante';
  return (
    <div className="auth-page">
      <div className="card auth-card">
        <Link to="/" className="logo-lg">CONEXA</Link>
        {loading && !data && <Loading />}
        {error && <Alert>{error.message}</Alert>}
        {data && !data.valid && <Alert>{data.reason ?? 'Convite inválido.'}</Alert>}
        {data?.valid && (
          <>
            <h1>Você foi convidado</h1>
            <p className="muted">
              {data.eventName}: participe como <strong>{data.role === 'Exhibitor' ? 'expositor' : 'visitante'}</strong>.
            </p>
            <Link className="btn btn-primary" to={`/cadastro/${slug}?convite=${encodeURIComponent(code)}`}>Criar minha conta</Link>
            <p className="muted center">Já tem conta? <Link to="/login">Entrar</Link></p>
          </>
        )}
      </div>
    </div>
  );
}
