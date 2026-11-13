import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useCurrentEvent } from '../features/events/EventContext';
import { PostCard } from '../features/forum/PostCard';
import { forumApi } from '../services/api/endpoints';
import type { Post } from '../types';
import { Alert, Button, Card, EmptyState, Loading, Select, Textarea } from '../components/ui';
import { PageHeader } from '../components/common';

const categories = ['Dúvida', 'Opinião', 'Oportunidade', 'Aviso'];

export function ForumPage() {
  const ev = useCurrentEvent();
  const [posts, setPosts] = useState<Post[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [text, setText] = useState('');
  const [category, setCategory] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(async (p: number, replace: boolean) => {
    setLoading(true); setError(null);
    try {
      const res = await forumApi.posts(ev.id, p);
      setPosts((prev) => (replace ? res.items : [...prev, ...res.items]));
      setPage(res.page); setTotalPages(res.totalPages);
    } catch (e) { setError((e as Error).message); } finally { setLoading(false); }
  }, [ev.id]);

  useEffect(() => { void load(1, true); }, [load]);

  async function publish(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setError(null);
    try {
      const post = await forumApi.createPost(ev.id, text.trim(), category || null);
      setPosts((prev) => [post, ...prev]);
      setText(''); setCategory('');
    } catch (err) { setError((err as Error).message); } finally { setBusy(false); }
  }

  return (
    <>
      <PageHeader title="Fórum" subtitle="Dúvidas, opiniões e oportunidades do evento." />
      <form className="card stack" onSubmit={publish}>
        <Textarea label="Nova publicação" rows={3} maxLength={2000} required value={text} onChange={(e) => setText(e.target.value)} />
        <div className="row wrap">
          <div className="grow"><Select label="Assunto (opcional)" placeholder="Sem assunto" value={category}
            onChange={(e) => setCategory(e.target.value)} options={categories.map((c) => ({ value: c, label: c }))} /></div>
          <Button type="submit" loading={busy} disabled={!text.trim()}>Publicar</Button>
        </div>
      </form>
      {error && <Alert>{error}</Alert>}
      <div className="stack">
        {posts.map((p) => (
          <PostCard key={p.id} post={p}
            onChange={(np) => setPosts((prev) => prev.map((x) => (x.id === np.id ? np : x)))}
            onRemoved={(id) => setPosts((prev) => prev.filter((x) => x.id !== id))} />
        ))}
      </div>
      {loading && <Loading />}
      {!loading && posts.length === 0 && !error && <Card><EmptyState title="Ainda não há publicações" description="Seja o primeiro a escrever." /></Card>}
      {!loading && page < totalPages && <Button variant="secondary" onClick={() => void load(page + 1, false)}>Carregar mais</Button>}
    </>
  );
}
