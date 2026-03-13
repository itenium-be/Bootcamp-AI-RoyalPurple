import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { BookOpen, CheckCircle, Clock, Trash2 } from 'lucide-react';
import { Button, Badge } from '@itenium-forge/ui';
import {
  fetchEnrollments,
  updateEnrollmentStatus,
  unenroll,
  type Enrollment,
  type EnrollmentStatus,
} from '@/api/client';

function statusVariant(status: EnrollmentStatus): 'outline' | 'secondary' | 'default' {
  if (status === 'Completed') return 'default';
  if (status === 'InProgress') return 'secondary';
  return 'outline';
}

function statusLabel(status: EnrollmentStatus, t: (k: string) => string): string {
  if (status === 'Completed') return t('enrollment.completed');
  if (status === 'InProgress') return t('enrollment.inProgress');
  return t('enrollment.enrolled');
}

export function MyCourses() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [unenrollTarget, setUnenrollTarget] = useState<Enrollment | null>(null);

  const { data: enrollments = [], isLoading } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: number; status: EnrollmentStatus }) =>
      updateEnrollmentStatus(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments'] });
      toast.success(t('enrollment.statusUpdated'));
    },
  });

  const unenrollMutation = useMutation({
    mutationFn: (id: number) => unenroll(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments'] });
      toast.success(t('enrollment.unenrollSuccess'));
      setUnenrollTarget(null);
    },
  });

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">{t('enrollment.title')}</h1>

      {/* Unenroll confirmation */}
      {unenrollTarget && (
        <div className="flex items-center justify-between rounded-md border border-destructive bg-destructive/10 p-3">
          <span className="text-sm">
            {t('enrollment.unenrollConfirm', { name: unenrollTarget.course.name })}
          </span>
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={() => setUnenrollTarget(null)}>
              {t('common.cancel')}
            </Button>
            <Button
              size="sm"
              variant="destructive"
              onClick={() => unenrollMutation.mutate(unenrollTarget.id)}
              disabled={unenrollMutation.isPending}
            >
              {t('enrollment.unenroll')}
            </Button>
          </div>
        </div>
      )}

      {enrollments.length === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <BookOpen className="size-12 mx-auto mb-3 opacity-30" />
          <p>{t('enrollment.noEnrollments')}</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {enrollments.map((enrollment) => (
            <div key={enrollment.id} className="rounded-lg border p-4 space-y-3">
              <div className="flex items-start justify-between gap-2">
                <h3 className="font-semibold">{enrollment.course.name}</h3>
                <Badge variant={statusVariant(enrollment.status)}>
                  {statusLabel(enrollment.status, t)}
                </Badge>
              </div>

              {enrollment.course.description && (
                <p className="text-sm text-muted-foreground line-clamp-2">
                  {enrollment.course.description}
                </p>
              )}

              <div className="flex items-center gap-1 text-xs text-muted-foreground">
                <Clock className="size-3" />
                <span>{t('enrollment.enrolledAt')}: {new Date(enrollment.enrolledAt).toLocaleDateString()}</span>
              </div>

              {enrollment.completedAt && (
                <div className="flex items-center gap-1 text-xs text-green-600">
                  <CheckCircle className="size-3" />
                  <span>{t('enrollment.completedAt')}: {new Date(enrollment.completedAt).toLocaleDateString()}</span>
                </div>
              )}

              <div className="flex gap-2 pt-1">
                {enrollment.status === 'Enrolled' && (
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => statusMutation.mutate({ id: enrollment.id, status: 'InProgress' })}
                    disabled={statusMutation.isPending}
                  >
                    {t('enrollment.markInProgress')}
                  </Button>
                )}
                {enrollment.status === 'InProgress' && (
                  <Button
                    size="sm"
                    onClick={() => statusMutation.mutate({ id: enrollment.id, status: 'Completed' })}
                    disabled={statusMutation.isPending}
                  >
                    {t('enrollment.markCompleted')}
                  </Button>
                )}
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => setUnenrollTarget(enrollment)}
                >
                  <Trash2 className="size-4 text-destructive" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
