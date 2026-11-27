import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Pagination } from '../components/common';
import { Alert, Avatar, Badge, Button, Card, EmptyState, Input, Loading, Modal, Select, Textarea } from '../components/ui';
import { ParticipantCard } from '../features/participants/ParticipantCard';
import type { ParticipantSummary } from '../types';

const participant: ParticipantSummary = {
  userId: 'demo-marina', displayName: 'Marina Costa', photoUrl: null,
  jobTitle: 'Diretora de inovação', companyName: 'Nimbus Cloud', area: 'Tecnologia',
  interests: ['IA aplicada', 'Parcerias', 'Cloud'], lookingFor: 'Conectar com empresas que estão modernizando seus dados.',
  roleInEvent: 'Exhibitor',
};

const sections = [
  ['shell', 'Navegação'], ['actions', 'Ações'], ['fields', 'Campos'], ['identity', 'Identidade'],
  ['surfaces', 'Superfícies'], ['feedback', 'Estados'], ['patterns', 'Padrões do produto'],
];

export function DesignSystemPage() {
  const [modalOpen, setModalOpen] = useState(false);
  const [page, setPage] = useState(2);

  return (
    <div className="kit-page">
      <header className="kit-topbar">
        <Link className="logo" to="/">CONEXA</Link>
        <span className="kit-topbar-label">Biblioteca de interface</span>
        <span className="kit-version"><i /> EVENTHUB · 01.00</span>
      </header>
      <div className="kit-layout">
        <aside className="kit-rail" aria-label="Seções da biblioteca">
          <span className="kit-rail-caption">CATÁLOGO</span>
          {sections.map(([id, label], index) => (
            <a key={id} href={`#${id}`} className={index === 0 ? 'is-current' : ''}>
              <span className="kit-rail-index">{String(index + 1).padStart(2, '0')}</span>{label}
            </a>
          ))}
          <div className="kit-rail-note"><span className="kit-live-dot" />Sistema ativo<br /><small>Tokens compartilhados</small></div>
        </aside>

        <main className="kit-canvas">
          <div className="kit-heading">
            <div><div className="kit-eyebrow"><span>CONEXA / EVENTHUB</span><span>•</span><span>GESTÃO DE EVENTOS</span></div>
              <h1>Eventos em foco.</h1>
              <p>Uma interface clara para organizar a feira, acompanhar a atividade e aproximar pessoas.</p>
            </div>
            <div className="kit-heading-index"><strong>01</strong><span>VISÃO GERAL</span></div>
          </div>

          <section id="shell" className="kit-section">
            <div className="kit-section-head"><div><span className="kit-kicker">01 / ESTRUTURA</span><h2>Navegação</h2></div><p>Orientação constante para explorar o evento.</p></div>
            <div className="kit-demo kit-nav-demo">
              <aside className="kit-nav-preview"><div className="kit-preview-brand"><span>C</span> CONEXA<small>EVENT HUB</small></div><small className="kit-kicker">NAVEGAÇÃO</small><a href="#shell" className="active">▦ <span>Painel</span></a><a href="#patterns">▤ <span>Eventos</span></a><a href="#patterns">♧ <span>Expositores</span></a><a href="#patterns">◉ <span>Networking</span></a></aside>
              <div className="kit-dashboard-preview"><div className="kit-preview-top"><span>CONEXA Summit · São Paulo</span><Avatar name="Marina Costa" size={32} /></div><div><span className="kit-kicker">QUARTA-FEIRA, 22 DE ABRIL</span><h3>Bem-vinda de volta, Marina</h3><p className="muted small">Acompanhe a atividade do seu evento.</p></div><div className="kit-stat-row"><div><span>Eventos</span><strong>142</strong><i className="kit-cyan-mark" /></div><div><span>Participantes</span><strong>12.840</strong><i className="kit-purple-mark" /></div><div><span>Check-ins</span><strong>8.291</strong><i className="kit-green-mark" /></div></div></div>
            </div>
            <div className="kit-mobile-nav" aria-label="Navegação compacta"><span>⌂<small>Evento</small></span><span>♧<small>Pessoas</small></span><span>▦<small>Empresas</small></span><span>☰<small>Menu</small></span></div>
          </section>

          <section id="actions" className="kit-section">
            <div className="kit-section-head"><div><span className="kit-kicker">02 / INTERAÇÃO</span><h2>Ações</h2></div><p>Hierarquia clara, resposta rápida e foco visível.</p></div>
            <div className="kit-demo kit-button-row"><Button>Confirmar presença</Button><Button variant="secondary">Ver programação</Button><Button variant="ghost">Ação discreta</Button><Button variant="danger">Remover</Button><Button loading>Salvando</Button><Button disabled>Indisponível</Button></div>
          </section>

          <section id="fields" className="kit-section">
            <div className="kit-section-head"><div><span className="kit-kicker">03 / ENTRADA</span><h2>Campos</h2></div><p>Rótulos explícitos e estados legíveis em cada superfície.</p></div>
            <div className="kit-demo kit-field-grid"><Input label="Buscar pessoas" placeholder="Nome, empresa ou área" /><Select label="Área de interesse" placeholder="Todas as áreas" options={[{ value: 'tech', label: 'Tecnologia' }, { value: 'biz', label: 'Negócios' }]} /><Textarea label="Apresentação" placeholder="O que você gostaria de compartilhar?" hint="Conte em poucas palavras o que busca no evento." /></div>
          </section>

          <section id="identity" className="kit-section">
            <div className="kit-section-head"><div><span className="kit-kicker">04 / IDENTIDADE</span><h2>Pessoas e sinais</h2></div><p>Informação de perfil e status em leitura rápida.</p></div>
            <div className="kit-demo kit-identity-row"><div className="kit-avatar-samples"><Avatar name="Marina Costa" size={52} /><Avatar name="Lucas Mendes" size={40} /><Avatar name="Ana Ribeiro" size={32} /></div><div className="kit-divider" /><div className="kit-badges"><Badge>Participante</Badge><Badge tone="brand">Expositor</Badge><Badge tone="success">Confirmado</Badge><Badge tone="warning">Aguardando</Badge><Badge tone="danger">Encerrado</Badge></div><div className="kit-divider" /><div className="kit-color-key"><span><i className="kit-cyan-mark"/>Ciano</span><span><i className="kit-purple-mark"/>Violeta</span><span><i className="kit-green-mark"/>Verde</span></div></div>
          </section>

          <section id="surfaces" className="kit-section">
            <div className="kit-section-head"><div><span className="kit-kicker">05 / CONTEÚDO</span><h2>Superfícies</h2></div><p>Painéis sólidos e camadas translúcidas organizam o conteúdo.</p></div>
            <div className="kit-surface-grid"><Card><span className="kit-kicker">SESSÃO EM DESTAQUE</span><h3>IA aplicada a negócios</h3><p className="muted">Hoje, 14h30 · Auditório principal</p><Badge tone="brand">Em breve</Badge></Card><Card className="kit-inset-card"><span className="kit-kicker">ÁREA DE CONEXÃO</span><h3>Encontre sua próxima conversa</h3><p className="muted">Descubra quem está construindo soluções para desafios parecidos.</p><Button variant="secondary">Explorar pessoas</Button></Card></div>
          </section>

          <section id="feedback" className="kit-section">
            <div className="kit-section-head"><div><span className="kit-kicker">06 / RESPOSTA</span><h2>Estados</h2></div><p>Feedback útil para avançar, corrigir ou tentar novamente.</p></div>
            <div className="kit-state-grid"><Alert tone="success">Presença confirmada. Seu QR Code está pronto.</Alert><Alert tone="info">A programação foi atualizada pela organização.</Alert><Alert>Não foi possível carregar os dados. Tente novamente.</Alert><div className="kit-state-pair"><div className="kit-demo"><Loading label="Carregando participantes…" /></div><div className="kit-demo"><EmptyState title="Nenhuma conversa por aqui" description="Quando houver novas publicações, elas aparecem nesta lista." /></div></div></div>
          </section>

          <section id="patterns" className="kit-section">
            <div className="kit-section-head"><div><span className="kit-kicker">07 / PRODUTO</span><h2>Padrões do CONEXA</h2></div><p>Componentes reais compostos com dados de demonstração.</p></div>
            <div className="kit-pattern-grid"><div><span className="kit-kicker">CARTÃO DE PARTICIPANTE</span><ParticipantCard p={participant} /></div><div><span className="kit-kicker">PAGINAÇÃO</span><div className="kit-demo"><Pagination page={page} totalPages={5} onChange={setPage} /></div><span className="kit-kicker kit-modal-label">DIÁLOGO</span><div className="kit-demo kit-modal-trigger"><p className="muted">Confirme antes de concluir uma ação importante.</p><Button variant="secondary" onClick={() => setModalOpen(true)}>Visualizar diálogo</Button></div></div></div>
            <div className="kit-post-preview"><div className="row"><Avatar name="Rafael Nunes" size={42} /><div className="grow"><strong>Rafael Nunes</strong><div className="muted small">há 12 min · CONEXA Summit</div></div><Badge tone="brand">Networking</Badge></div><p className="post-text">Alguém mais está explorando aplicações de IA para otimizar operações? Quero trocar experiências sobre projetos que já chegaram à produção.</p><div className="row wrap"><button className="chip chip-on">✦ Útil · 8</button><button className="chip">♡ Curtir</button><span className="grow"/><Button variant="ghost">Comentários · 3</Button></div></div>
          </section>

          <footer className="kit-footer"><span>CONEXA <b>／</b> EVENTHUB OPERATIONS</span><span>DESIGN SYSTEM · 2026</span></footer>
        </main>
      </div>
      {modalOpen && <Modal title="Confirmar presença" onClose={() => setModalOpen(false)}><p>Você confirma sua participação em IA aplicada a negócios?</p><div className="row wrap"><Button onClick={() => setModalOpen(false)}>Confirmar</Button><Button variant="secondary" onClick={() => setModalOpen(false)}>Voltar</Button></div></Modal>}
    </div>
  );
}
