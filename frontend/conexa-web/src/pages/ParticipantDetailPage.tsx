import { Link, useParams } from 'react-router-dom';
import { useCurrentEvent } from '../features/events/EventContext';
import { participantsApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Avatar, Badge, Card } from '../components/ui';
import { AsyncView } from '../components/common';

export function ParticipantDetailPage() {
  const { id = '' } = useParams();
  const ev = useCurrentEvent();
  const { data, loading, error } = useAsync(() => participantsApi.get(ev.id, id), [ev.id, id]);
  const p = data?.profile;

  return (
    <>
      <Link to="/participantes" className="back">← Voltar</Link>
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {p && (
          <Card>
            <div className="row">
              <Avatar name={p.displayName} url={p.photoUrl} size={72} />
              <div>
                <h1>{p.displayName}</h1>
                <div className="muted">{[p.jobTitle, p.companyName].filter(Boolean).join(' · ')}</div>
                {p.area && <div className="muted small">Área: {p.area}</div>}
              </div>
            </div>
            {p.bio && <p>{p.bio}</p>}
            {p.lookingFor && <p><strong>Busca no evento:</strong> {p.lookingFor}</p>}
            {p.offers && <p><strong>Pode oferecer:</strong> {p.offers}</p>}
            {p.interests.length > 0 && <div className="chips">{p.interests.map((i) => <Badge key={i}>{i}</Badge>)}</div>}
            {(data?.companies.length ?? 0) > 0 && (
              <p>Representa: {data?.companies.map((c) => <Link key={c.id} to={`/empresas/${c.id}`}>{c.name} </Link>)}</p>
            )}
            <hr />
            <h3>Contato</h3>
            {p.email || p.phone ? (
              <ul className="plain">
                {p.email && <li>✉️ <a href={`mailto:${p.email}`}>{p.email}</a></li>}
                {p.phone && <li>📞 {p.phone}</li>}
              </ul>
            ) : <p className="muted">Esta pessoa não disponibilizou contatos.</p>}
          </Card>
        )}
      </AsyncView>
    </>
  );
}
