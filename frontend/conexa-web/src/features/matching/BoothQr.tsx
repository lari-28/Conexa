import { useEffect, useState } from 'react';
import QRCode from 'qrcode';
import { engagementApi } from '../../services/api/endpoints';
import { Alert, Button, Card } from '../../components/ui';

/** QR de check-in do estande, para imprimir e colocar no estande. */
export function BoothQr({ eventId, companyId, name }: { eventId: string; companyId: string; name: string }) {
  const [img, setImg] = useState<string | null>(null);
  const [url, setUrl] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    engagementApi.checkInCode(eventId, companyId)
      .then(async ({ code }) => {
        const target = `${window.location.origin}/checkin/${code}`;
        const data = await QRCode.toDataURL(target, { margin: 1, width: 320 });
        if (!cancelled) { setUrl(target); setImg(data); }
      })
      .catch((e: Error) => { if (!cancelled) setError(e.message); });
    return () => { cancelled = true; };
  }, [eventId, companyId]);

  return (
    <Card>
      <h2 style={{ marginTop: 0 }}>QR de check-in</h2>
      <p className="muted small" style={{ margin: 0 }}>
        Imprima e deixe no estande. Quem ler o QR registra a visita, e isso entra nos relatórios do evento.
      </p>
      {error && <Alert>{error}</Alert>}
      {img && (
        <div className="stack" style={{ alignItems: 'flex-start' }}>
          <img src={img} alt={`QR de check-in de ${name}`} width={200} height={200} />
          <small className="muted" style={{ wordBreak: 'break-all' }}>{url}</small>
          <Button variant="secondary" onClick={() => window.print()}>Imprimir</Button>
        </div>
      )}
    </Card>
  );
}
