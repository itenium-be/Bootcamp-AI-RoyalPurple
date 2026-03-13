import { useTranslation } from 'react-i18next';
import { BookOpen, Users, Target } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@itenium-forge/ui';
import { useTeamStore, useGoalStore } from '@/stores';

export function Dashboard() {
  const { t } = useTranslation();
  const { mode, selectedTeam } = useTeamStore();
  const activeGoalsCount = useGoalStore((state) => state.goals.filter((g) => g.isActive).length);

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
            <div className="text-2xl font-bold">24</div>
            <p className="text-xs text-muted-foreground">+3 from last month</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">{t('dashboard.activeLearners')}</CardTitle>
            <Users className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">156</div>
            <p className="text-xs text-muted-foreground">Active this month</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">{t('dashboard.activeGoals')}</CardTitle>
            <Target className="size-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{activeGoalsCount}</div>
            <p className="text-xs text-muted-foreground">{t('dashboard.activeGoalsSubtitle')}</p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
