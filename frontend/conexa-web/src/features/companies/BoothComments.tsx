import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { AsyncView } from '../../components/common';
import { Alert, Avatar, Button, Card, EmptyState, Textarea } from '../../components/ui';
import { useAsync } from '../../hooks/useAsync';
import { companiesApi } from '../../services/api/endpoints';
import { timeAgo } from '../../utils/format';

export function BoothComments({ eventId, companyId, companyName }: {
  eventId: string;
  companyId: string;
  companyName: string;
}) {
  const { data, loading, error, reload } = useAsync(
    () => companiesApi.comments(eventId, companyId), [eventId, companyId],
  );
  const [text, setText] = useState('');
  const [saving, setSaving] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const clean = text.trim();
    if (!clean || saving) return;
    setSaving(true);
    setActionError(null);
    try {
      await companiesApi.addComment(eventId, companyId, clean);
      setText('');
      reload();
    } catch (err) {
      setActionError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async (commentId: string) => {
    if (!window.confirm('Remover este comentário?')) return;
    setActionError(null);
    try {
      await companiesApi.deleteComment(commentId);
      reload();
    } catch (err) {
      setActionError((err as Error).message);
    }
  };

  return (
    <section id="booth-comments" className="booth-comments-section" aria-labelledby="booth-comments-title">
      <div className="booth-comments-heading">
        <div><span className="eyebrow">CONVERSA DO ESTANDE</span><h2 id="booth-comments-title">Comentários</h2></div>
        <p>Troque experiências com quem visitou {companyName}.</p>
      </div>
      <AsyncView loading={loading} error={error} hasData={!!data}>
        {data && !data.canView ? (
          <Card className="booth-checkin-gate">
            <span className="booth-gate-icon" aria-hidden>✓</span>
            <div><strong>Faça check-in para participar</strong>
              <p className="muted">Depois de escanear o QR Code deste estande, você poderá ler e publicar comentários.</p></div>
          </Card>
        ) : data && (
          <div className="stack">
            {actionError && <Alert>{actionError}</Alert>}
            <Card className="booth-comment-composer">
              <form onSubmit={submit} className="stack">
                <Textarea label="Deixe seu comentário" placeholder={`Compartilhe uma impressão sobre ${companyName}…`}
                  maxLength={1000} required value={text} onChange={(e) => setText(e.target.value)} />
                <div className="row wrap booth-comment-form-footer"><small className="muted">{text.length}/1000 caracteres</small>
                  <Button type="submit" loading={saving} disabled={!text.trim()}>Publicar comentário</Button></div>
              </form>
            </Card>
            <div className="booth-comments-list">
              <div className="booth-comments-count"><h3>Da comunidade</h3><span>{data.totalCount}</span></div>
              {data.items.length === 0 ? <Card><EmptyState title="A conversa começa com você"
                description="Seu comentário ficará visível para quem já fez check-in neste estande." /></Card> : (
                <div className="stack">
                  {data.items.map((comment) => <Card className="booth-comment" key={comment.id}>
                    <div className="row booth-comment-author"><Avatar name={comment.author.displayName} url={comment.author.photoUrl} size={38} />
                      <div className="grow"><Link to={`/participantes/${comment.author.id}`}><strong>{comment.author.displayName}</strong></Link>
                        <small className="muted">{timeAgo(comment.createdAt)}</small></div>
                      {comment.canDelete && <Button variant="ghost" onClick={() => void remove(comment.id)}>Remover</Button>}
                    </div>
                    <p>{comment.text}</p>
                  </Card>)}
                </div>
              )}
            </div>
          </div>
        )}
      </AsyncView>
    </section>
  );
}
