import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { MessageSquare, Star, Trash2 } from 'lucide-react';
import { Button, Badge } from '@itenium-forge/ui';
import {
  fetchFeedback,
  submitFeedback,
  deleteFeedback,
  fetchEnrollments,
  type Feedback,
} from '@/api/client';

function StarRating({ rating, onChange }: { rating: number; onChange?: (r: number) => void }) {
  return (
    <div className="flex gap-1">
      {[1, 2, 3, 4, 5].map((star) => (
        <button
          key={star}
          type="button"
          onClick={() => onChange?.(star)}
          className={onChange ? 'cursor-pointer' : 'cursor-default'}
        >
          <Star
            className={`size-5 ${star <= rating ? 'fill-yellow-400 text-yellow-400' : 'text-muted-foreground'}`}
          />
        </button>
      ))}
    </div>
  );
}

export function FeedbackPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [deleteTarget, setDeleteTarget] = useState<Feedback | null>(null);
  const [submitCourseId, setSubmitCourseId] = useState<number | null>(null);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');

  const { data: feedbackList = [], isLoading } = useQuery({
    queryKey: ['feedback'],
    queryFn: () => fetchFeedback(),
  });

  const { data: enrollments = [] } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
  });

  // Courses enrolled in but without feedback yet
  const feedbackCourseIds = new Set(feedbackList.map((f) => f.courseId));
  const pendingCourses = enrollments
    .map((e) => e.course)
    .filter((c) => !feedbackCourseIds.has(c.id));

  const submitMutation = useMutation({
    mutationFn: () => submitFeedback(submitCourseId!, rating, comment || null),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feedback'] });
      toast.success(t('feedback.submitted'));
      setSubmitCourseId(null);
      setRating(5);
      setComment('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteFeedback(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feedback'] });
      toast.success(t('feedback.deleted'));
      setDeleteTarget(null);
    },
  });

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">{t('feedback.title')}</h1>

      {/* Delete confirmation */}
      {deleteTarget && (
        <div className="flex items-center justify-between rounded-md border border-destructive bg-destructive/10 p-3">
          <span className="text-sm">
            {t('feedback.deleteConfirm', { name: deleteTarget.course.name })}
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

      {/* Submit new feedback */}
      {pendingCourses.length > 0 && (
        <div className="rounded-lg border p-4 space-y-4">
          <h2 className="font-semibold text-lg">{t('feedback.submitFeedback')}</h2>
          <div className="flex flex-wrap gap-2">
            {pendingCourses.map((course) => (
              <Button
                key={course.id}
                size="sm"
                variant={submitCourseId === course.id ? 'default' : 'outline'}
                onClick={() => setSubmitCourseId(course.id)}
              >
                {course.name}
              </Button>
            ))}
          </div>

          {submitCourseId != null && (
            <div className="space-y-3 border-t pt-3">
              <div className="space-y-1">
                <span className="text-sm font-medium">{t('feedback.rating')}</span>
                <StarRating rating={rating} onChange={setRating} />
              </div>
              <div className="space-y-1">
                <span className="text-sm font-medium">{t('feedback.comment')}</span>
                <textarea
                  className="w-full rounded-md border bg-background px-3 py-2 text-sm min-h-[80px] resize-none focus:outline-none focus:ring-2 focus:ring-ring"
                  placeholder={t('feedback.commentPlaceholder')}
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                />
              </div>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  onClick={() => submitMutation.mutate()}
                  disabled={submitMutation.isPending}
                >
                  {t('feedback.submitFeedback')}
                </Button>
                <Button size="sm" variant="outline" onClick={() => setSubmitCourseId(null)}>
                  {t('common.cancel')}
                </Button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Existing feedback */}
      {feedbackList.length === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <MessageSquare className="size-12 mx-auto mb-3 opacity-30" />
          <p>{t('feedback.noFeedback')}</p>
        </div>
      ) : (
        <div className="space-y-3">
          {feedbackList.map((item) => (
            <div key={item.id} className="rounded-lg border p-4 space-y-2">
              <div className="flex items-start justify-between gap-2">
                <div className="space-y-1">
                  <h3 className="font-semibold">{item.course.name}</h3>
                  <StarRating rating={item.rating} />
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant="outline">
                    {new Date(item.createdAt).toLocaleDateString()}
                  </Badge>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => setDeleteTarget(item)}
                  >
                    <Trash2 className="size-4 text-destructive" />
                  </Button>
                </div>
              </div>
              {item.comment && (
                <p className="text-sm text-muted-foreground">{item.comment}</p>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
