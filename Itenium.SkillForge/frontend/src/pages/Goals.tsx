import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useTeamStore } from '@/stores';
import { fetchGoals, createGoal, type CreateGoalRequest } from '@/api/client';

interface AssignGoalForm {
  consultantUserId: string;
  skillName: string;
  currentNiveau: string;
  targetNiveau: string;
  deadline: string;
  linkedResources: string;
}

export function Goals() {
  const { t } = useTranslation();
  const { mode } = useTeamStore();
  const queryClient = useQueryClient();
  const isManager = mode === 'manager';
  const [showForm, setShowForm] = useState(false);

  const { data: goals, isLoading } = useQuery({
    queryKey: ['goals'],
    queryFn: fetchGoals,
  });

  const { mutate, isPending } = useMutation({
    mutationFn: (request: CreateGoalRequest) => createGoal(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['goals'] });
      setShowForm(false);
      reset();
    },
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AssignGoalForm>();

  const onSubmit = (data: AssignGoalForm) => {
    mutate({
      consultantUserId: data.consultantUserId,
      skillName: data.skillName,
      currentNiveau: parseInt(data.currentNiveau, 10),
      targetNiveau: parseInt(data.targetNiveau, 10),
      deadline: new Date(data.deadline).toISOString(),
      linkedResources: data.linkedResources || null,
    });
  };

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">{t('goals.title')}</h1>
        {isManager && !showForm && (
          <button
            type="button"
            onClick={() => setShowForm(true)}
            className="rounded bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
          >
            {t('goals.assignGoal')}
          </button>
        )}
      </div>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 rounded-md border p-4">
          <h2 className="text-lg font-semibold">{t('goals.assignGoal')}</h2>

          <div className="space-y-1">
            <label htmlFor="consultantUserId" className="text-sm font-medium">
              {t('goals.consultantUserId')}
            </label>
            <input
              id="consultantUserId"
              {...register('consultantUserId', { required: true })}
              className="w-full rounded border px-3 py-2 text-sm"
            />
            {errors.consultantUserId && <p className="text-xs text-destructive">{errors.consultantUserId.message}</p>}
          </div>

          <div className="space-y-1">
            <label htmlFor="skillName" className="text-sm font-medium">
              {t('goals.skillName')}
            </label>
            <input
              id="skillName"
              {...register('skillName', { required: true })}
              className="w-full rounded border px-3 py-2 text-sm"
            />
            {errors.skillName && <p className="text-xs text-destructive">{errors.skillName.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label htmlFor="currentNiveau" className="text-sm font-medium">
                {t('goals.currentNiveau')}
              </label>
              <input
                id="currentNiveau"
                type="number"
                min={1}
                max={7}
                {...register('currentNiveau', { required: true, min: 1, max: 7 })}
                className="w-full rounded border px-3 py-2 text-sm"
              />
              {errors.currentNiveau && <p className="text-xs text-destructive">{errors.currentNiveau.message}</p>}
            </div>

            <div className="space-y-1">
              <label htmlFor="targetNiveau" className="text-sm font-medium">
                {t('goals.targetNiveau')}
              </label>
              <input
                id="targetNiveau"
                type="number"
                min={1}
                max={7}
                {...register('targetNiveau', { required: true, min: 1, max: 7 })}
                className="w-full rounded border px-3 py-2 text-sm"
              />
              {errors.targetNiveau && <p className="text-xs text-destructive">{errors.targetNiveau.message}</p>}
            </div>
          </div>

          <div className="space-y-1">
            <label htmlFor="deadline" className="text-sm font-medium">
              {t('goals.deadline')}
            </label>
            <input
              id="deadline"
              type="date"
              {...register('deadline', { required: true })}
              className="w-full rounded border px-3 py-2 text-sm"
            />
            {errors.deadline && <p className="text-xs text-destructive">{errors.deadline.message}</p>}
          </div>

          <div className="space-y-1">
            <label htmlFor="linkedResources" className="text-sm font-medium">
              {t('goals.linkedResources')}
            </label>
            <textarea
              id="linkedResources"
              {...register('linkedResources')}
              rows={3}
              className="w-full rounded border px-3 py-2 text-sm"
            />
          </div>

          <div className="flex gap-2">
            <button
              type="submit"
              disabled={isPending}
              className="rounded bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
            >
              {t('common.save')}
            </button>
            <button
              type="button"
              onClick={() => {
                setShowForm(false);
                reset();
              }}
              className="rounded border px-4 py-2 text-sm font-medium"
            >
              {t('common.cancel')}
            </button>
          </div>
        </form>
      )}

      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('goals.skillName')}</th>
              <th className="p-3 text-left font-medium">{t('goals.consultant')}</th>
              <th className="p-3 text-left font-medium">{t('goals.niveau')}</th>
              <th className="p-3 text-left font-medium">{t('goals.deadline')}</th>
              <th className="p-3 text-left font-medium">{t('goals.status')}</th>
            </tr>
          </thead>
          <tbody>
            {goals?.map((goal) => (
              <tr key={goal.id} className="border-b">
                <td className="p-3">{goal.skillName}</td>
                <td className="p-3 text-muted-foreground">{goal.consultantUserId}</td>
                <td className="p-3">
                  {goal.currentNiveau} → {goal.targetNiveau}
                </td>
                <td className="p-3">{new Date(goal.deadline).toLocaleDateString()}</td>
                <td className="p-3">
                  {goal.isActive && (
                    <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
                      {t('common.active')}
                    </span>
                  )}
                </td>
              </tr>
            ))}
            {goals?.length === 0 && (
              <tr>
                <td colSpan={5} className="p-3 text-center text-muted-foreground">
                  {t('goals.noGoals')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
