import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { fetchGoals } from '@/api/client';

export default function Goals() {
  const { t } = useTranslation();

  const { data: goals = [], isLoading } = useQuery({
    queryKey: ['goals'],
    queryFn: () => fetchGoals(),
  });

  if (isLoading) {
    return <p>{t('common.loading')}</p>;
  }

  if (goals.length === 0) {
    return <p className="text-muted-foreground">{t('goals.noGoals')}</p>;
  }

  return (
    <div className="space-y-4">
      {goals.map((goal) => (
        <div key={goal.id} className="border rounded-lg p-4">
          <div className="flex items-start justify-between gap-4">
            <h3 className="font-semibold text-lg">{goal.skillName}</h3>
            <span className="text-sm text-muted-foreground whitespace-nowrap">
              {t('goals.deadline')}: {new Date(goal.deadline).toLocaleDateString()}
            </span>
          </div>

          <p className="text-sm text-muted-foreground mt-1">
            {t('goals.niveau')}: {goal.currentLevel} → {goal.targetLevel}
          </p>

          <div className="mt-3">
            <p className="text-sm font-medium mb-1">{t('goals.resources')}</p>
            {goal.resources.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t('goals.noResources')}</p>
            ) : (
              <ul className="space-y-1">
                {goal.resources.map((r) => (
                  <li key={r.id} className="text-sm">
                    <a href={r.url} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">
                      {r.title}
                    </a>
                    <span className="text-muted-foreground ml-1">({r.type})</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
