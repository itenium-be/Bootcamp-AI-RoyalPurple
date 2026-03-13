import { useTranslation } from 'react-i18next';
import { BookOpen, Users, Award } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@itenium-forge/ui';
import { useQuery } from '@tanstack/react-query';
import { fetchCourses, fetchDashboardStats } from '@/api/client';
import { useTeamStore } from '@/stores';

export function Dashboard() {
  const { t } = useTranslation();
  const { mode, selectedTeam } = useTeamStore();
  const { data: courses = [] } = useQuery({ queryKey: ['courses'], queryFn: fetchCourses });
  const { data: stats } = useQuery({ queryKey: ['dashboard-stats'], queryFn: fetchDashboardStats });
  const publishedCount = courses.filter((c) => c.status === 'Published').length;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
        <p className="text-muted-foreground">
          {t('dashboard.welcome')}
          {mode === 'manager' && selectedTeam && ` - ${selectedTeam.name}`}
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">{t('dashboard.totalCourses')}</CardTitle>
            <BookOpen className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{publishedCount}</div>
            <p className="text-xs text-muted-foreground">{courses.length} {t('dashboard.total')}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">{t('dashboard.activeLearners')}</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.activeLearners ?? '—'}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">{t('dashboard.completedCourses')}</CardTitle>
            <Award className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.completedEnrollments ?? '—'}</div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
