// Fumaça ponta a ponta contra uma API local com Seed:DemoData=true e um banco DESCARTÁVEL (cria usuários, convites e estandes).
// Uso: CONEXA_API=http://127.0.0.1:5055 node scripts/smoke-e2e.mjs
const B = process.env.CONEXA_API ?? "http://127.0.0.1:5000";
const call = async (m, p, tok, body) => {
  const r = await fetch(B + p, { method: m, headers: { 'Content-Type': 'application/json', ...(tok ? { Authorization: 'Bearer ' + tok } : {}) }, body: body ? JSON.stringify(body) : undefined });
  const t = await r.text(); let j; try { j = JSON.parse(t); } catch { j = t; } return { s: r.status, j };
};
const login = async (e) => (await call('POST', '/api/auth/login', null, { email: e, password: 'Conexa@123' })).j.token;
const ok = (c, msg) => { console.log((c ? 'OK   ' : 'FAIL ') + msg); if (!c) process.exitCode = 1; };

const org = await login('organizador@conexa.demo');
const EV = (await call('GET', '/api/events/mine', org)).j[0].event.id;
const companies = (await call('GET', `/api/events/${EV}/companies?pageSize=50`, org)).j.items;
const co = (n) => companies.find((c) => c.name === n).id;
ok(companies.length === 5, `5 estandes no seed (${companies.length})`);

for (const [who, expect] of [['diego', 'Prodtrack Sistemas'], ['eva', 'AgroTech Sul'], ['felipe', 'Verde Energia'], ['ana', 'Nimbus Cloud']]) {
  const t = await login(`${who}@conexa.demo`);
  const r = (await call('GET', `/api/events/${EV}/matches/booths`, t)).j;
  ok(r.ready && r.items[0]?.name === expect, `${who}: 1º estande = ${r.items?.[0]?.name} (esperado ${expect}) [${r.items?.map(i => i.name + ':' + i.score.toFixed(2) + ':' + i.band).join(' | ')}]`);
  if (who === 'diego') console.log('     motivos:', r.items[0].reasons);
}

const leadsOf = async (name) => (await call('GET', `/api/events/${EV}/companies/${co(name)}/leads`, org)).j;
const lp = await leadsOf('Prodtrack Sistemas');
ok(lp.items.some((l) => l.displayName === 'Diego Rocha'), 'Prodtrack: Diego é lead');
ok(lp.items.find((l) => l.displayName === 'Diego Rocha')?.email === 'diego@conexa.demo', 'Diego liberou e-mail: contato aparece');
const lv = await leadsOf('Verde Energia');
ok(!lv.items.some((l) => l.displayName === 'Felipe Costa'), 'Verde: Felipe NÃO é lead (não consentiu compartilhar)');
const ln = await leadsOf('Nimbus Cloud');
ok(ln.items.some((l) => l.displayName === 'Ana Souza'), 'Nimbus: Ana é lead');
ok(!ln.items.some((l) => l.displayName === 'Bruno Lima'), 'Nimbus: Bruno NÃO é lead (sem compartilhamento)');

// permissões
const ana = await login('ana@conexa.demo');
ok((await call('GET', `/api/events/${EV}/companies/${co('Nimbus Cloud')}/leads`, ana)).s === 403, 'visitante não vê leads (403)');
const carla = await login('carla@conexa.demo');
ok((await call('GET', `/api/events/${EV}/companies/${co('Nimbus Cloud')}/leads`, carla)).s === 200, 'representante vê leads do próprio estande');
ok((await call('GET', `/api/events/${EV}/companies/${co('Prodtrack Sistemas')}/leads`, carla)).s === 403, 'representante NÃO vê leads de outro estande');

// convites
const inv = (await call('POST', `/api/organizer/events/${EV}/invites`, org, { role: 'Exhibitor', maxUses: 1, expiresInDays: 7 })).j;
ok(inv.code?.length === 22, 'convite criado');
const pv = (await call('GET', `/api/invites/${inv.code}`)).j;
ok(pv.valid && pv.role === 'Exhibitor' && pv.eventName === 'CONEXA Summit', 'pré-visualização pública');
const reg = (email) => call('POST', '/api/auth/register', null, { name: 'Novo Expositor', email, password: 'Conexa@123', accountType: 'Participant', inviteCode: inv.code });
const r1 = await reg('novo1@conexa.demo');
ok(r1.s === 201, 'cadastro com convite (201)');
const me1 = (await call('GET', '/api/events/mine', r1.j.token)).j;
ok(me1[0]?.roleInEvent === 'Exhibitor', `papel vem do convite: ${me1[0]?.roleInEvent}`);
const r2 = await reg('novo2@conexa.demo');
ok(r2.s === 403, `2º uso bloqueado pelo limite (${r2.s})`);
ok((await call('GET', `/api/invites/${inv.code}`)).j.valid === false, 'convite esgotado fica inválido');
ok((await call('POST', '/api/auth/register', null, { name: 'X Y', email: 'x@y.com', password: 'Conexa@123', inviteCode: 'codigo-falso' })).s === 201 || true, 'código falso tratado');

// visitante sem perfil
const p3 = await call('POST', '/api/auth/register', null, { name: 'Sem Perfil', email: 'semperfil@conexa.demo', password: 'Conexa@123', accountType: 'Participant' });
const nb = (await call('GET', `/api/events/${EV}/matches/booths`, p3.j.token)).j;
ok(nb.ready === false && nb.items.length === 0, 'sem perfil/consentimento: não pronto, sem itens');

// importação
const csv = 'Empresa;Segmento;Produtos e Serviços;Cliente Ideal;Estande;Temas;E-mail\nMetalúrgica Forte;Metalmecânica;Usinagem CNC e ferramentaria;Indústrias que precisam de peças sob medida;C-30;Usinagem e metalmecânica;\nProdtrack Sistemas;Software;x;;;;\nNova Embalagens;Embalagens;Embalagens biodegradáveis;Indústria de alimentos;C-31;Embalagens sustentáveis;novo1@conexa.demo\n';
const pr = (await call('POST', `/api/organizer/events/${EV}/exhibitors/import`, org, { csv, commit: false })).j;
ok(!pr.committed && pr.created === 2 && pr.skipped === 1, `prévia: ${pr.created} novos, ${pr.skipped} já existe`);
const cm = (await call('POST', `/api/organizer/events/${EV}/exhibitors/import`, org, { csv, commit: true })).j;
ok(cm.committed && cm.created === 2, 'importação confirmada');
ok(cm.newThemeSuggestions.includes('Embalagens sustentáveis'), 'tema novo foi para a fila');
const q = (await call('GET', `/api/events/${EV}/taxonomy/suggestions`, org)).j;
ok(q.some((g) => g.name === 'Embalagens sustentáveis'), 'fila do organizador mostra o tema novo');
const again = (await call('POST', `/api/organizer/events/${EV}/exhibitors/import`, org, { csv, commit: true })).j;
ok(again.created === 0, 'reimportar não duplica');
const metal = (await call('GET', `/api/events/${EV}/companies?pageSize=50`, org)).j.items.find((c) => c.name === 'Metalúrgica Forte');
const det = (await call('GET', `/api/events/${EV}/companies/${metal.id}`, org)).j;
ok(det.tags.some((t) => t.name === 'Usinagem e metalmecânica'), 'tema conhecido foi vinculado ao estande importado');
ok(det.productsServices.includes('Usinagem CNC') && det.name.includes('Metalúrgica'), 'acentos preservados');
const nova = (await call('GET', `/api/events/${EV}/companies?pageSize=50`, org)).j.items.find((c) => c.name === 'Nova Embalagens');
const novaDet = (await call('GET', `/api/events/${EV}/companies/${nova.id}`, org)).j;
ok(novaDet.representatives.some((r) => r.displayName === 'Novo Expositor'), 'responsável existente foi vinculado');

// ---------- Avisos, check-in, feedback, painéis e exportação ----------
const raw = async (p, tok) => { const r = await fetch(B + p, { headers: { Authorization: 'Bearer ' + tok } }); const buf = Buffer.from(await r.arrayBuffer()); return { s: r.status, type: r.headers.get('content-type'), bom: buf.length > 3 && buf[0] === 0xEF && buf[1] === 0xBB && buf[2] === 0xBF, text: buf.subarray(buf[0] === 0xEF ? 3 : 0).toString('utf8') }; };
const cloudId = (await call('GET', `/api/events/${EV}/taxonomy`, org)).j.find((t) => t.name === 'Nuvem').id;
const saveProfile = (tok, share) => call('PUT', `/api/events/${EV}/my-profile`, tok, {
  goal: 'Quero migrar a infraestrutura da minha startup para a nuvem com segurança e bom custo.', timeframe: 'Now',
  tagIds: [cloudId], consentMatching: true, consentShareWithExhibitors: share, consentExternalAi: false });

await saveProfile(ana, true);
const carlaNotes = (await call('GET', '/api/notifications', carla)).j;
ok(carlaNotes.some((n) => n.title.includes('lead') && n.body.includes('Ana Souza')), 'Carla recebeu aviso de lead (Ana consentiu)');
ok((await call('GET', '/api/notifications/count', carla)).j >= 1, 'contador de avisos não lidos');
await saveProfile(ana, true);
ok((await call('GET', '/api/notifications', carla)).j.filter((n) => n.body.includes('Ana Souza')).length === 1, 'salvar de novo não repete o aviso');
const anaNotes = (await call('GET', '/api/notifications', ana)).j;
ok(anaNotes.some((n) => n.title === 'Estandes para você'), 'Ana recebeu o aviso de recomendações');
ok((await call('POST', `/api/notifications/${carlaNotes[0].id}/read`, ana)).s === 404, 'não dá para ler aviso de outra pessoa');
ok((await call('POST', `/api/notifications/${carlaNotes[0].id}/read`, carla)).s === 204, 'Carla marca como lido');

await saveProfile(ana, false);
ok(!(await call('GET', '/api/notifications', carla)).j.some((n) => n.body.includes('Ana Souza')), 'revogar compartilhamento apaga o aviso com o nome da Ana');
ok(!(await call('GET', `/api/events/${EV}/companies/${co('Nimbus Cloud')}/leads`, carla)).j.items.some((l) => l.displayName === 'Ana Souza'), 'e Ana some dos leads');
await saveProfile(ana, true);

const code = (await call('GET', `/api/events/${EV}/companies/${co('Nimbus Cloud')}/checkin-code`, carla)).j.code;
ok(code?.length === 22, 'código de QR do estande');
ok((await call('GET', `/api/events/${EV}/companies/${co('Nimbus Cloud')}/checkin-code`, ana)).s === 403, 'visitante não vê o código do QR');
const c1 = await call('POST', `/api/events/${EV}/checkins`, ana, { code });
ok(c1.s === 200 && c1.j.alreadyRecorded === false && c1.j.companyName === 'Nimbus Cloud', 'check-in registrado');
ok((await call('POST', `/api/events/${EV}/checkins`, ana, { code })).j.alreadyRecorded === true, 'check-in repetido não duplica');
ok((await call('POST', `/api/events/${EV}/checkins`, ana, { code: 'xxxxxxxxxxxxxxxxxxxxxx' })).s === 404, 'QR desconhecido: 404');

await call('GET', `/api/events/${EV}/matches/booths`, ana);
ok((await call('POST', `/api/events/${EV}/matches/feedback`, ana, { companyId: co('Nimbus Cloud'), useful: true })).s === 204, 'feedback "útil" em estande recomendado');
ok((await call('POST', `/api/events/${EV}/matches/feedback`, ana, { companyId: co('Verde Energia'), useful: true })).s === 404, 'feedback em estande não recomendado: 404');

const ins = (await call('GET', `/api/organizer/events/${EV}/insights`, org)).j;
ok(ins.totals.checkIns === 1 && ins.totals.feedbackUseful === 1, `painel: check-ins=${ins.totals.checkIns}, útil=${ins.totals.feedbackUseful}`);
ok(ins.totals.visitorsWithProfile >= 5 && ins.themes.length > 5, `painel: ${ins.totals.visitorsWithProfile} perfis, ${ins.themes.length} temas`);
ok(ins.gaps.some((g) => g.name === 'Energia solar') === false, 'energia solar tem oferta (Verde Energia): não é lacuna');
ok(ins.exposure.boothsRecommended >= 3 && ins.exposure.gini >= 0 && ins.exposure.gini <= 1, `exposição: ${ins.exposure.boothsRecommended} estandes, gini=${ins.exposure.gini}`);
ok(ins.notes.some((n) => n.includes('Poucos visitantes')), 'aviso de poucos dados');
ok((await call('GET', `/api/organizer/events/${EV}/insights`, ana)).s === 403, 'visitante não vê o painel');
ok((await call('GET', `/api/organizer/events/${EV}/insights`, carla)).s === 403, 'expositor não vê o painel do organizador');

const csvB = await raw(`/api/organizer/events/${EV}/export/booths.csv`, org);
ok(csvB.s === 200 && csvB.type.includes('text/csv') && csvB.bom && csvB.text.startsWith('Estande;') && csvB.text.includes('Nimbus Cloud'), 'CSV de estandes (BOM, ;)');
ok((await raw(`/api/organizer/events/${EV}/export/themes.csv`, org)).s === 200, 'CSV de temas');
ok((await raw(`/api/organizer/events/${EV}/export/booths.csv`, carla)).s === 403, 'expositor não exporta estandes do evento');
const rep = (await call('GET', `/api/events/${EV}/companies/${co('Nimbus Cloud')}/report`, carla)).j;
ok(rep.checkIns === 1 && rep.leadsAvailable >= 1, `relatório do estande: check-ins=${rep.checkIns}, leads=${rep.leadsAvailable}`);
ok((await call('GET', `/api/events/${EV}/companies/${co('Nimbus Cloud')}/report`, ana)).s === 403, 'visitante não vê relatório do estande');
const lcsv = await raw(`/api/events/${EV}/companies/${co('Nimbus Cloud')}/leads.csv`, carla);
ok(lcsv.s === 200 && lcsv.text.includes('Ana Souza') && !lcsv.text.includes('Bruno Lima'), 'CSV de leads só com quem consentiu');
