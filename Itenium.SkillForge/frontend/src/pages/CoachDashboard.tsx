import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { fetchCoachDashboard, type ConsultantSummary } from '@/api/client';

function ConsultantCard({ consultant }: { consultant: ConsultantSummary }) {
  const { t } = useTranslation();
  const initials = `${consultant.firstName.charAt(0)}${consultant.lastName.charAt(0)}`.toUpperCase();

  return (
    <div
      className={`border rounded-lg p-4 ${consultant.isInactive ? 'border-amber-400 bg-amber-50 dark:bg-amber-950/20' : ''}`}
    >
      <div className="flex items-start gap-3">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-muted text-sm font-medium">
          {initials}
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-medium">
            {consultant.firstName} {consultant.lastName}
          </p>
          <p className="text-sm text-muted-foreground truncate">{consultant.email}</p>
        </div>
        <div className="flex flex-col items-end gap-1 shrink-0">
          {consultant.isInactive && (
            <span className="text-xs font-medium text-amber-700 dark:text-amber-400 bg-amber-100 dark:bg-amber-900/40 rounded px-2 py-0.5">
              {t('dashboard.inactive')}
            </span>
          )}
          {consultant.isReady && (
            <span className="text-xs font-medium text-green-700 dark:text-green-400 bg-green-100 dark:bg-green-900/40 rounded px-2 py-0.5">
              {t('dashboard.ready')}
            </span>
          )}
          {consultant.isReady && consultant.readinessFlagAgeInDays !== null && (
            <span className="text-xs text-muted-foreground">
              {t('dashboard.readinessFlagAge', { days: consultant.readinessFlagAgeInDays })}
            </span>
          )}
          <span className="text-xs text-muted-foreground">
            {t('dashboard.goals')}: {consultant.activeGoalCount}
          </span>
        </div>
      </div>
    </div>
  );
}

export default function CoachDashboard() {
  const { t } = useTranslation();

  const { data: consultants = [], isLoading } = useQuery({
    queryKey: ['coach-dashboard'],
    queryFn: fetchCoachDashboard,
  });

  if (isLoading) {
    return <p>{t('common.loading')}</p>;
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">{t('dashboard.title')}</h1>

      {consultants.length === 0 ? (
        <p className="text-muted-foreground">{t('dashboard.noConsultants')}</p>
      ) : (
        <div className="space-y-3">
          {consultants.map((consultant) => (
            <ConsultantCard key={consultant.id} consultant={consultant} />
          ))}
        </div>
      )}
    </div>
  );
}
