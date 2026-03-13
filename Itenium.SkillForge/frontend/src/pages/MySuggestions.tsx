import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Lightbulb, Trash2, Clock, CheckCircle, XCircle } from 'lucide-react';
import { Button, Badge, Input } from '@itenium-forge/ui';
import {
  fetchSuggestions,
  submitSuggestion,
  deleteSuggestion,
  type CourseSuggestion,
  type SuggestionStatus,
} from '@/api/client';

function statusIcon(status: SuggestionStatus) {
  if (status === 'Approved') return <CheckCircle className="size-4 text-green-600" />;
  if (status === 'Rejected') return <XCircle className="size-4 text-destructive" />;
  return <Clock className="size-4 text-muted-foreground" />;
}

function statusVariant(status: SuggestionStatus): 'default' | 'destructive' | 'outline' {
  if (status === 'Approved') return 'default';
  if (status === 'Rejected') return 'destructive';
  return 'outline';
}

export function MySuggestions() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [reason, setReason] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<CourseSuggestion | null>(null);

  const { data: suggestions = [], isLoading } = useQuery({
    queryKey: ['suggestions'],
    queryFn: fetchSuggestions,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitSuggestion({ title, description: description || null, reason: reason || null }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suggestions'] });
      toast.success(t('suggestions.submitted'));
      setTitle('');
      setDescription('');
      setReason('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteSuggestion(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suggestions'] });
      toast.success(t('suggestions.deleted'));
      setDeleteTarget(null);
    },
  });

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">{t('nav.mySuggestions')}</h1>

      {/* Submit form */}
      <div className="rounded-lg border p-5 space-y-4">
        <h2 className="font-semibold text-lg">{t('suggestions.newSuggestion')}</h2>
        <div className="space-y-3">
          <Input
            placeholder={t('suggestions.titlePlaceholder')}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />
          <Input
            placeholder={t('suggestions.descriptionPlaceholder')}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <Input
            placeholder={t('suggestions.reasonPlaceholder')}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
          />
        </div>
        <Button
          onClick={() => submitMutation.mutate()}
          disabled={!title.trim() || submitMutation.isPending}
        >
          <Lightbulb className="size-4 mr-2" />
          {t('suggestions.submit')}
        </Button>
      </div>

      {/* Delete confirmation */}
      {deleteTarget && (
        <div className="flex items-center justify-between rounded-md border border-destructive bg-destructive/10 p-3">
          <span className="text-sm">
            {t('suggestions.deleteConfirm', { title: deleteTarget.title })}
          </span>
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={() => setDeleteTarget(null)}>
              {t('common.cancel')}
            </Button>
            <Button
              size="sm"
              variant="destructive"
              onClick={() => deleteMutation.mutate(deleteTarget.id)}
              disabled={deleteMutation.isPending}
            >
              {t('common.delete')}
            </Button>
          </div>
        </div>
      )}

      {/* Suggestion list */}
      {suggestions.length === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <Lightbulb className="size-12 mx-auto mb-3 opacity-30" />
          <p>{t('suggestions.noSuggestions')}</p>
        </div>
      ) : (
        <div className="space-y-3">
          {suggestions.map((s) => (
            <div key={s.id} className="rounded-lg border p-4 space-y-2">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-2">
                  {statusIcon(s.status)}
                  <h3 className="font-semibold">{s.title}</h3>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <Badge variant={statusVariant(s.status)}>
                    {t(`suggestions.status.${s.status.toLowerCase()}`)}
                  </Badge>
                  {s.status === 'Pending' && (
                    <Button size="sm" variant="ghost" onClick={() => setDeleteTarget(s)}>
                      <Trash2 className="size-4 text-destructive" />
                    </Button>
                  )}
                </div>
              </div>
              {s.description && <p className="text-sm text-muted-foreground">{s.description}</p>}
              {s.reason && (
                <p className="text-xs text-muted-foreground italic">{t('suggestions.reason')}: {s.reason}</p>
              )}
              {s.reviewNote && (
                <p className="text-sm border-l-2 pl-3 border-muted-foreground/30 text-muted-foreground">
                  {t('suggestions.reviewNote')}: {s.reviewNote}
                </p>
              )}
              <p className="text-xs text-muted-foreground">
                {new Date(s.submittedAt).toLocaleDateString()}
              </p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
