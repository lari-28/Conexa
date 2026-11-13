import { useState } from 'react';
import type { FormEvent } from 'react';
import { forumApi } from '../../services/api/endpoints';
import type { Post, PostComment, ReactionType } from '../../types';
import { Alert, Avatar, Badge, Button, Card, Modal, Textarea } from '../../components/ui';
import { timeAgo } from '../../utils/format';

const reactions: { type: ReactionType; icon: string; label: string }[] = [
  { type: 'Like', icon: '👍', label: 'Curtir' },
  { type: 'Insightful', icon: '💡', label: 'Útil' },
  { type: 'Celebrate', icon: '🎉', label: 'Celebrar' },
];

export function PostCard({ post, onChange, onRemoved }: { post: Post; onChange: (p: Post) => void; onRemoved: (id: string) => void }) {
  const [open, setOpen] = useState(false);
  const [comments, setComments] = useState<PostComment[] | null>(null);
  const [text, setText] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [reporting, setReporting] = useState(false);
  const [reason, setReason] = useState('');
  const [notice, setNotice] = useState<string | null>(null);

  const run = async (fn: () => Promise<void>) => {
    setError(null);
    try { await fn(); } catch (e) { setError((e as Error).message); }
  };

  const toggleReaction = (type: ReactionType) => run(async () => {
    const res = post.myReaction === type ? await forumApi.unreact(post.id) : await forumApi.react(post.id, type);
    onChange({ ...post, reactionCount: res.count, myReaction: res.mine });
  });

  const toggleComments = () => run(async () => {
    if (!open && comments === null) setComments((await forumApi.comments(post.id)).items);
    setOpen(!open);
  });

  const addComment = (e: FormEvent) => { e.preventDefault(); void run(async () => {
    const c = await forumApi.addComment(post.id, text.trim());
    setComments([...(comments ?? []), c]);
    setText('');
    onChange({ ...post, commentCount: post.commentCount + 1 });
  }); };

  const removePost = () => run(async () => {
    if (!window.confirm('Remover esta publicação?')) return;
    await forumApi.deletePost(post.id);
    onRemoved(post.id);
  });

  const removeComment = (id: string) => run(async () => {
    await forumApi.deleteComment(id);
    setComments((comments ?? []).filter((c) => c.id !== id));
    onChange({ ...post, commentCount: Math.max(0, post.commentCount - 1) });
  });

  const sendReport = (e: FormEvent) => { e.preventDefault(); void run(async () => {
    await forumApi.report(post.eventId, 'Post', post.id, reason.trim());
    setReporting(false); setReason('');
    setNotice('Denúncia enviada à moderação. Obrigado.');
  }); };

  return (
    <Card>
      <div className="row">
        <Avatar name={post.author.displayName} url={post.author.photoUrl} />
        <div className="grow">
          <strong>{post.author.displayName}</strong>
          <div className="muted small">{timeAgo(post.createdAt)}{post.updatedAt && ' · editado'}</div>
        </div>
        {post.category && <Badge tone="brand">{post.category}</Badge>}
      </div>
      <p className="post-text">{post.text}</p>
      {error && <Alert>{error}</Alert>}
      {notice && <Alert tone="success">{notice}</Alert>}
      <div className="row wrap">
        {reactions.map((r) => (
          <button key={r.type} className={`chip ${post.myReaction === r.type ? 'chip-on' : ''}`} onClick={() => void toggleReaction(r.type)} aria-pressed={post.myReaction === r.type}>
            {r.icon} {r.label}
          </button>
        ))}
        <span className="muted small">{post.reactionCount} reação(ões)</span>
        <span className="grow" />
        <Button variant="ghost" onClick={() => void toggleComments()}>💬 {post.commentCount}</Button>
        {post.canDelete && <Button variant="ghost" onClick={() => void removePost()}>Remover</Button>}
        {!post.canEdit && <Button variant="ghost" onClick={() => setReporting(true)}>Denunciar</Button>}
      </div>

      {open && (
        <div className="comments">
          {(comments ?? []).map((c) => (
            <div key={c.id} className="comment">
              <Avatar name={c.author.displayName} url={c.author.photoUrl} size={28} />
              <div className="grow"><strong>{c.author.displayName}</strong> <span className="muted small">{timeAgo(c.createdAt)}</span><div>{c.text}</div></div>
              {c.canDelete && <button className="chip" onClick={() => void removeComment(c.id)} aria-label="Remover comentário">✕</button>}
            </div>
          ))}
          <form onSubmit={addComment} className="stack">
            <Textarea label="Comentar" rows={2} maxLength={1000} required value={text} onChange={(e) => setText(e.target.value)} />
            <Button type="submit" disabled={!text.trim()}>Comentar</Button>
          </form>
        </div>
      )}

      {reporting && (
        <Modal title="Denunciar publicação" onClose={() => setReporting(false)}>
          <form onSubmit={sendReport} className="stack">
            <Textarea label="Motivo" required minLength={5} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} />
            <Button type="submit">Enviar denúncia</Button>
          </form>
        </Modal>
      )}
    </Card>
  );
}
