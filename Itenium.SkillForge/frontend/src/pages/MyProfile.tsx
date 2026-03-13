import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { fetchMyProfile, fetchCourses } from '@/api/client';

export function MyProfile() {
  const { t } = useTranslation();

  const { data: profile, isLoading: profileLoading } = useQuery({
    queryKey: ['myProfile'],
    queryFn: fetchMyProfile,
    retry: false,
  });

  const { data: courses, isLoading: coursesLoading } = useQuery({
    queryKey: ['courses', profile?.teamId],
    queryFn: () => fetchCourses(profile?.teamId ?? undefined),
    enabled: profile !== undefined,
  });

  if (profileLoading || coursesLoading) {
    return <div>{t('common.loading')}</div>;
  }

  if (!profile) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">{t('profile.myProfileTitle')}</h1>
          <p className="text-muted-foreground mt-2">{t('profile.myProfileDescription')}</p>
        </div>
        <div className="rounded-md border p-6 text-center text-muted-foreground">{t('profile.notAssigned')}</div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('profile.myProfileTitle')}</h1>
        <p className="text-muted-foreground mt-2">{t('profile.myProfileDescription')}</p>
      </div>

      <div className="rounded-md border p-4 flex items-center gap-3">
        <span className="font-semibold">{t('profile.assignedProfile')}:</span>
        <span className="text-lg">{profile.teamName}</span>
      </div>

      <div>
        <h2 className="text-xl font-semibold mb-3">{t('courses.title')}</h2>
        <div className="rounded-md border">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="p-3 text-left font-medium">{t('courses.name')}</th>
                <th className="p-3 text-left font-medium">{t('courses.category')}</th>
                <th className="p-3 text-left font-medium">{t('courses.level')}</th>
              </tr>
            </thead>
            <tbody>
              {courses?.map((course) => (
                <tr key={course.id} className="border-b">
                  <td className="p-3">{course.name}</td>
                  <td className="p-3">{course.category || '-'}</td>
                  <td className="p-3">{course.level || '-'}</td>
                </tr>
              ))}
              {courses?.length === 0 && (
                <tr>
                  <td colSpan={3} className="p-3 text-center text-muted-foreground">
                    {t('courses.noCourses')}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
