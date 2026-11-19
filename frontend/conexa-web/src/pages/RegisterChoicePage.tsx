import { Link } from 'react-router-dom';
import { profileOrder, profiles } from '../content/profiles';

export function RegisterChoicePage() {
  return (
    <div className="auth-page">
      <div className="stack" style={{ width: '100%', maxWidth: 560 }}>
        <Link to="/" className="logo-lg">CONEXA</Link>
        <h1 className="center">Como você vai participar?</h1>
        {profileOrder.map((slug) => (
          <Link key={slug} to={`/cadastro/${slug}`} className="door">
            <h3>{profiles[slug].title}</h3>
            <p className="muted" style={{ margin: 0 }}>{profiles[slug].pitch}</p>
            <span className="door-cta">{profiles[slug].cta} →</span>
          </Link>
        ))}
        <p className="muted center">Já tem conta? <Link to="/login">Entrar</Link></p>
      </div>
    </div>
  );
}
