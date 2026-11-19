import { Link, Navigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { Loading } from '../components/ui';
import { profileOrder, profiles } from '../content/profiles';

const steps = [
  { n: 1, title: 'Cadastre-se', text: 'Visitante, expositor ou organizador: cada um informa o que busca e o que oferece.' },
  { n: 2, title: 'Explore o evento', text: 'Veja as pessoas e os estandes do evento e encontre quem tem interesses em comum.' },
  { n: 3, title: 'Conecte-se', text: 'Receba recomendações por afinidade, com o motivo de cada uma (versão inicial). O relatório final do evento está em desenvolvimento.' },
];

export function HomePage() {
  const { user, loading } = useAuth();
  if (loading) return <Loading />;
  if (user) return <Navigate to="/evento" replace />;

  return (
    <div className="pub">
      <header className="pub-bar">
        <span className="logo">CONEXA</span>
        <Link className="btn btn-secondary" to="/login">Entrar</Link>
      </header>

      <main className="pub-main">
        <section className="hero">
          <div>
            <h1>Conecte quem visita, quem expõe e quem organiza.</h1>
            <p className="lead">
              O Conexa aproxima pessoas e empresas de uma feira por interesses em comum,
              para que o networking aconteça de propósito, não por acaso.
            </p>
            <div className="hero-actions">
              <Link className="btn btn-primary btn-lg" to="/cadastro">Começar agora</Link>
              <Link className="btn btn-lg" to="/login">Já tenho conta</Link>
            </div>
          </div>

          <div className="example" aria-label="Exemplo ilustrativo">
            <span className="ex-label">Exemplo ilustrativo · dados fictícios</span>
            <p style={{ margin: 0 }}><strong>Objetivo do visitante:</strong> “Quero automatizar o controle de produção da minha fábrica.”</p>
            <div className="example-item"><span>Estande B-06 · Sistemas de gestão</span><span className="badge badge-match">Afinidade alta</span></div>
            <div className="example-item"><span>Estande C-12 · Sensores industriais</span><span className="badge badge-match">Afinidade média</span></div>
            <small className="muted">Apenas para mostrar a ideia. As recomendações reais dependem do que cada pessoa informar.</small>
          </div>
        </section>

        <section className="pub-section">
          <h2>Escolha como você participa</h2>
          <div className="grid">
            {profileOrder.map((slug) => (
              <Link key={slug} to={`/cadastro/${slug}`} className="door">
                <h3>{profiles[slug].title}</h3>
                <p className="muted" style={{ margin: 0 }}>{profiles[slug].door}</p>
                <span className="door-cta">{profiles[slug].cta} →</span>
              </Link>
            ))}
          </div>
        </section>

        <section className="pub-section">
          <h2>Como funciona</h2>
          <div className="grid">
            {steps.map((s) => (
              <div key={s.n} className="card">
                <div className="row"><span className="step-n">{s.n}</span><h3>{s.title}</h3></div>
                <p className="muted" style={{ margin: 0 }}>{s.text}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="pub-cta">
          <h2>Pronto para participar do próximo evento?</h2>
          <Link className="btn btn-primary btn-lg" to="/cadastro">Criar minha conta</Link>
        </section>
      </main>

      <footer className="pub-foot">© Conexa · Protótipo em desenvolvimento</footer>
    </div>
  );
}
