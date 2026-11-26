import { useMemo } from 'react';
import { useCurrentEvent } from '../features/events/EventContext';
import { eventsApi } from '../services/api/endpoints';
import { useAsync } from '../hooks/useAsync';
import type { EventActivityInfo } from '../types';
import { roleLabel } from '../utils/format';
import { Badge, Card, EmptyState } from '../components/ui';
import { AsyncView, PageHeader } from '../components/common';

function groupActivities(activities: EventActivityInfo[]) {
  const days = new Map<string, { date: Date; slots: Map<string, EventActivityInfo[]> }>();
  for (const activity of activities) {
    const start = new Date(activity.startsAt);
    const dayKey = `${start.getFullYear()}-${start.getMonth()}-${start.getDate()}`;
    const timeKey = `${String(start.getHours()).padStart(2, '0')}:${String(start.getMinutes()).padStart(2, '0')}`;
    let day = days.get(dayKey);
    if (!day) { day = { date: start, slots: new Map() }; days.set(dayKey, day); }
    const slot = day.slots.get(timeKey) ?? [];
    slot.push(activity);
    day.slots.set(timeKey, slot);
  }
  return [...days.values()].map((day) => ({ ...day, slots: [...day.slots.entries()] }));
}

const clock = (value: string) => new Date(value).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });

export function ProgramPage() {
  const event = useCurrentEvent();
  const { data, loading, error } = useAsync(() => eventsApi.activities(event.id), [event.id]);
  const days = useMemo(() => groupActivities(data ?? []), [data]);

  return (
    <div className="program-page stack">
      <PageHeader title="Programação" subtitle={`${event.name} · horários, locais e pessoas de cada atividade.`} />
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data?.length === 0 ? <Card><EmptyState title="A programação ainda não foi publicada" description="Quando a organização cadastrar as atividades, os horários e locais aparecerão aqui." /></Card> : (
          <div className="program-days stack">
            {days.map((day) => <section className="program-day stack" key={day.date.toDateString()}>
              <h2>{day.date.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })}</h2>
              {day.slots.map(([time, activities]) => (
                <div className="program-slot" key={time}>
                  <div className="program-slot-time"><time>{time}</time>
                    {activities.length > 1 && <span>{activities.length} atividades simultâneas</span>}
                  </div>
                  <div className="program-slot-items">
                    {activities.map((activity) => <Card className="program-activity" key={activity.id}>
                      <div className="program-activity-head">
                        <Badge tone={activity.kind === 'Talk' ? 'brand' : 'neutral'}>{activity.kind === 'Talk' ? 'Palestra' : 'Atividade'}</Badge>
                        <span className="muted small">{clock(activity.startsAt)}–{clock(activity.endsAt)}</span>
                      </div>
                      <h3>{activity.title}</h3>
                      {activity.description && <p>{activity.description}</p>}
                      <p className="program-location"><strong>Local</strong><span>{activity.location}</span></p>
                      {activity.people.length > 0 && <div className="program-speakers">
                        <strong>{activity.people.length === 1 ? 'Pessoa vinculada' : 'Pessoas vinculadas'}</strong>
                        <ul className="plain">
                          {activity.people.map((person) => <li key={person.userId}>
                            <span>{person.displayName}</span>
                            <span className="muted small">{[person.title, person.companyName, roleLabel[person.roleInEvent]].filter(Boolean).join(' · ')}</span>
                          </li>)}
                        </ul>
                      </div>}
                    </Card>)}
                  </div>
                </div>
              ))}
            </section>)}
          </div>
        )}
      </AsyncView>
    </div>
  );
}
