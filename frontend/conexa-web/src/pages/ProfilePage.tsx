import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useLocation } from 'react-router-dom';
import { profileApi } from '../services/api/endpoints';
import { ApiError } from '../services/api/client';
import { useAsync } from '../hooks/useAsync';
import { Alert, Button, Input, Textarea } from '../components/ui';
import { AsyncView, PageHeader } from '../components/common';
import { emptyToNull, splitList } from '../utils/format';
import type { Profile } from '../types';

export function ProfilePage() {
  const { data, loading, error, reload } = useAsync(() => profileApi.me(), []);
  const location = useLocation();
  useEffect(() => {
    if (!loading && data && location.hash === '#privacidade') {
      document.getElementById('privacidade')?.scrollIntoView({ block: 'start' });
    }
  }, [data, loading, location.hash]);
  return (
    <>
      <PageHeader title="Meu perfil" subtitle="Defina o que os outros participantes podem ver." />
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data && <><ProfileForm key={data.userId + data.displayName} initial={data} onSaved={reload} /><PrivacyForm initial={data.privacy} email={data.email} /></>}
      </AsyncView>
    </>
  );
}

function ProfileForm({ initial, onSaved }: { initial: Profile; onSaved: () => void }) {
  const [f, setF] = useState({
    displayName: initial.displayName, photoUrl: initial.photoUrl ?? '', companyName: initial.companyName ?? '',
    jobTitle: initial.jobTitle ?? '', area: initial.area ?? '', bio: initial.bio ?? '',
    interests: initial.interests.join(', '), lookingFor: initial.lookingFor ?? '', offers: initial.offers ?? '',
  });
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const [fields, setFields] = useState<Record<string, string[]>>({});
  const [busy, setBusy] = useState(false);
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF({ ...f, [k]: e.target.value });
  const fe = (k: string) => fields[k]?.[0] ?? fields[k.charAt(0).toUpperCase() + k.slice(1)]?.[0];

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setMsg(null); setFields({});
    try {
      await profileApi.update({
        displayName: f.displayName.trim(), photoUrl: emptyToNull(f.photoUrl), companyName: emptyToNull(f.companyName),
        jobTitle: emptyToNull(f.jobTitle), area: emptyToNull(f.area), bio: emptyToNull(f.bio),
        interests: splitList(f.interests), lookingFor: emptyToNull(f.lookingFor), offers: emptyToNull(f.offers),
      });
      setMsg({ tone: 'success', text: 'Perfil atualizado.' });
      onSaved();
    } catch (err) {
      setMsg({ tone: 'error', text: (err as Error).message });
      if (err instanceof ApiError) setFields(err.fieldErrors);
    } finally { setBusy(false); }
  }

  return (
    <form className="card stack" onSubmit={submit}>
      <h2>Dados do perfil</h2>
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
      <Input label="Nome de exibição" required value={f.displayName} onChange={set('displayName')} error={fe('displayName')} />
      <Input label="Foto (URL, opcional)" type="url" value={f.photoUrl} onChange={set('photoUrl')} error={fe('photoUrl')} />
      <Input label="Empresa" value={f.companyName} onChange={set('companyName')} />
      <Input label="Cargo" value={f.jobTitle} onChange={set('jobTitle')} />
      <Input label="Área de atuação" value={f.area} onChange={set('area')} />
      <Textarea label="Apresentação" maxLength={1000} value={f.bio} onChange={set('bio')} />
      <Input label="Interesses" hint="Separe por vírgula. Ex.: startups, inteligência artificial" value={f.interests} onChange={set('interests')} />
      <Textarea label="O que busco no evento" maxLength={500} value={f.lookingFor} onChange={set('lookingFor')} />
      <Textarea label="O que posso oferecer" maxLength={500} value={f.offers} onChange={set('offers')} />
      <Button type="submit" loading={busy}>Salvar perfil</Button>
    </form>
  );
}

function PrivacyForm({ initial, email }: { initial: { emailVisible: boolean; phoneVisible: boolean; phone: string | null; consentGiven: boolean }; email: string }) {
  const [emailVisible, setEmailVisible] = useState(initial.emailVisible);
  const [phoneVisible, setPhoneVisible] = useState(initial.phoneVisible);
  const [phone, setPhone] = useState(initial.phone ?? '');
  const [consent, setConsent] = useState(initial.consentGiven);
  const [msg, setMsg] = useState<{ tone: 'success' | 'error'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);
  const anyVisible = emailVisible || phoneVisible;

  useEffect(() => { if (!anyVisible) setConsent(false); }, [anyVisible]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setMsg(null);
    try {
      await profileApi.updatePrivacy({ emailVisible, phoneVisible, phone: emptyToNull(phone), consentAccepted: consent });
      setMsg({ tone: 'success', text: 'Preferências de privacidade salvas.' });
    } catch (err) {
      setMsg({ tone: 'error', text: (err as Error).message });
    } finally { setBusy(false); }
  }

  return (
    <form id="privacidade" className="card stack" onSubmit={submit}>
      <h2>Privacidade de contato</h2>
      <p className="muted small">Por padrão seus contatos ficam ocultos. Você pode mudar isso a qualquer momento.</p>
      {msg && <Alert tone={msg.tone}>{msg.text}</Alert>}
      <label className="check"><input type="checkbox" checked={emailVisible} onChange={(e) => setEmailVisible(e.target.checked)} /> Mostrar meu e-mail ({email})</label>
      <label className="check"><input type="checkbox" checked={phoneVisible} onChange={(e) => setPhoneVisible(e.target.checked)} /> Mostrar meu telefone</label>
      <Input label="Telefone" type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} />
      {anyVisible && (
        <label className="check">
          <input type="checkbox" checked={consent} onChange={(e) => setConsent(e.target.checked)} />
          Autorizo expressamente a exibição dos contatos marcados aos participantes do evento.
        </label>
      )}
      <Button type="submit" loading={busy}>Salvar privacidade</Button>
    </form>
  );
}
