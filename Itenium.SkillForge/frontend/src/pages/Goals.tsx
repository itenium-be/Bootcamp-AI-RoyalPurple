import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { fetchGoals, raiseReadinessFlag, resolveReadinessFlag } from '@/api/client';

export default function Goals() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { data: goals = [], isLoading } = useQuery({
    queryKey: ['goals'],
    queryFn: () => fetchGoals(),
  });

  const raise = useMutation({
    mutationFn: raiseReadinessFlag,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['goals'] }),
  });

  const resolve = useMutation({
    mutationFn: resolveReadinessFlag,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['goals'] }),
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
            {goal.hasActiveReadinessFlag ? (
              <button
                className="text-sm text-yellow-700 border border-yellow-400 rounded px-2 py-1 mr-2"
                onClick={() => resolve.mutate(goal.id)}
              >
                {t('goals.resolveFlag')}
              </button>
            ) : (
              <button
                className="text-sm text-green-700 border border-green-400 rounded px-2 py-1 mr-2"
                onClick={() => raise.mutate(goal.id)}
              >
                {t('goals.raiseFlag')}
              </button>
            )}
          </div>

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
