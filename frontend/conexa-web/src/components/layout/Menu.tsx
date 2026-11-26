import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../features/auth/AuthContext';
import { useEvent } from '../../features/events/EventContext';

export interface MenuItem { to: string; label: string; hint: string }
export interface MenuGroup { key: string; label: string; title: string; description: string; items: MenuItem[] }

/** Categorias do menu, montadas conforme o papel da pessoa. Quem não tem acesso a uma categoria nem a vê. */
export function useMenuGroups(): MenuGroup[] {
  const { hasRole } = useAuth();
  const { roleInEvent } = useEvent();
  const organizer = hasRole('Organizer', 'PlatformAdmin');
  const admin = hasRole('PlatformAdmin');

  return useMemo(() => {
    const groups: MenuGroup[] = [
      {
        key: 'explorar', label: 'Explorar', title: 'Explorar o evento',
        description: 'Descubra estandes, pessoas e conversas do evento.',
        items: [
          { to: '/programacao', label: 'Programação', hint: 'Palestras, atividades, horários e locais' },
          { to: '/empresas', label: 'Estandes', hint: 'Expositores e o que cada um oferece' },
          { to: '/participantes', label: 'Pessoas', hint: 'Participantes do evento' },
          { to: '/forum', label: 'Comunidade', hint: 'Dúvidas, opiniões e conversas' },
        ],
      },
      {
        key: 'meu-espaco', label: 'Meu espaço', title: 'Meu espaço',
        description: 'O que é seu: perfil, autorizações e avisos.',
        items: [
          ...(roleInEvent === 'Participant' ? [{ to: '/evento/meu-perfil', label: 'Meu perfil no evento', hint: 'Objetivo, temas e autorizações de uso dos dados' }] : []),
          ...(roleInEvent === 'Exhibitor' ? [{ to: '/evento/meu-estande', label: 'Meu estande', hint: 'Oferta, cliente ideal, temas e QR de check-in' }] : []),
          { to: '/perfil', label: 'Perfil e privacidade', hint: 'Seus dados e quais contatos aparecem' },
          { to: '/avisos', label: 'Avisos', hint: 'Recomendações e leads' },
        ],
      },
    ];
    if (organizer) {
      groups.push(
        {
          key: 'evento', label: 'Evento', title: 'Gestão do evento',
          description: 'Configure o evento e cuide de quem participa.',
          items: [
            { to: '/organizador/evento', label: 'Dados do evento', hint: 'Nome, datas, local e situação' },
            { to: '/organizador/programacao', label: 'Programação', hint: 'Cadastre palestras e atividades do evento' },
            { to: '/organizador/participantes', label: 'Participantes', hint: 'Quem está no evento e com qual papel' },
            { to: '/organizador/convites', label: 'Convites', hint: 'Links de cadastro para visitantes e expositores' },
            { to: '/organizador/moderacao', label: 'Moderação', hint: 'Denúncias e conteúdo da comunidade' },
          ],
        },
        {
          key: 'catalogo', label: 'Catálogo', title: 'Catálogo e temas',
          description: 'Organize os expositores e os temas que conectam as pessoas.',
          items: [
            { to: '/organizador/taxonomia', label: 'Taxonomia', hint: 'Temas, sinônimos e fila de aprovação' },
            { to: '/organizador/importacao', label: 'Importar expositores', hint: 'Catálogo em planilha CSV' },
          ],
        },
        {
          key: 'analises', label: 'Análises', title: 'Análises do evento',
          description: 'Entenda o que o evento gerou.',
          items: [
            { to: '/organizador/painel', label: 'Painel do evento', hint: 'Lacunas, demanda × oferta, exposição e check-ins' },
          ],
        },
      );
    }
    if (admin) {
      groups.push({
        key: 'admin', label: 'Administração', title: 'Administração da plataforma',
        description: 'Eventos e contas de toda a plataforma.',
        items: [
          { to: '/admin/eventos', label: 'Eventos', hint: 'Criar eventos e designar organizadores' },
          { to: '/admin/usuarios', label: 'Usuários', hint: 'Buscar, suspender e reativar contas' },
        ],
      });
    }
    return groups;
  }, [organizer, admin, roleInEvent]);
}

const isActive = (g: MenuGroup, path: string) => g.items.some((i) => path === i.to || path.startsWith(i.to + '/'));

/** Menu do topo (telas largas): categorias que abrem um painel, como um mega menu. Abre por clique ou ao passar o mouse. */
export function MegaNav({ groups, children }: { groups: MenuGroup[]; children: React.ReactNode }) {
  const [open, setOpen] = useState<string | null>(null);
  const { pathname } = useLocation();
  const ref = useRef<HTMLDivElement>(null);
  const hover = useRef<number | undefined>(undefined);
  const current = groups.find((g) => g.key === open) ?? null;

  useEffect(() => { setOpen(null); }, [pathname]);
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') setOpen(null); };
    const onClick = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(null); };
    document.addEventListener('keydown', onKey);
    document.addEventListener('mousedown', onClick);
    return () => { document.removeEventListener('keydown', onKey); document.removeEventListener('mousedown', onClick); };
  }, [open]);

  const enter = (key: string) => {
    if (!window.matchMedia('(hover: hover)').matches) return; // em toque, só o clique abre
    window.clearTimeout(hover.current);
    hover.current = window.setTimeout(() => setOpen(key), 90);
  };
  const leave = () => { window.clearTimeout(hover.current); hover.current = window.setTimeout(() => setOpen(null), 180); };
  const stay = () => window.clearTimeout(hover.current);

  return (
    <div className="mega" ref={ref} onMouseLeave={leave} onMouseEnter={stay}>
      <nav className="mega-nav" aria-label="Principal">
        {children}
        {groups.map((g) => (
          <button key={g.key} type="button" className={`mega-btn ${open === g.key ? 'open' : ''} ${isActive(g, pathname) ? 'current' : ''}`}
            aria-expanded={open === g.key} aria-controls={`mega-${g.key}`}
            onMouseEnter={() => enter(g.key)} onClick={() => setOpen(open === g.key ? null : g.key)}>
            {g.label}
            <svg className="chev" viewBox="0 0 12 12" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden><path d="M2 4.5l4 4 4-4" /></svg>
          </button>
        ))}
      </nav>
      {current && (
        <div className="mega-panel" id={`mega-${current.key}`} role="region" aria-label={current.title} onMouseEnter={stay}>
          <div className="mega-intro">
            <h2>{current.title}</h2>
            <p>{current.description}</p>
          </div>
          <ul className="mega-links plain">
            {current.items.map((i) => (
              <li key={i.to}>
                <Link to={i.to} className={pathname === i.to ? 'on' : ''}>
                  <strong>{i.label}</strong>
                  <span>{i.hint}</span>
                </Link>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

/** Menu do celular: folha em tela cheia com as categorias em sanfona. */
export function MobileMenu({ groups, onClose }: { groups: MenuGroup[]; onClose: () => void }) {
  const { pathname } = useLocation();
  const opened = useRef(pathname);
  // Fecha ao navegar, mas não na montagem (senão a folha abriria e fecharia na hora).
  useEffect(() => { if (pathname !== opened.current) onClose(); }, [pathname]); // eslint-disable-line react-hooks/exhaustive-deps
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div className="sheet" role="dialog" aria-modal="true" aria-label="Menu">
      <div className="sheet-head">
        <strong>Menu</strong>
        <button type="button" className="btn btn-ghost" onClick={onClose} aria-label="Fechar menu">✕</button>
      </div>
      <div className="sheet-body">
        {groups.map((g) => (
          <details key={g.key} className="acc" open={isActive(g, pathname)}>
            <summary>{g.label}</summary>
            <p className="muted small">{g.description}</p>
            <ul className="plain">
              {g.items.map((i) => (
                <li key={i.to}><Link to={i.to} className={pathname === i.to ? 'on' : ''}><strong>{i.label}</strong><span>{i.hint}</span></Link></li>
              ))}
            </ul>
          </details>
        ))}
      </div>
    </div>
  );
}
