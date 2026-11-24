import type { EventRole } from '../types';

/** Textos dos três perfis, centralizados. Só prometer o que o sistema já faz; o resto fica "em breve". */
export type ProfileSlug = 'visitante' | 'expositor' | 'feira';

export interface ProfileContent {
  slug: ProfileSlug;
  title: string;
  door: string;
  pitch: string;
  accountType: 'Participant' | 'Exhibitor' | null; // null = sem cadastro público
  cta: string;
  nextStep: string;
  nextPath: string;
  aiSoon: { title: string; text: string };
}

export const profiles: Record<ProfileSlug, ProfileContent> = {
  visitante: {
    slug: 'visitante',
    title: 'Sou visitante',
    door: 'Diga o que procura na feira e encontre quem pode ajudar.',
    pitch: 'Cadastre seus interesses e seu objetivo. Veja pessoas e estandes do evento.',
    accountType: 'Participant',
    cta: 'Entrar como visitante',
    nextStep: 'Conte o que você procura nesta feira e escolha seus temas de interesse.',
    nextPath: '/evento/meu-perfil',
    aiSoon: {
      title: 'Estandes para você',
      text: 'Em breve: recomendações de estandes com base no seu objetivo, com a explicação do porquê.',
    },
  },
  expositor: {
    slug: 'expositor',
    title: 'Sou expositor',
    door: 'Mostre o que sua empresa oferece e quem você quer atender.',
    pitch: 'Cadastre seu estande, sua oferta e o cliente ideal. Apareça para quem tem afinidade.',
    accountType: 'Exhibitor',
    cta: 'Cadastrar meu estande',
    nextStep: 'Cadastre seu estande: o que oferece, quem quer atender e até 3 temas.',
    nextPath: '/evento/meu-estande',
    aiSoon: {
      title: 'Leads para sua empresa',
      text: 'Em breve: visitantes com mais afinidade com o seu estande, com o motivo da sugestão.',
    },
  },
  feira: {
    slug: 'feira',
    title: 'Organizo uma feira',
    door: 'Reúna expositores e visitantes e acompanhe o que o evento gerou.',
    pitch: 'O acesso de organizador é aberto pela equipe Conexa, junto com a configuração do evento.',
    accountType: null,
    cta: 'Falar com a equipe',
    nextStep: 'Defina os temas do evento e aprove as sugestões de visitantes e expositores.',
    nextPath: '/organizador/taxonomia',
    aiSoon: {
      title: 'Resumo do evento',
      text: 'Em breve: painéis e relatório final com os interesses de visitantes e expositores.',
    },
  },
};

export const profileOrder: ProfileSlug[] = ['visitante', 'expositor', 'feira'];

export const profileByRole = (role: EventRole | null): ProfileContent =>
  role === 'Exhibitor' ? profiles.expositor : role === 'Organizer' ? profiles.feira : profiles.visitante;

export const contactEmail: string = (import.meta.env.VITE_CONTACT_EMAIL as string | undefined) ?? '';
