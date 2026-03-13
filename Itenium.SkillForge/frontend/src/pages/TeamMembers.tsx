import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ArrowLeft, UserMinus, UserPlus, Users } from 'lucide-react';
import { Link } from '@tanstack/react-router';
import { Button, Badge } from '@itenium-forge/ui';
import {
  fetchTeamMembers,
  addTeamMember,
  removeTeamMember,
  fetchUsers,
  fetchUserTeams,
  type UserDto,
} from '@/api/client';

interface TeamMembersProps {
  teamId: number;
  teamName?: string;
  hideManage?: boolean;
}

export function TeamMembers({ teamId, teamName, hideManage = false }: TeamMembersProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [removeTarget, setRemoveTarget] = useState<UserDto | null>(null);
  const [addUserId, setAddUserId] = useState('');

  const { data: members = [], isLoading } = useQuery({
    queryKey: ['team-members', teamId],
    queryFn: () => fetchTeamMembers(teamId),
  });

  const { data: allUsers = [] } = useQuery({
    queryKey: ['users'],
    queryFn: fetchUsers,
  });

  const { data: teams = [] } = useQuery({
    queryKey: ['teams'],
    queryFn: fetchUserTeams,
  });

  const resolvedTeamName = teamName ?? teams.find((t) => t.id === teamId)?.name;

  const memberIds = new Set(members.map((m) => m.id));
  const nonMembers = allUsers.filter((u) => !memberIds.has(u.id));

  const addMutation = useMutation({
    mutationFn: (userId: string) => addTeamMember(teamId, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['team-members', teamId] });
      toast.success(t('teams.memberAdded'));
      setAddUserId('');
    },
  });

  const removeMutation = useMutation({
    mutationFn: (userId: string) => removeTeamMember(teamId, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['team-members', teamId] });
      toast.success(t('teams.memberRemoved'));
      setRemoveTarget(null);
    },
  });

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        {!hideManage && (
          <Button variant="ghost" size="sm" asChild>
            <Link to="/admin/teams">
              <ArrowLeft className="size-4" />
            </Link>
          </Button>
        )}
        <h1 className="text-3xl font-bold">
          {resolvedTeamName ? `${resolvedTeamName} — ${t('teams.members')}` : t('teams.members')}
        </h1>
      </div>

      {/* Remove confirmation */}
      {removeTarget && (
        <div className="flex items-center justify-between rounded-md border border-destructive bg-destructive/10 p-3">
          <span className="text-sm">
            {t('teams.removeMember')}: {removeTarget.firstName} {removeTarget.lastName}?
          </span>
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={() => setRemoveTarget(null)}>
              {t('common.cancel')}
            </Button>
            <Button
              size="sm"
              variant="destructive"
              onClick={() => removeMutation.mutate(removeTarget.id)}
              disabled={removeMutation.isPending}
            >
              {t('teams.removeMember')}
            </Button>
          </div>
        </div>
      )}

      {/* Add member */}
      {!hideManage && nonMembers.length > 0 && (
        <div className="rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">{t('teams.addMember')}</h2>
          <div className="flex gap-2">
            <select
              className="flex-1 rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
              value={addUserId}
              onChange={(e) => setAddUserId(e.target.value)}
            >
              <option value="">{t('teams.selectUser')}</option>
              {nonMembers.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.firstName} {u.lastName} ({u.username})
                </option>
              ))}
            </select>
            <Button
              onClick={() => addUserId && addMutation.mutate(addUserId)}
              disabled={!addUserId || addMutation.isPending}
            >
              <UserPlus className="size-4 mr-2" />
              {t('teams.addMember')}
            </Button>
          </div>
        </div>
      )}

      {/* Members list */}
      {members.length === 0 ? (
        <div className="text-center py-12 text-muted-foreground">
          <Users className="size-12 mx-auto mb-3 opacity-30" />
          <p>{t('teams.noMembers')}</p>
        </div>
      ) : (
        <div className="rounded-md border">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="p-3 text-left font-medium">{t('users.name')}</th>
                <th className="p-3 text-left font-medium">{t('users.email')}</th>
                <th className="p-3 text-left font-medium">{t('users.roles')}</th>
                {!hideManage && <th className="p-3 text-right font-medium">{t('courses.actions')}</th>}
              </tr>
            </thead>
            <tbody>
              {members.map((member) => (
                <tr key={member.id} className="border-b">
                  <td className="p-3 font-medium">{member.firstName} {member.lastName}</td>
                  <td className="p-3 text-muted-foreground">{member.email}</td>
                  <td className="p-3">
                    <div className="flex flex-wrap gap-1">
                      {member.roles.map((role) => (
                        <Badge key={role} variant="secondary">{role}</Badge>
                      ))}
                    </div>
                  </td>
                  {!hideManage && (
                    <td className="p-3 text-right">
                      <Button variant="ghost" size="sm" onClick={() => setRemoveTarget(member)}>
                        <UserMinus className="size-4 text-destructive" />
                      </Button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
