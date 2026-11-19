import { Link, useParams } from 'react-router-dom';
import { useCurrentEvent } from '../features/events/EventContext';
import { engagementApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Alert, Loading } from '../components/ui';

/** Destino do QR do estande: registra a visita e mostra o estande. */
export function CheckInPage() {
  const { code = '' } = useParams();
  const ev = useCurrentEvent();
  const { data, loading, error } = useAsync(() => engagementApi.checkIn(ev.id, code), [ev.id, code]);
  return (
    <div className="stack" style={{ maxWidth: 480 }}>
      <h1>Check-in no estande</h1>
      {loading && !data && <Loading label="Registrando visita…" />}
      {error && <Alert>{error.message}</Alert>}
      {data && (
        <>
          <Alert tone="success">
            {data.alreadyRecorded ? 'Sua visita já estava registrada: ' : 'Visita registrada: '}<strong>{data.companyName}</strong>.
          </Alert>
          <div className="row wrap">
            <Link className="btn btn-primary" to={`/empresas/${data.companyId}`}>Ver o estande</Link>
            <Link className="btn" to="/evento">Voltar ao evento</Link>
          </div>
        </>
      )}
    </div>
  );
}
