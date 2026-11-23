#!/usr/bin/env node
// Gera o catálogo fictício de teste (150 expositores) chamando a API do Google Gemini, em 5 lotes de 30.
// O prompt e os lotes vêm de docs/prompt-catalogo-teste.md (fonte única). Cada lote é validado
// (cabeçalho, 30 linhas, colunas, estandes na faixa, sites .example, nomes únicos) e refeito se falhar.
//
// Chave: variável GEMINI_API_KEY ou `dotnet user-secrets set "Llm:ApiKey" "<chave>" --project backend/Conexa.Api`.
// A chave nunca é impressa nem gravada em arquivo.
//
// Uso:
//   node scripts/gerar-catalogo.mjs --dry-run            mostra o prompt do lote 1, sem chamar a API
//   node scripts/gerar-catalogo.mjs                      roda os 5 lotes
//   node scripts/gerar-catalogo.mjs --lotes 2,3          roda só alguns lotes (os outros são lidos da pasta de saída)
//   opções: --model <nome> (ou GEMINI_MODEL)  --out <pasta>  --temperature 1.0  --tentativas 3
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const HEADER = 'nome;segmento;descricao;produtos;cliente_ideal;estande;site;temas;email';
const COLS = HEADER.split(';');
const ROWS_PER_LOT = 30;

// ---------- argumentos ----------
const args = process.argv.slice(2);
const flag = (name) => args.includes(`--${name}`);
const opt = (name, def) => { const i = args.indexOf(`--${name}`); return i >= 0 && args[i + 1] ? args[i + 1] : def; };
const config = {
  model: opt('model', process.env.GEMINI_MODEL ?? 'gemini-3.5-flash'),
  out: resolve(ROOT, opt('out', 'dados-teste/catalogo')),
  temperature: Number(opt('temperature', '1.0')),
  attempts: Number(opt('tentativas', '3')),
  lots: opt('lotes', '1,2,3,4,5').split(',').map((n) => Number(n.trim())),
  base: process.env.GEMINI_BASE_URL ?? 'https://generativelanguage.googleapis.com/v1beta',
};

// ---------- prompt e lotes (docs/prompt-catalogo-teste.md) ----------
export function loadPrompt(md) {
  const block = md.match(/````text\r?\n([\s\S]*?)\r?\n````/);
  if (!block) throw new Error('Prompt 1 não encontrado em docs/prompt-catalogo-teste.md');
  const lots = [...md.matchAll(/^\|\s*(\d)\s*\|\s*(.+?)\s*\|\s*(Pavilhão [A-Z]) • Estande 01 a 30\s*\|\s*$/gm)]
    .map((m) => ({ n: Number(m[1]), foco: m[2], pavilhao: m[3] }));
  if (lots.length !== 5) throw new Error(`Esperava 5 lotes na tabela, achei ${lots.length}`);
  return { template: block[1], lots };
}

export function buildPrompt(template, lot) {
  return template
    .replaceAll('{{N}}', String(lot.n))
    .replaceAll('{{FOCO}}', lot.foco)
    .replaceAll('{{FAIXA}}', `${lot.pavilhao} • Estande 01 a 30`);
}

// ---------- CSV ----------
export function parseCsv(text) {
  const rows = []; let row = []; let cell = ''; let quoted = false;
  const t = text.replace(/^﻿/, '');
  for (let i = 0; i < t.length; i++) {
    const c = t[i];
    if (quoted) {
      if (c === '"' && t[i + 1] === '"') { cell += '"'; i++; }
      else if (c === '"') quoted = false;
      else cell += c;
    } else if (c === '"') quoted = true;
    else if (c === ';') { row.push(cell); cell = ''; }
    else if (c === '\r') { /* ignora */ }
    else if (c === '\n') { row.push(cell); cell = ''; rows.push(row); row = []; }
    else cell += c;
  }
  if (cell !== '' || row.length) { row.push(cell); rows.push(row); }
  return rows.filter((r) => r.some((c) => c.trim() !== ''));
}

const key = (s) => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().replace(/\s+/g, ' ').trim();

/** Remove cerca de markdown e texto fora do CSV, caso o modelo desobedeça. */
export function cleanResponse(text) {
  let t = text.replace(/^﻿/, '').trim();
  t = t.replace(/^```[a-z]*\r?\n/i, '').replace(/\r?\n```\s*$/, '');
  const at = t.indexOf(HEADER);
  return at > 0 ? t.slice(at) : t;
}

/** Devolve a lista de problemas (vazia = lote válido). `usedNames` guarda os nomes dos outros lotes. */
export function validateLot(csv, lot, usedNames = new Set()) {
  const problems = [];
  const rows = parseCsv(csv);
  if (rows.length === 0) return ['Resposta vazia.'];
  if (rows[0].join(';') !== HEADER) problems.push(`Cabeçalho incorreto. Use exatamente: ${HEADER}`);
  const data = rows.slice(1);
  if (data.length !== ROWS_PER_LOT) problems.push(`Esperava ${ROWS_PER_LOT} linhas de dados e recebi ${data.length}.`);

  const names = new Set(); const booths = new Set();
  data.forEach((r, i) => {
    const at = `Linha ${i + 2}`;
    if (r.length !== COLS.length) { problems.push(`${at}: ${r.length} colunas em vez de ${COLS.length}.`); return; }
    const [nome, segmento, descricao, produtos, , estande, site, temas, email] = r;
    if (!nome.trim()) problems.push(`${at}: nome vazio.`);
    else if (names.has(key(nome)) || usedNames.has(key(nome))) problems.push(`${at}: nome repetido ("${nome}").`);
    else names.add(key(nome));
    if (!segmento.trim()) problems.push(`${at}: segmento vazio.`);
    if (!descricao.trim()) problems.push(`${at}: descrição vazia.`);
    if (!produtos.trim()) problems.push(`${at}: produtos vazio.`);
    if (temas.trim()) problems.push(`${at}: a coluna temas deve ficar vazia.`);
    if (email.trim()) problems.push(`${at}: a coluna email deve ficar vazia.`);
    if (!/^https?:\/\/[a-z0-9.-]+\.example(\/\S*)?$/i.test(site.trim())) problems.push(`${at}: site deve terminar em .example.`);
    const m = estande.trim().match(/^(Pavilhão [A-Z]) • Estande (\d{2})$/);
    if (!m) problems.push(`${at}: estande fora do formato "Pavilhão X • Estande NN".`);
    else {
      const n = Number(m[2]);
      if (m[1] !== lot.pavilhao || n < 1 || n > ROWS_PER_LOT) problems.push(`${at}: estande fora da faixa do lote (${lot.pavilhao}, 01 a 30).`);
      else if (booths.has(n)) problems.push(`${at}: estande ${m[2]} repetido.`);
      else booths.add(n);
    }
  });
  return problems;
}

// ---------- chave e API ----------
function readApiKey() {
  if (process.env.GEMINI_API_KEY) return process.env.GEMINI_API_KEY.trim();
  try {
    const out = execFileSync('dotnet', ['user-secrets', 'list', '--project', join(ROOT, 'backend/Conexa.Api')], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
    const line = out.split(/\r?\n/).find((l) => l.startsWith('Llm:ApiKey'));
    if (line) return line.slice(line.indexOf('=') + 1).trim();
  } catch { /* sem dotnet ou sem segredo */ }
  return null;
}

async function generate(apiKey, prompt, signal) {
  const url = `${config.base}/models/${encodeURIComponent(config.model)}:generateContent`;
  const body = {
    contents: [{ role: 'user', parts: [{ text: prompt }] }],
    generationConfig: { temperature: config.temperature, maxOutputTokens: 20000 },
  };
  for (let wait = 4000, tries = 0; ; tries++, wait *= 2) {
    const res = await fetch(url, { method: 'POST', signal, headers: { 'Content-Type': 'application/json', 'x-goog-api-key': apiKey }, body: JSON.stringify(body) });
    if ((res.status === 429 || res.status >= 500) && tries < 4) { await new Promise((r) => setTimeout(r, wait)); continue; }
    const json = await res.json().catch(() => ({}));
    if (!res.ok) {
      const err = new Error(`API ${res.status}: ${json?.error?.message ?? 'erro sem detalhes'}`);
      // 4xx (modelo inexistente, chave inválida, sem permissão) não melhora tentando de novo
      err.fatal = res.status >= 400 && res.status < 500 && res.status !== 429 && res.status !== 408;
      throw err;
    }
    const cand = json.candidates?.[0];
    const text = (cand?.content?.parts ?? []).map((p) => p.text ?? '').join('');
    if (!text) throw new Error(`Resposta sem texto (motivo: ${cand?.finishReason ?? json.promptFeedback?.blockReason ?? 'desconhecido'}).`);
    if (cand.finishReason === 'MAX_TOKENS') throw new Error('Resposta cortada por limite de tamanho.');
    return text;
  }
}

// ---------- execução ----------
async function main() {
  const md = readFileSync(join(ROOT, 'docs/prompt-catalogo-teste.md'), 'utf8');
  const { template, lots } = loadPrompt(md);
  const promptHash = createHash('sha256').update(template).digest('hex').slice(0, 12);

  if (flag('dry-run')) {
    console.log(buildPrompt(template, lots[0]));
    console.error(`\n[dry-run] modelo: ${config.model} · lotes: ${lots.map((l) => l.n).join(',')} · prompt ${promptHash}`);
    return;
  }

  const apiKey = readApiKey();
  if (!apiKey) {
    console.error('Chave não encontrada. Defina GEMINI_API_KEY ou rode:\n  dotnet user-secrets set "Llm:ApiKey" "<chave>" --project backend/Conexa.Api');
    process.exit(2);
  }
  mkdirSync(config.out, { recursive: true });
  const file = (n) => join(config.out, `lote-${n}.csv`);
  const usedNames = new Set();
  const meta = { gerado_em: new Date().toISOString(), modelo: config.model, temperatura: config.temperature, prompt_sha256: promptHash, lotes: {} };

  for (const lot of lots) {
    if (!config.lots.includes(lot.n)) {
      if (existsSync(file(lot.n))) parseCsv(readFileSync(file(lot.n), 'utf8')).slice(1).forEach((r) => usedNames.add(key(r[0])));
      continue;
    }
    let feedback = '';
    let ok = false;
    for (let attempt = 1; attempt <= config.attempts && !ok; attempt++) {
      process.stdout.write(`Lote ${lot.n} (${lot.foco.slice(0, 40)}…) tentativa ${attempt}/${config.attempts}… `);
      try {
        // modelos com raciocínio podem levar minutos num lote de 30 linhas (a latência observada varia de 4 s a 30 s num "oi")
        const ctl = AbortSignal.timeout(300_000);
        const raw = await generate(apiKey, buildPrompt(template, lot) + feedback, ctl);
        const csv = cleanResponse(raw);
        const problems = validateLot(csv, lot, usedNames);
        if (problems.length === 0) {
          writeFileSync(file(lot.n), csv.trim() + '\n', 'utf8');
          parseCsv(csv).slice(1).forEach((r) => usedNames.add(key(r[0])));
          meta.lotes[lot.n] = { tentativas: attempt };
          console.log('ok');
          ok = true;
        } else {
          console.log(`${problems.length} problema(s)`);
          problems.slice(0, 6).forEach((p) => console.log(`   - ${p}`));
          feedback = `\n\nATENÇÃO: a tentativa anterior foi rejeitada pelos motivos abaixo. Gere o lote COMPLETO de novo, corrigindo tudo:\n${problems.slice(0, 15).map((p) => `- ${p}`).join('\n')}`;
        }
      } catch (e) {
        console.log(`falhou: ${e.message}`);
        if (e.fatal) {
          console.error(`\nErro que repetir não resolve. Se for o modelo, tente: node scripts/gerar-catalogo.mjs --model <nome> (modelo atual: ${config.model}).`);
          process.exit(1);
        }
      }
    }
    if (!ok) { console.error(`Lote ${lot.n} não passou na validação após ${config.attempts} tentativas. Nada foi mesclado.`); process.exit(1); }
  }

  // mescla (um só cabeçalho, BOM para o Excel) e confere o conjunto
  const all = [];
  for (const lot of lots) {
    if (!existsSync(file(lot.n))) { console.error(`Falta o lote ${lot.n} em ${config.out}.`); process.exit(1); }
    all.push(...parseCsv(readFileSync(file(lot.n), 'utf8')).slice(1));
  }
  const names = new Set(all.map((r) => key(r[0])));
  if (all.length !== ROWS_PER_LOT * lots.length || names.size !== all.length) {
    console.error(`Conjunto inválido: ${all.length} linhas, ${names.size} nomes únicos.`); process.exit(1);
  }
  const q = (s) => `"${s.replaceAll('"', '""')}"`;
  const merged = '﻿' + HEADER + '\r\n' + all.map((r) => r.map(q).join(';')).join('\r\n') + '\r\n';
  writeFileSync(join(config.out, 'catalogo_150.csv'), merged, 'utf8');
  writeFileSync(join(config.out, 'metadados.json'), JSON.stringify(meta, null, 2) + '\n', 'utf8');
  console.log(`\nPronto: ${all.length} expositores em ${join(config.out, 'catalogo_150.csv')}`);
  console.log('Próximo passo: Organizador → Importar expositores → colar → Validar. Não edite o arquivo depois de ver os resultados.');
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch((e) => { console.error(e.message); process.exit(1); });
}
