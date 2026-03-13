import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { BookOpen, CheckCircle, Clock, TrendingUp } from 'lucide-react';
import { fetchTeamProgress, type EnrollmentStatus } from '@/api/client';
import { useTeamStore } from '@/stores';

function statusIcon(status: EnrollmentStatus) {
  if (status === 'Completed') return <CheckCircle className="size-3 text-green-600" />;
  if (status === 'InProgress') return <Clock className="size-3 text-blue-500" />;
  return <BookOpen className="size-3 text-muted-foreground" />;
}

export function TeamProgress() {
  const { t } = useTranslation();
  const { selectedTeam } = useTeamStore();

  const { data: members = [], isLoading } = useQuery({
    queryKey: ['team-progress', selectedTeam?.id],
    queryFn: () => fetchTeamProgress(selectedTeam!.id),
    enabled: !!selectedTeam,
  });

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <TrendingUp className="size-6 text-muted-foreground" />
        <h1 className="text-3xl font-bold">
          {selectedTeam ? `${selectedTeam.name} — ${t('nav.teamProgress')}` : t('nav.teamProgress')}
        </h1>
      </div>

      {members.length === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <TrendingUp className="size-12 mx-auto mb-3 opacity-30" />
          <p>{t('teams.noMembers')}</p>
        </div>
      ) : (
        <div className="rounded-md border">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="p-3 text-left font-medium">{t('users.name')}</th>
                <th className="p-3 text-left font-medium">{t('users.email')}</th>
                <th className="p-3 text-center font-medium">{t('enrollment.enrolled')}</th>
                <th className="p-3 text-center font-medium">{t('enrollment.inProgress')}</th>
                <th className="p-3 text-center font-medium">{t('enrollment.completed')}</th>
                <th className="p-3 text-left font-medium">{t('learners.progress')}</th>
              </tr>
            </thead>
            <tbody>
              {members.map((member) => {
                const total = member.enrollments.length;
                const inProgress = member.enrollments.filter((e) => e.status === 'InProgress').length;
                const completed = member.enrollments.filter((e) => e.status === 'Completed').length;
                const completionPct = total > 0 ? Math.round((completed / total) * 100) : 0;

                return (
                  <tr key={member.userId} className="border-b last:border-0">
                    <td className="p-3 font-medium">{member.fullName}</td>
                    <td className="p-3 text-muted-foreground">{member.email}</td>
                    <td className="p-3 text-center">{total}</td>
                    <td className="p-3 text-center text-blue-500">{inProgress}</td>
                    <td className="p-3 text-center text-green-600">{completed}</td>
                    <td className="p-3">
                      <div className="flex items-center gap-2">
                        <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden">
                          <div
                            className="h-full bg-green-500 rounded-full transition-all"
                            style={{ width: `${completionPct}%` }}
                          />
                        </div>
                        <span className="text-xs text-muted-foreground w-10 text-right">
                          {completed} / {total}
                        </span>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Per-member course detail */}
      <div className="space-y-4">
        {members.filter((m) => m.enrollments.length > 0).map((member) => (
          <div key={member.userId} className="rounded-lg border p-4 space-y-2">
            <h2 className="font-semibold">{member.fullName}</h2>
            <div className="space-y-1">
              {member.enrollments.map((enrollment) => (
                <div key={enrollment.courseId} className="flex items-center gap-2 text-sm">
                  {statusIcon(enrollment.status)}
                  <span>{enrollment.courseName}</span>
                  {enrollment.completedAt && (
                    <span className="text-xs text-muted-foreground ml-auto">
                      {new Date(enrollment.completedAt).toLocaleDateString()}
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
