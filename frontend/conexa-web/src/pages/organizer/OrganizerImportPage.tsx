import { useState } from 'react';
import { useCurrentEvent } from '../../features/events/EventContext';
import { importApi } from '../../services/api/endpoints';
import { Alert, Badge, Button, Card, Textarea } from '../../components/ui';
import { PageHeader } from '../../components/common';
import type { ImportReport } from '../../types';

const tone = { Created: 'success', Skipped: 'warning', Invalid: 'danger' } as const;
const label = { Created: 'Importar', Skipped: 'Já existe', Invalid: 'Inválida' } as const;

/** Importa o catálogo de expositores em CSV: primeiro valida, depois confirma. */
export function OrganizerImportPage() {
  const ev = useCurrentEvent();
  const [csv, setCsv] = useState('');
  const [report, setReport] = useState<ImportReport | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function readFile(file: File | undefined) {
    if (!file) return;
    setCsv(await file.text()); setReport(null); setError(null);
  }

  async function run(commit: boolean) {
    setBusy(true); setError(null);
    try { setReport(await importApi.exhibitors(ev.id, csv, commit)); }
    catch (e) { setError((e as Error).message); }
    finally { setBusy(false); }
  }

  const canCommit = report && !report.committed && report.fileErrors.length === 0 && report.created > 0;

  return (
    <>
      <PageHeader title="Importar expositores" subtitle="Envie o catálogo em CSV. Nada é gravado até você confirmar." />
      <Card>
        <p className="muted small" style={{ margin: 0 }}>
          Colunas aceitas: <strong>nome</strong> (obrigatória), segmento, descrição, produtos, cliente ideal, estande, site, temas
          (separados por <code>;</code>, até 3) e e-mail do responsável. No Excel, use “Salvar como CSV”. Até 1000 linhas.
        </p>
        <input type="file" accept=".csv,text/csv,text/plain" aria-label="Arquivo CSV" onChange={(e) => readFile(e.target.files?.[0])} />
        <Textarea label="Ou cole o conteúdo" rows={6} value={csv} onChange={(e) => { setCsv(e.target.value); setReport(null); }} />
        <div className="row wrap">
          <Button variant="secondary" loading={busy} disabled={!csv.trim()} onClick={() => run(false)}>Validar</Button>
          <Button loading={busy} disabled={!canCommit} onClick={() => run(true)}>Importar {report ? `(${report.created})` : ''}</Button>
        </div>
      </Card>
      {error && <Alert>{error}</Alert>}
      {report?.fileErrors.map((m) => <Alert key={m}>{m}</Alert>)}
      {report && report.fileErrors.length === 0 && (
        <>
          <Alert tone={report.committed ? 'success' : 'info'}>
            {report.committed ? 'Importação concluída. ' : 'Pré-visualização. '}
            {report.created} {report.committed ? 'criado(s)' : 'a criar'}, {report.skipped} já existente(s), {report.invalid} inválida(s).
            {report.newThemeSuggestions.length > 0 && ` ${report.newThemeSuggestions.length} tema(s) novo(s) vão para a fila de aprovação.`}
          </Alert>
          <div className="stack">
            {report.rows.map((r) => (
              <Card key={r.line}>
                <div className="row wrap"><Badge tone={tone[r.status]}>{label[r.status]}</Badge><strong>{r.name || '(sem nome)'}</strong>
                  <span className="muted small">linha {r.line}</span></div>
                {r.messages.map((m) => <small key={m} className="muted">{m}</small>)}
              </Card>
            ))}
          </div>
        </>
      )}
    </>
  );
}
