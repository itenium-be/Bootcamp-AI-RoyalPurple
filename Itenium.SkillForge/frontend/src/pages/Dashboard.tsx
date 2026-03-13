import { useTranslation } from 'react-i18next';
import { BookOpen, Users, ClipboardList } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@itenium-forge/ui';
import { useQuery } from '@tanstack/react-query';
import { useTeamStore } from '@/stores';
import { fetchDashboardStats, type DashboardStats } from '@/api/client';

export function Dashboard() {
  const { t } = useTranslation();
  const { mode, selectedTeam } = useTeamStore();

  const { data: stats } = useQuery<DashboardStats>({
    queryKey: ['dashboard-stats'],
    queryFn: fetchDashboardStats,
    enabled: mode === 'manager',
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
        <p className="text-muted-foreground">
          {t('dashboard.welcome')}
          {mode === 'manager' && selectedTeam && ` - ${selectedTeam.name}`}
        </p>
      </div>

      {mode === 'manager' && (
        <div className="grid gap-4 md:grid-cols-3">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">{t('dashboard.totalCourses')}</CardTitle>
              <BookOpen className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stats?.totalCourses ?? '—'}</div>
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
              <CardTitle className="text-sm font-medium">{t('dashboard.assignedCourses')}</CardTitle>
              <ClipboardList className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stats?.assignedCourses ?? '—'}</div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
