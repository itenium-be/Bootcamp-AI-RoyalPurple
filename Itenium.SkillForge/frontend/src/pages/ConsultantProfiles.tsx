import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@itenium-forge/ui';
import { fetchConsultants, assignProfile, removeProfile, fetchUserTeams } from '@/api/client';

export function ConsultantProfiles() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [assigningUserId, setAssigningUserId] = useState<string | null>(null);
  const [selectedTeamId, setSelectedTeamId] = useState<string>('');

  const { data: consultants, isLoading } = useQuery({
    queryKey: ['consultants'],
    queryFn: fetchConsultants,
  });

  const { data: teams } = useQuery({
    queryKey: ['teams'],
    queryFn: fetchUserTeams,
  });

  const assignMutation = useMutation({
    mutationFn: ({ userId, teamId }: { userId: string; teamId: number }) => assignProfile(userId, teamId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['consultants'] });
      setAssigningUserId(null);
      setSelectedTeamId('');
    },
  });

  const removeMutation = useMutation({
    mutationFn: (userId: string) => removeProfile(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['consultants'] });
    },
  });

  const handleAssign = (userId: string) => {
    if (!selectedTeamId) return;
    assignMutation.mutate({ userId, teamId: parseInt(selectedTeamId) });
  };

  if (isLoading) {
    return <div>{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('profile.title')}</h1>
      </div>

      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('profile.consultant')}</th>
              <th className="p-3 text-left font-medium">{t('profile.assignedProfile')}</th>
              <th className="p-3 text-left font-medium">{t('courses.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {consultants?.map((consultant) => (
              <tr key={consultant.userId} className="border-b">
                <td className="p-3">{consultant.firstName} {consultant.lastName}</td>
                <td className="p-3">
                  {consultant.teamName ?? <span className="text-muted-foreground">{t('profile.noProfile')}</span>}
                </td>
                <td className="p-3">
                  <div className="flex items-center gap-2">
                    {assigningUserId === consultant.userId ? (
                      <>
                        <Select value={selectedTeamId} onValueChange={setSelectedTeamId}>
                          <SelectTrigger className="w-40">
                            <SelectValue placeholder={t('profile.assignProfile')} />
                          </SelectTrigger>
                          <SelectContent>
                            {teams?.map((team) => (
                              <SelectItem key={team.id} value={String(team.id)}>
                                {team.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <Button
                          size="sm"
                          onClick={() => handleAssign(consultant.userId)}
                          disabled={!selectedTeamId || assignMutation.isPending}
                        >
                          {t('common.save')}
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => {
                            setAssigningUserId(null);
                            setSelectedTeamId('');
                          }}
                        >
                          {t('common.cancel')}
                        </Button>
                      </>
                    ) : (
                      <>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => {
                            setAssigningUserId(consultant.userId);
                            setSelectedTeamId('');
                          }}
                        >
                          {t('profile.assignProfile')}
                        </Button>
                        {consultant.teamId && (
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => removeMutation.mutate(consultant.userId)}
                            disabled={removeMutation.isPending}
                          >
                            {t('profile.removeProfile')}
                          </Button>
                        )}
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {consultants?.length === 0 && (
              <tr>
                <td colSpan={3} className="p-3 text-center text-muted-foreground">
                  {t('profile.noConsultants')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
