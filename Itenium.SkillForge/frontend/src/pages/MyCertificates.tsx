import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Award, CheckCircle } from 'lucide-react';
import { Badge } from '@itenium-forge/ui';
import { fetchEnrollments } from '@/api/client';

export function MyCertificates() {
  const { t } = useTranslation();

  const { data: enrollments = [], isLoading } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
  });

  if (isLoading) return <div>{t('common.loading')}</div>;

  const completed = enrollments.filter((e) => e.status === 'Completed');

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('nav.myCertificates')}</h1>
        {completed.length > 0 && (
          <p className="text-muted-foreground mt-1">
            {completed.length} {t('certificates.earned')}
          </p>
        )}
      </div>

      {completed.length === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <Award className="size-12 mx-auto mb-3 opacity-30" />
          <p>{t('certificates.noCertificates')}</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {completed.map((enrollment) => (
            <div
              key={enrollment.id}
              className="rounded-lg border p-5 space-y-3 bg-gradient-to-br from-background to-muted/30"
            >
              <div className="flex items-start justify-between gap-2">
                <Award className="size-8 text-yellow-500 shrink-0" />
                <Badge variant="default">
                  <CheckCircle className="size-3 mr-1" />
                  {t('enrollment.completed')}
                </Badge>
              </div>

              <div>
                <h3 className="font-semibold text-lg leading-tight">{enrollment.course.name}</h3>
                {enrollment.course.category && (
                  <p className="text-sm text-muted-foreground mt-0.5">{enrollment.course.category}</p>
                )}
              </div>

              {enrollment.course.level && (
                <Badge variant="outline">{enrollment.course.level}</Badge>
              )}

              {enrollment.completedAt && (
                <p className="text-xs text-muted-foreground border-t pt-3">
                  {t('certificates.completedOn')}: {new Date(enrollment.completedAt).toLocaleDateString()}
                </p>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
