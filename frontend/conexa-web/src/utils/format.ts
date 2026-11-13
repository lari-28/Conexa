export const formatDateTime = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

export const formatDate = (iso: string) => new Date(iso).toLocaleDateString('pt-BR', { dateStyle: 'long' });

export function timeAgo(iso: string): string {
  const diff = Math.floor((Date.now() - new Date(iso).getTime()) / 1000);
  if (diff < 60) return 'agora';
  if (diff < 3600) return `${Math.floor(diff / 60)} min`;
  if (diff < 86400) return `${Math.floor(diff / 3600)} h`;
  return new Date(iso).toLocaleDateString('pt-BR');
}

/** Converte ISO (UTC) para o valor de <input type="datetime-local"> no fuso local. */
export function toLocalInput(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export const fromLocalInput = (value: string) => new Date(value).toISOString();

export const emptyToNull = (s: string) => (s.trim() === '' ? null : s.trim());

export const splitList = (s: string) => s.split(',').map((x) => x.trim()).filter(Boolean);

export const roleLabel: Record<string, string> = {
  Participant: 'Participante',
  Exhibitor: 'Expositor',
  Organizer: 'Organizador',
  PlatformAdmin: 'Administrador',
};
