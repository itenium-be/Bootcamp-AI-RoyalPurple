import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Users, GraduationCap, Briefcase, Component } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle, Avatar, AvatarFallback } from '@itenium-forge/ui';
import { useAuthStore } from '@/stores';
import { fetchUsers, fetchCurrentUser, fetchMyCoaches, fetchUserTeams, type UserDto } from '@/api/client';

// ─── Role badge ──────────────────────────────────────────────────────────────

const roleMeta: Record<string, { label: string; className: string }> = {
  backoffice: { label: 'Admin', className: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200' },
  manager: { label: 'Coach', className: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200' },
  learner: { label: 'Consultant', className: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' },
};

function RoleBadge({ role }: { role: string }) {
  const meta = roleMeta[role] ?? { label: role, className: 'bg-gray-100 text-gray-800' };
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${meta.className}`}>
      {meta.label}
    </span>
  );
}

// ─── User row / card ─────────────────────────────────────────────────────────

function UserRow({ user, teamNames }: { user: UserDto; teamNames: Map<number, string> }) {
  const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  const teamLabels = user.teams.map((id) => teamNames.get(id) ?? `Team ${id}`).join(', ');

  return (
    <div className="flex items-center gap-4 py-3 border-b last:border-0">
      <Avatar className="size-9">
        <AvatarFallback>{initials || user.userName.charAt(0).toUpperCase()}</AvatarFallback>
      </Avatar>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium truncate">
          {user.firstName} {user.lastName}
        </p>
        <p className="text-xs text-muted-foreground truncate">{user.email}</p>
      </div>
      <div className="flex items-center gap-2 shrink-0">
        {teamLabels && <span className="text-xs text-muted-foreground hidden sm:block">{teamLabels}</span>}
        <RoleBadge role={user.role} />
      </div>
    </div>
  );
}

// ─── Admin view: all users table ─────────────────────────────────────────────

function AdminView() {
  const { t } = useTranslation();
  const { data: users = [], isLoading } = useQuery({ queryKey: ['users'], queryFn: fetchUsers });
  const { data: teams = [] } = useQuery({ queryKey: ['teams'], queryFn: fetchUserTeams });
  const teamNames = new Map(teams.map((t) => [t.id, t.name]));

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200">
          <Briefcase className="size-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">{t('users.title', 'Users')}</h1>
          <p className="text-sm text-muted-foreground">
            {isLoading
              ? '…'
              : t('users.adminSubtitle', { count: users.length, defaultValue: `${users.length} total users` })}
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Users className="size-4" />
            {t('users.allUsers', 'All Users')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('common.loading', 'Loading…')}</p>
          ) : users.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('users.empty', 'No users found')}</p>
          ) : (
            users.map((user) => <UserRow key={user.id} user={user} teamNames={teamNames} />)
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// ─── Coach view: teams with their members ────────────────────────────────────

function CoachView() {
  const { t } = useTranslation();
  const { data: users = [], isLoading } = useQuery({ queryKey: ['users'], queryFn: fetchUsers });
  const { data: teams = [] } = useQuery({ queryKey: ['teams'], queryFn: fetchUserTeams });
  const teamNames = new Map(teams.map((t) => [t.id, t.name]));

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
          <Component className="size-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">{t('users.myTeams', 'My Teams')}</h1>
          <p className="text-sm text-muted-foreground">
            {t('users.coachSubtitle', {
              count: users.length,
              defaultValue: `${users.length} members across your teams`,
            })}
          </p>
        </div>
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">{t('common.loading', 'Loading…')}</p>
      ) : (
        teams.map((team) => {
          const members = users.filter((u) => u.teams.includes(team.id));
          return (
            <Card key={team.id}>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-base">
                  <Component className="size-4" />
                  {team.name}
                  <span className="ml-auto text-xs font-normal text-muted-foreground">{members.length} members</span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                {members.length === 0 ? (
                  <p className="text-sm text-muted-foreground py-2">
                    {t('users.noMembers', 'No members in this team')}
                  </p>
                ) : (
                  members.map((user) => <UserRow key={user.id} user={user} teamNames={teamNames} />)
                )}
              </CardContent>
            </Card>
          );
        })
      )}
    </div>
  );
}

// ─── Learner view: own profile + coach ───────────────────────────────────────

function ProfileCard({ user, teamNames, label }: { user: UserDto; teamNames: Map<number, string>; label: string }) {
  const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  const teamLabels = user.teams.map((id) => teamNames.get(id) ?? `Team ${id}`).join(', ');

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm text-muted-foreground font-normal">{label}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="flex items-center gap-4">
          <Avatar className="size-12">
            <AvatarFallback className="text-lg">{initials || user.userName.charAt(0).toUpperCase()}</AvatarFallback>
          </Avatar>
          <div className="flex-1">
            <p className="font-semibold">
              {user.firstName} {user.lastName}
            </p>
            <p className="text-sm text-muted-foreground">{user.email}</p>
            {teamLabels && <p className="text-sm text-muted-foreground mt-1">{teamLabels}</p>}
          </div>
          <RoleBadge role={user.role} />
        </div>
      </CardContent>
    </Card>
  );
}

function LearnerView() {
  const { t } = useTranslation();
  const { data: me, isLoading: meLoading } = useQuery({ queryKey: ['user-me'], queryFn: fetchCurrentUser });
  const { data: coaches = [], isLoading: coachesLoading } = useQuery({
    queryKey: ['coaches'],
    queryFn: fetchMyCoaches,
  });
  const { data: teams = [] } = useQuery({ queryKey: ['teams'], queryFn: fetchUserTeams });
  const teamNames = new Map(teams.map((t) => [t.id, t.name]));

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
          <GraduationCap className="size-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">{t('users.myProfile', 'My Profile')}</h1>
          <p className="text-sm text-muted-foreground">
            {t('users.learnerSubtitle', 'Your profile and assigned coach')}
          </p>
        </div>
      </div>

      {meLoading ? (
        <p className="text-sm text-muted-foreground">{t('common.loading', 'Loading…')}</p>
      ) : me ? (
        <ProfileCard user={me} teamNames={teamNames} label={t('users.you', 'You')} />
      ) : null}

      {coachesLoading ? null : coaches.length > 0 ? (
        <div className="space-y-3">
          <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
            {t('users.yourCoach', 'Your Coach')}
          </h2>
          {coaches.map((coach) => (
            <ProfileCard key={coach.id} user={coach} teamNames={teamNames} label={t('users.coach', 'Coach')} />
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-8 text-center text-sm text-muted-foreground">
            {t('users.noCoach', 'No coach assigned yet')}
          </CardContent>
        </Card>
      )}
    </div>
  );
}

// ─── Main export ─────────────────────────────────────────────────────────────

export function UsersPage() {
  const { user } = useAuthStore();

  if (user?.role === 'backoffice') return <AdminView />;
  if (user?.role === 'manager') return <CoachView />;
  return <LearnerView />;
}
