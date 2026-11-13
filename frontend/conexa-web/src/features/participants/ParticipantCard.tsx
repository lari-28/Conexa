import { useNavigate } from 'react-router-dom';
import { Avatar, Badge, Card } from '../../components/ui';
import type { ParticipantSummary } from '../../types';
import { roleLabel } from '../../utils/format';

export function ParticipantCard({ p }: { p: ParticipantSummary }) {
  const navigate = useNavigate();
  return (
    <Card onClick={() => navigate(`/participantes/${p.userId}`)}>
      <div className="row">
        <Avatar name={p.displayName} url={p.photoUrl} size={48} />
        <div className="grow">
          <strong>{p.displayName}</strong>
          <div className="muted">{[p.jobTitle, p.companyName].filter(Boolean).join(' · ') || 'Sem cargo informado'}</div>
        </div>
        {p.roleInEvent !== 'Participant' && <Badge tone="brand">{roleLabel[p.roleInEvent]}</Badge>}
      </div>
      {p.area && <div className="muted small">Área: {p.area}</div>}
      {p.lookingFor && <div className="small"><strong>Busca:</strong> {p.lookingFor}</div>}
      {p.interests.length > 0 && <div className="chips">{p.interests.map((i) => <Badge key={i}>{i}</Badge>)}</div>}
    </Card>
  );
}
