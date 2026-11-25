import { useEffect, useState } from 'react';
import { eventsApi, organizerApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { Alert, Button, Card, Input, Loading } from './ui';

const mapRowKey = () => `${Date.now()}-${Math.random()}`;

export function EventMaps({ eventId, organizer = false }: { eventId: string; organizer?: boolean }) {
  const { data: maps, loading, error, reload } = useAsync(() => eventsApi.maps(eventId), [eventId]);
  const [rows, setRows] = useState([{ key: mapRowKey(), file: null as File | null, description: '' }]);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const [zoomedMap, setZoomedMap] = useState<{ description: string; imageDataUrl: string } | null>(null);

  useEffect(() => {
    if (!zoomedMap) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const onKeyDown = (event: KeyboardEvent) => { if (event.key === 'Escape') setZoomedMap(null); };
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [zoomedMap]);

  async function upload() {
    const pending = rows.filter((row) => row.file);
    if (!pending.length || pending.some((row) => !row.description.trim())) {
      setMessage({ tone: 'error', text: 'Selecione cada arquivo PNG e informe seu andar ou bloco.' });
      return;
    }
    setBusy(true); setMessage(null);
    let uploaded = 0;
    try {
      for (const row of pending) {
        await organizerApi.addMap(eventId, row.file!, row.description.trim());
        uploaded++;
        setRows((items) => items.filter((item) => item.key !== row.key));
      }
      setRows((items) => items.length ? items : [{ key: mapRowKey(), file: null, description: '' }]);
      setMessage({ tone: 'success', text: `${uploaded} mapa(s) enviado(s) e disponível(is) para os participantes.` });
      await reload();
    } catch (err) {
      if (uploaded > 0) await reload();
      setMessage({ tone: 'error', text: (err as Error).message });
    }
    finally { setBusy(false); }
  }

  async function remove(id: string) {
    if (!window.confirm('Remover este mapa do evento?')) return;
    setMessage(null);
    try { await organizerApi.deleteMap(eventId, id); await reload(); }
    catch (err) { setMessage({ tone: 'error', text: (err as Error).message }); }
  }

  if (!organizer && !loading && (!maps || maps.length === 0)) return null;

  return (
    <section className="event-maps stack" aria-labelledby={`event-maps-title-${eventId}`}>
      <div>
        <h2 id={`event-maps-title-${eventId}`}>{organizer ? 'Mapas do evento' : 'Localize os estandes'}</h2>
        <p className="muted">{organizer
          ? 'Envie um mapa PNG para cada andar, bloco ou área. Os participantes verão os mapas junto à lista de estandes.'
          : 'Consulte o mapa do andar ou bloco para encontrar os estandes durante o evento.'}</p>
      </div>
      {message && <Alert tone={message.tone}>{message.text}</Alert>}
      {loading ? <Loading label="Carregando mapas…" /> : error ? <Alert>{error.message}</Alert> : null}
      {!!maps?.length && <div className="event-map-list">
        {maps.map((map) => (
          <Card key={map.id} className="event-map-card">
            <div className="event-map-heading"><h3>{map.description}</h3>
              {organizer && <Button type="button" variant="danger" onClick={() => remove(map.id)}>Remover</Button>}
            </div>
            <button type="button" className="event-map-zoom-trigger" onClick={() => setZoomedMap(map)} aria-label={`Ampliar mapa: ${map.description}`}>
              <img src={map.imageDataUrl} alt={`Mapa do evento: ${map.description}`} loading="lazy" />
              <span>Toque ou clique para ampliar</span>
            </button>
          </Card>
        ))}
      </div>}
      {organizer && <Card className="stack">
        <h3>Adicionar mapa</h3>
        {rows.map((row, index) => <div className="event-map-upload-row" key={row.key}>
          <Input label={`Mapa ${index + 1} · arquivo PNG`} type="file" accept="image/png,.png"
            onChange={(e) => setRows((items) => items.map((item) => item.key === row.key ? { ...item, file: e.target.files?.[0] ?? null } : item))} />
          <Input label="Andar, bloco ou área" placeholder="Ex.: Primeiro andar, Bloco B" maxLength={120} required={!!row.file}
            value={row.description} onChange={(e) => setRows((items) => items.map((item) => item.key === row.key ? { ...item, description: e.target.value } : item))} />
          {rows.length > 1 && <Button type="button" variant="ghost" onClick={() => setRows((items) => items.filter((item) => item.key !== row.key))}>Remover linha</Button>}
        </div>)}
        <div className="row">
          <Button type="button" variant="secondary" onClick={() => setRows((items) => [...items, { key: mapRowKey(), file: null, description: '' }])}>Adicionar outro andar ou bloco</Button>
          <Button type="button" loading={busy} onClick={upload}>Enviar mapas</Button>
        </div>
        <small className="muted">Somente arquivos PNG, até 14 MB cada.</small>
      </Card>}
      {zoomedMap && <div className="event-map-lightbox" role="dialog" aria-modal="true"
        aria-label={`Mapa ampliado: ${zoomedMap.description}`} onClick={() => setZoomedMap(null)}>
        <div className="event-map-lightbox-content" onClick={(event) => event.stopPropagation()}>
          <div className="event-map-lightbox-heading">
            <h2>{zoomedMap.description}</h2>
            <button type="button" className="event-map-lightbox-close" onClick={() => setZoomedMap(null)} aria-label="Fechar mapa ampliado">✕</button>
          </div>
          <img src={zoomedMap.imageDataUrl} alt={`Mapa ampliado do evento: ${zoomedMap.description}`} />
          <p>Use o gesto de pinça para ampliar ou toque fora do mapa para fechar.</p>
        </div>
      </div>}
    </section>
  );
}
