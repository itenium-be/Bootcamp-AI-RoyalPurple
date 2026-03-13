import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp, BookOpen, Clock, CheckCircle } from 'lucide-react';
import { Badge } from '@itenium-forge/ui';
import { fetchEnrollments, type EnrollmentStatus } from '@/api/client';

function statusVariant(status: EnrollmentStatus): 'outline' | 'secondary' | 'default' {
  if (status === 'Completed') return 'default';
  if (status === 'InProgress') return 'secondary';
  return 'outline';
}

export function MyProgress() {
  const { t } = useTranslation();

  const { data: enrollments = [], isLoading } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
  });

  if (isLoading) return <div>{t('common.loading')}</div>;

  const total = enrollments.length;
  const inProgress = enrollments.filter((e) => e.status === 'InProgress').length;
  const completed = enrollments.filter((e) => e.status === 'Completed').length;
  const completionRate = total > 0 ? Math.round((completed / total) * 100) : 0;

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">{t('nav.myProgress')}</h1>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="rounded-lg border p-4 space-y-1">
          <p className="text-sm text-muted-foreground">{t('progress.totalCourses')}</p>
          <p className="text-3xl font-bold">{total}</p>
        </div>
        <div className="rounded-lg border p-4 space-y-1">
          <p className="text-sm text-muted-foreground">{t('progress.inProgress')}</p>
          <p className="text-3xl font-bold text-blue-500">{inProgress}</p>
        </div>
        <div className="rounded-lg border p-4 space-y-1">
          <p className="text-sm text-muted-foreground">{t('progress.completed')}</p>
          <p className="text-3xl font-bold text-green-600">{completed}</p>
        </div>
        <div className="rounded-lg border p-4 space-y-1">
          <p className="text-sm text-muted-foreground">{t('progress.completionRate')}</p>
          <p className="text-3xl font-bold">{completionRate}%</p>
        </div>
      </div>

      {/* Progress bar */}
      {total > 0 && (
        <div className="space-y-2">
          <div className="flex justify-between text-sm text-muted-foreground">
            <span>{t('progress.completionRate')}</span>
            <span>{completed}/{total}</span>
          </div>
          <div className="h-3 rounded-full bg-muted overflow-hidden">
            <div
              className="h-full bg-green-600 rounded-full transition-all"
              style={{ width: `${completionRate}%` }}
            />
          </div>
        </div>
      )}

      {/* Course list */}
      {total === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <TrendingUp className="size-12 mx-auto mb-3 opacity-30" />
          <p>{t('progress.noCourses')}</p>
        </div>
      ) : (
        <div className="space-y-2">
          {enrollments.map((enrollment) => (
            <div
              key={enrollment.id}
              className="flex items-center justify-between rounded-lg border px-4 py-3 gap-4"
            >
              <div className="flex items-center gap-3 min-w-0">
                {enrollment.status === 'Completed' ? (
                  <CheckCircle className="size-5 text-green-600 shrink-0" />
                ) : enrollment.status === 'InProgress' ? (
                  <Clock className="size-5 text-blue-500 shrink-0" />
                ) : (
                  <BookOpen className="size-5 text-muted-foreground shrink-0" />
                )}
                <div className="min-w-0">
                  <p className="font-medium truncate">{enrollment.course.name}</p>
                  {enrollment.course.category && (
                    <p className="text-xs text-muted-foreground">{enrollment.course.category}</p>
                  )}
                </div>
              </div>
              <div className="flex items-center gap-3 shrink-0">
                {enrollment.completedAt && (
                  <span className="text-xs text-muted-foreground hidden sm:block">
                    {new Date(enrollment.completedAt).toLocaleDateString()}
                  </span>
                )}
                <Badge variant={statusVariant(enrollment.status)}>
                  {t(`enrollment.${enrollment.status === 'InProgress' ? 'inProgress' : enrollment.status.toLowerCase()}`)}
                </Badge>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
