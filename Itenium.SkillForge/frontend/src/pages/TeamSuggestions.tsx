import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Lightbulb, CheckCircle, XCircle, Clock } from 'lucide-react';
import { Button, Badge, Input } from '@itenium-forge/ui';
import {
  fetchSuggestions,
  reviewSuggestion,
  type CourseSuggestion,
  type SuggestionStatus,
} from '@/api/client';

function statusIcon(status: SuggestionStatus) {
  if (status === 'Approved') return <CheckCircle className="size-4 text-green-600" />;
  if (status === 'Rejected') return <XCircle className="size-4 text-destructive" />;
  return <Clock className="size-4 text-amber-500" />;
}

function statusVariant(status: SuggestionStatus): 'default' | 'destructive' | 'outline' | 'secondary' {
  if (status === 'Approved') return 'default';
  if (status === 'Rejected') return 'destructive';
  return 'secondary';
}

export function TeamSuggestions() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [reviewTarget, setReviewTarget] = useState<CourseSuggestion | null>(null);
  const [reviewNote, setReviewNote] = useState('');
  const [filter, setFilter] = useState<SuggestionStatus | '_all'>('_all');

  const { data: suggestions = [], isLoading } = useQuery({
    queryKey: ['suggestions'],
    queryFn: fetchSuggestions,
  });

  const reviewMutation = useMutation({
    mutationFn: ({ id, status }: { id: number; status: SuggestionStatus }) =>
      reviewSuggestion(id, status, reviewNote || null),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['suggestions'] });
      toast.success(t('suggestions.reviewed'));
      setReviewTarget(null);
      setReviewNote('');
    },
  });

  const handleReview = (status: SuggestionStatus) => {
    if (!reviewTarget) return;
    reviewMutation.mutate({ id: reviewTarget.id, status });
  };

  if (isLoading) return <div>{t('common.loading')}</div>;

  const filtered = filter === '_all' ? suggestions : suggestions.filter((s) => s.status === filter);
  const pending = suggestions.filter((s) => s.status === 'Pending').length;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('suggestions.teamTitle')}</h1>
          {pending > 0 && (
            <p className="text-muted-foreground mt-1">
              {pending} {t('suggestions.pendingCount')}
            </p>
          )}
        </div>
      </div>

      {/* Filter */}
      <div className="flex gap-2 flex-wrap">
        {(['_all', 'Pending', 'Approved', 'Rejected'] as const).map((f) => (
          <Button
            key={f}
            size="sm"
            variant={filter === f ? 'default' : 'outline'}
            onClick={() => setFilter(f)}
          >
            {f === '_all' ? t('common.all') : t(`suggestions.status.${f.toLowerCase()}`)}
          </Button>
        ))}
      </div>

      {/* Review panel */}
      {reviewTarget && (
        <div className="rounded-lg border p-4 space-y-3 bg-muted/30">
          <h3 className="font-semibold">{t('suggestions.reviewTitle')}: {reviewTarget.title}</h3>
          <Input
            placeholder={t('suggestions.reviewNotePlaceholder')}
            value={reviewNote}
            onChange={(e) => setReviewNote(e.target.value)}
          />
          <div className="flex gap-2">
            <Button
              size="sm"
              onClick={() => handleReview('Approved')}
              disabled={reviewMutation.isPending}
            >
              <CheckCircle className="size-4 mr-1" />
              {t('suggestions.approve')}
            </Button>
            <Button
              size="sm"
              variant="destructive"
              onClick={() => handleReview('Rejected')}
              disabled={reviewMutation.isPending}
            >
              <XCircle className="size-4 mr-1" />
              {t('suggestions.reject')}
            </Button>
            <Button size="sm" variant="outline" onClick={() => { setReviewTarget(null); setReviewNote(''); }}>
              {t('common.cancel')}
            </Button>
          </div>
        </div>
      )}

      {/* Suggestion list */}
      {filtered.length === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <Lightbulb className="size-12 mx-auto mb-3 opacity-30" />
          <p>{t('suggestions.noSuggestions')}</p>
        </div>
      ) : (
        <div className="space-y-3">
          {filtered.map((s) => (
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
                    <Button size="sm" variant="outline" onClick={() => { setReviewTarget(s); setReviewNote(''); }}>
                      {t('suggestions.review')}
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
