import { Link } from 'react-router-dom';
import { useCurrentEvent, useEvent } from '../features/events/EventContext';
import { Alert, Badge, Card, Loading } from '../components/ui';
import { PageHeader } from '../components/common';
import { profileByRole } from '../content/profiles';
import { BoothMatchesBlock } from '../features/matching/MatchBlocks';
import { useAuth } from '../features/auth/AuthContext';
import { profileApi, visitorProfileApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import { formatDateTime, roleLabel } from '../utils/format';
import { ExhibitorHome } from './ExhibitorHome';
import { OrganizerDashboardPage } from './organizer/OrganizerDashboardPage';

export function EventPage() {
  const ev = useCurrentEvent();
  const { roleInEvent } = useEvent();
  if (roleInEvent === 'Participant') return <ParticipantHome event={ev} />;
  if (roleInEvent === 'Exhibitor') return <ExhibitorHome event={ev} />;
  if (roleInEvent === 'Organizer') return <OrganizerDashboardPage />;

  const profile = profileByRole(roleInEvent);
  const shortcuts = [
    { to: '/empresas', title: 'Estandes', text: 'Conheça os expositores e o que eles oferecem.' },
    { to: '/participantes', title: 'Pessoas', text: 'Busque participantes por nome, empresa ou interesse.' },
    { to: '/forum', title: 'Comunidade', text: 'Tire dúvidas e compartilhe opiniões.' },
  ];
  return (
    <>
      <PageHeader title={ev.name} subtitle={ev.location ?? undefined}
        actions={roleInEvent ? <Badge tone="brand">{roleLabel[roleInEvent]}</Badge> : undefined} />
      <Card>
        {ev.description && <p>{ev.description}</p>}
        <p className="muted">{formatDateTime(ev.startsAt)} — {formatDateTime(ev.endsAt)}</p>
      </Card>

      <Card>
        <h3>Próximo passo</h3>
        <p className="muted" style={{ margin: 0 }}>{profile.nextStep}</p>
        <div><Link className="btn btn-primary" to={profile.nextPath}>Continuar</Link></div>
      </Card>

      <BoothMatchesBlock eventId={ev.id} />

      <div className="grid">
        {shortcuts.map((s) => (
          <Link key={s.to} to={s.to} className="card card-click link-card"><strong>{s.title}</strong><span className="muted">{s.text}</span></Link>
        ))}
      </div>
    </>
  );
}

function ParticipantHome({ event }: { event: ReturnType<typeof useCurrentEvent> }) {
  const { user } = useAuth();
  const { data: setup, loading, error } = useAsync(async () => {
    const [profile, visitorProfile] = await Promise.all([profileApi.me(), visitorProfileApi.get(event.id)]);
    return { profile, visitorProfile };
  }, [event.id]);
  const firstName = (setup?.profile.displayName ?? user?.displayName ?? '').trim().split(/\s+/)[0];
  const profile = setup?.profile;
  const visitorProfile = setup?.visitorProfile;
  const coreTasks = [
    {
      title: 'Conte quem você é', to: '/perfil',
      description: 'Cargo, empresa, área e apresentação ajudam as pessoas a conhecer você.',
      done: Boolean(profile?.companyName?.trim() && profile.jobTitle?.trim() && profile.area?.trim() && profile.bio?.trim()),
    },
    {
      title: 'Diga o que você busca no evento', to: '/evento/meu-perfil',
      description: 'Conte o que deseja encontrar e o prazo que tem em mente.',
      done: Boolean(visitorProfile?.goal?.trim()),
    },
    {
      title: 'Escolha seus interesses', to: '/evento/meu-perfil',
      description: 'Temas ajudam a orientar as sugestões de estandes.',
      done: Boolean(visitorProfile?.tags.length),
    },
  ];
  const completedTasks = coreTasks.filter((task) => task.done).length;
  const nextTask = coreTasks.find((task) => !task.done);
  const profileReady = visitorProfile?.readyForMatching === true;
  const nextTitle = nextTask?.title ?? (profileReady ? 'Suas recomendações estão prontas' : 'Seu perfil está organizado');
  const nextDescription = nextTask?.description ?? (profileReady
    ? 'As sugestões consideram seu objetivo e seus temas. Você pode atualizar essas informações quando quiser.'
    : 'Revise suas preferências de recomendação ou explore os estandes e pessoas do evento.');
  const nextPath = nextTask?.to ?? '/evento/meu-perfil';

  const shortcuts = [
    {
      to: '/participantes', title: 'Pessoas', text: 'Busque por nome, área ou interesse.',
      icon: 'M16 20v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2M10 10a3.5 3.5 0 100-7 3.5 3.5 0 000 7zM20 20v-2a3 3 0 00-2.5-3M15.5 3.2a3.5 3.5 0 010 6.6',
    },
    {
      to: '/empresas', title: 'Estandes', text: 'Conheça expositores e o que eles oferecem.',
      icon: 'M5 21V4a1 1 0 011-1h8a1 1 0 011 1v17M15 9h4a1 1 0 011 1v11M3 21h18M9 7h2M9 11h2M9 15h2',
    },
    {
      to: '/forum', title: 'Comunidade', text: 'Tire dúvidas e compartilhe opiniões.',
      icon: 'M21 11.5a8.4 8.4 0 01-.9 3.8 8.5 8.5 0 01-7.6 4.7 8.4 8.4 0 01-3.8-.9L3 21l1.9-5.7a8.4 8.4 0 01-.9-3.8A8.5 8.5 0 018.7 3.9a8.4 8.4 0 013.8-.9h.5a8.5 8.5 0 018 8v.5z',
    },
  ];
  const dateRange = `${new Date(event.startsAt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })} – ${new Date(event.endsAt).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' })}`;
  const privacySafe = Boolean(profile && ((!profile.privacy.emailVisible && !profile.privacy.phoneVisible) || profile.privacy.consentGiven));
  const totalProfileSteps = coreTasks.length + 1;
  const profileProgress = completedTasks + (privacySafe ? 1 : 0);

  return (
    <div className="participant-home">
      <header className="participant-welcome">
        <div className="participant-greeting-copy">
          <h1>Olá{firstName ? `, ${firstName}` : ''}</h1>
          <p>{event.name} · {dateRange}</p>
        </div>
        <span className="participant-role">Participante</span>
      </header>

      <section className={`participant-next ${profileReady ? 'is-ready' : ''}`} aria-labelledby="participant-next-title">
        <div className="participant-next-copy">
          <span className="participant-eyebrow">PRÓXIMO PASSO</span>
          <h2 id="participant-next-title">{nextTitle}</h2>
          <p>{nextDescription}</p>
        </div>
        {profileReady
          ? <a className="btn btn-primary participant-next-action" href="#recommendations">Ver recomendações</a>
          : <Link className="btn btn-primary participant-next-action" to={nextPath}>Continuar</Link>}
      </section>

      <section className="participant-profile-card" aria-labelledby="participant-profile-title">
        <div className="participant-profile-heading">
          <strong id="participant-profile-title">Seu perfil</strong>
          <span className="muted small">{profileProgress} de {totalProfileSteps}</span>
        </div>
        <div className="participant-progress" role="progressbar" aria-label="Etapas recomendadas do perfil"
          aria-valuemin={0} aria-valuemax={totalProfileSteps} aria-valuenow={profileProgress}>
          <span style={{ width: `${(profileProgress / totalProfileSteps) * 100}%` }} />
        </div>
        {loading && !setup ? <Loading label="Carregando seu progresso…" /> : error && !setup
          ? <Alert>Não foi possível carregar o progresso do perfil. Você ainda pode abrir e atualizar seu perfil.</Alert>
          : <div className="participant-task-list">
            {coreTasks.map((task) => (
              <Link key={task.title} to={task.to} className={`participant-task ${task.done ? 'is-done' : ''}`}>
                <span className="participant-task-mark" aria-hidden="true">
                  {task.done ? <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12l5 5 9-10" /></svg> : null}
                </span>
                <span className="participant-task-title">{task.title}</span>
                {!task.done && <span className="participant-task-arrow" aria-hidden="true">›</span>}
              </Link>
            ))}
            <Link to="/perfil#privacidade" className={`participant-task participant-task-optional ${privacySafe ? 'is-done' : ''}`}>
              <span className={`participant-task-mark ${privacySafe ? 'is-private' : ''}`} aria-hidden="true">
                {privacySafe ? <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12l5 5 9-10" /></svg> : null}
              </span>
              <span className="participant-task-title">Quem pode ver seus contatos <small>· opcional</small></span>
              <span className="participant-task-arrow" aria-hidden="true">›</span>
            </Link>
          </div>}
      </section>

      <section id="recommendations" className="participant-recommendations" aria-label="Recomendações de estandes">
        <BoothMatchesBlock eventId={event.id} />
      </section>

      <section className="participant-explore" aria-labelledby="participant-explore-title">
        <div className="participant-section-heading">
          <h2 id="participant-explore-title">Explorar o evento</h2>
        </div>
        <div className="participant-shortcuts">
          {shortcuts.map((shortcut) => (
            <Link key={shortcut.to} to={shortcut.to} className="participant-shortcut">
              <span className="participant-shortcut-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                  <path d={shortcut.icon} />
                </svg>
              </span>
              <span className="participant-shortcut-copy">
                <strong>{shortcut.title}</strong>
                <span>{shortcut.text}</span>
              </span>
              <span className="participant-shortcut-arrow" aria-hidden="true">→</span>
            </Link>
          ))}
        </div>
      </section>

      <details className="participant-about">
        <summary><strong>Sobre o evento</strong><span>Ver detalhes <span aria-hidden="true">⌄</span></span></summary>
        <div className="participant-about-content">
          {event.location && <p>{event.location}</p>}
          {event.description && <p>{event.description}</p>}
          <p>{formatDateTime(event.startsAt)} — {formatDateTime(event.endsAt)}</p>
        </div>
      </details>
    </div>
  );
}
