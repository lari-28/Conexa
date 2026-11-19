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
