import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { fetchMyEnrollments, type Enrollment } from '@/api/client';

export function Progress() {
  const { t } = useTranslation();

  const { data: enrollments, isLoading } = useQuery({
    queryKey: ['my-enrollments'],
    queryFn: fetchMyEnrollments,
  });

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('progress.title')}</h1>
      </div>

      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('courses.name')}</th>
              <th className="p-3 text-left font-medium">{t('progress.enrolledAt')}</th>
            </tr>
          </thead>
          <tbody>
            {enrollments?.map((enrollment: Enrollment) => (
              <tr key={enrollment.id} className="border-b">
                <td className="p-3">{enrollment.courseName}</td>
                <td className="p-3 text-muted-foreground">{new Date(enrollment.enrolledAt).toLocaleDateString()}</td>
              </tr>
            ))}
            {enrollments?.length === 0 && (
              <tr>
                <td colSpan={2} className="p-3 text-center text-muted-foreground">
                  {t('progress.noEnrollments')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
