import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod/v4';
import { Users, GraduationCap, Briefcase, Component, Plus, MoreHorizontal, Pencil, UserCheck } from 'lucide-react';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Avatar,
  AvatarFallback,
  Button,
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
  Form,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
  Input,
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  Checkbox,
} from '@itenium-forge/ui';
import { toast } from 'sonner';
import { useAuthStore } from '@/stores';
import {
  fetchUsers,
  fetchCurrentUser,
  fetchMyCoaches,
  fetchUserTeams,
  createUser,
  updateUserRole,
  updateUserTeams,
  type UserDto,
  type CreateUserRequest,
} from '@/api/client';

// ─── Types ────────────────────────────────────────────────────────────────────

interface Team {
  id: number;
  name: string;
}

// ─── Role badge ───────────────────────────────────────────────────────────────

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

// ─── Create user sheet ────────────────────────────────────────────────────────

const createUserSchema = z.object({
  firstName: z.string().min(1, 'Required'),
  lastName: z.string().min(1, 'Required'),
  userName: z.string().min(1, 'Required'),
  email: z.string().email('Invalid email'),
  password: z.string().min(8, 'Min 8 characters'),
  role: z.enum(['learner', 'manager', 'backoffice']),
  teams: z.array(z.number()),
});

type CreateUserFormValues = z.infer<typeof createUserSchema>;

function CreateUserSheet({
  open,
  onOpenChange,
  defaultRole = 'learner',
  teams,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  defaultRole?: 'learner' | 'manager' | 'backoffice';
  teams: Team[];
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const form = useForm<CreateUserFormValues>({
    resolver: zodResolver(createUserSchema),
    defaultValues: { firstName: '', lastName: '', userName: '', email: '', password: '', role: defaultRole, teams: [] },
  });

  const mutation = useMutation({
    mutationFn: (values: CreateUserFormValues) => createUser(values as CreateUserRequest),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success(t('users.created', 'User created'));
      form.reset();
      onOpenChange(false);
    },
    onError: () => toast.error(t('users.createError', 'Failed to create user')),
  });

  const selectedTeams = form.watch('teams');
  const selectedRole = form.watch('role');

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-[420px] overflow-y-auto pl-4">
        <SheetHeader>
          <SheetTitle>{t('users.createTitle', 'Create User')}</SheetTitle>
          <SheetDescription>{t('users.createDesc', 'Add a new user to the platform.')}</SheetDescription>
        </SheetHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit((v) => mutation.mutate(v))} className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-3">
              <FormField
                control={form.control}
                name="firstName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('users.firstName', 'First name')}</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="lastName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('users.lastName', 'Last name')}</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="userName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('users.username', 'Username')}</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('users.email', 'Email')}</FormLabel>
                  <FormControl>
                    <Input type="email" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('users.password', 'Password')}</FormLabel>
                  <FormControl>
                    <Input type="password" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="role"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('users.role', 'Role')}</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="learner">Consultant</SelectItem>
                      <SelectItem value="manager">Coach</SelectItem>
                      <SelectItem value="backoffice">Admin</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {(selectedRole === 'manager' || selectedRole === 'learner') && teams.length > 0 && (
              <FormField
                control={form.control}
                name="teams"
                render={() => (
                  <FormItem>
                    <FormLabel>{t('users.teams', 'Teams')}</FormLabel>
                    <div className="space-y-2">
                      {teams.map((team) => (
                        <div key={team.id} className="flex items-center gap-2">
                          <Checkbox
                            id={`team-${team.id}`}
                            checked={selectedTeams.includes(team.id)}
                            onCheckedChange={(checked) => {
                              const current = form.getValues('teams');
                              form.setValue(
                                'teams',
                                checked ? [...current, team.id] : current.filter((id) => id !== team.id),
                              );
                            }}
                          />
                          <label htmlFor={`team-${team.id}`} className="text-sm">
                            {team.name}
                          </label>
                        </div>
                      ))}
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}

            <SheetFooter className="pt-4">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t('common.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('common.saving', 'Saving…') : t('common.create', 'Create')}
              </Button>
            </SheetFooter>
          </form>
        </Form>
      </SheetContent>
    </Sheet>
  );
}

// ─── Edit user sheet (role + teams) ──────────────────────────────────────────

const editUserSchema = z.object({
  role: z.enum(['learner', 'manager', 'backoffice']),
  teams: z.array(z.number()),
});

type EditUserFormValues = z.infer<typeof editUserSchema>;

function EditUserSheet({
  user,
  teams,
  onOpenChange,
}: {
  user: UserDto;
  teams: Team[];
  onOpenChange: (v: boolean) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const form = useForm<EditUserFormValues>({
    resolver: zodResolver(editUserSchema),
    defaultValues: {
      role: (user.role as 'learner' | 'manager' | 'backoffice') ?? 'learner',
      teams: user.teams,
    },
  });

  const roleMutation = useMutation({
    mutationFn: ({ role }: { role: string }) => updateUserRole(user.id, role),
  });
  const teamsMutation = useMutation({
    mutationFn: ({ teamIds }: { teamIds: number[] }) => updateUserTeams(user.id, teamIds),
  });

  const onSubmit = async (values: EditUserFormValues) => {
    try {
      await Promise.all([
        roleMutation.mutateAsync({ role: values.role }),
        teamsMutation.mutateAsync({ teamIds: values.teams }),
      ]);
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success(t('users.updated', 'User updated'));
      onOpenChange(false);
    } catch {
      toast.error(t('users.updateError', 'Failed to update user'));
    }
  };

  const selectedTeams = form.watch('teams');
  const selectedRole = form.watch('role');
  const isPending = roleMutation.isPending || teamsMutation.isPending;

  return (
    <Sheet open onOpenChange={onOpenChange}>
      <SheetContent className="w-[420px] overflow-y-auto pl-4">
        <SheetHeader>
          <SheetTitle>
            {user.firstName} {user.lastName}
          </SheetTitle>
          <SheetDescription>{user.email}</SheetDescription>
        </SheetHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 py-4">
            <FormField
              control={form.control}
              name="role"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('users.role', 'Role')}</FormLabel>
                  <Select onValueChange={field.onChange} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="learner">Consultant</SelectItem>
                      <SelectItem value="manager">Coach</SelectItem>
                      <SelectItem value="backoffice">Admin</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {(selectedRole === 'manager' || selectedRole === 'learner') && teams.length > 0 && (
              <FormField
                control={form.control}
                name="teams"
                render={() => (
                  <FormItem>
                    <FormLabel>{t('users.teams', 'Teams')}</FormLabel>
                    <div className="space-y-2">
                      {teams.map((team) => (
                        <div key={team.id} className="flex items-center gap-2">
                          <Checkbox
                            id={`edit-team-${team.id}`}
                            checked={selectedTeams.includes(team.id)}
                            onCheckedChange={(checked) => {
                              const current = form.getValues('teams');
                              form.setValue(
                                'teams',
                                checked ? [...current, team.id] : current.filter((id) => id !== team.id),
                              );
                            }}
                          />
                          <label htmlFor={`edit-team-${team.id}`} className="text-sm">
                            {team.name}
                          </label>
                        </div>
                      ))}
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}

            <SheetFooter className="pt-4">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t('common.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending ? t('common.saving', 'Saving…') : t('common.save', 'Save')}
              </Button>
            </SheetFooter>
          </form>
        </Form>
      </SheetContent>
    </Sheet>
  );
}

// ─── User row with actions ────────────────────────────────────────────────────

function UserRow({
  user,
  teamNames,
  teams,
  showActions,
}: {
  user: UserDto;
  teamNames: Map<number, string>;
  teams: Team[];
  showActions: boolean;
}) {
  const [editOpen, setEditOpen] = useState(false);
  const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  const teamLabels = user.teams.map((id) => teamNames.get(id) ?? `Team ${id}`).join(', ');

  return (
    <>
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
          {showActions && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="size-8 p-0">
                  <MoreHorizontal className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel>Actions</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => setEditOpen(true)}>
                  <Pencil className="size-4 mr-2" />
                  Edit role & teams
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>

      {editOpen && <EditUserSheet user={user} teams={teams} onOpenChange={setEditOpen} />}
    </>
  );
}

// ─── Admin view ───────────────────────────────────────────────────────────────

function AdminView() {
  const { t } = useTranslation();
  const [createOpen, setCreateOpen] = useState(false);
  const [createRole, setCreateRole] = useState<'learner' | 'manager'>('learner');

  const { data: users = [], isLoading } = useQuery({ queryKey: ['users'], queryFn: fetchUsers });
  const { data: teams = [] } = useQuery({ queryKey: ['teams'], queryFn: fetchUserTeams });
  const teamNames = new Map(teams.map((t) => [t.id, t.name]));

  const coaches = users.filter((u) => u.role === 'manager');
  const consultants = users.filter((u) => u.role === 'learner');

  const openCreate = (role: 'learner' | 'manager') => {
    setCreateRole(role);
    setCreateOpen(true);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200">
            <Briefcase className="size-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold">{t('users.title', 'Users')}</h1>
            <p className="text-sm text-muted-foreground">{isLoading ? '…' : `${users.length} total users`}</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => openCreate('manager')}>
            <UserCheck className="size-4 mr-2" />
            {t('users.addCoach', 'Add Coach')}
          </Button>
          <Button onClick={() => openCreate('learner')}>
            <Plus className="size-4 mr-2" />
            {t('users.addUser', 'Add User')}
          </Button>
        </div>
      </div>

      {/* Coaches */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <UserCheck className="size-4" />
            {t('users.coaches', 'Coaches')}
            <span className="ml-auto text-xs font-normal text-muted-foreground">{coaches.length}</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('common.loading', 'Loading…')}</p>
          ) : coaches.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('users.noCoaches', 'No coaches yet')}</p>
          ) : (
            coaches.map((user) => <UserRow key={user.id} user={user} teamNames={teamNames} teams={teams} showActions />)
          )}
        </CardContent>
      </Card>

      {/* Consultants */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Users className="size-4" />
            {t('users.consultants', 'Consultants')}
            <span className="ml-auto text-xs font-normal text-muted-foreground">{consultants.length}</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('common.loading', 'Loading…')}</p>
          ) : consultants.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">
              {t('users.noConsultants', 'No consultants yet')}
            </p>
          ) : (
            consultants.map((user) => (
              <UserRow key={user.id} user={user} teamNames={teamNames} teams={teams} showActions />
            ))
          )}
        </CardContent>
      </Card>

      <CreateUserSheet open={createOpen} onOpenChange={setCreateOpen} defaultRole={createRole} teams={teams} />
    </div>
  );
}

// ─── Coach view ───────────────────────────────────────────────────────────────

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
          <p className="text-sm text-muted-foreground">{`${users.length} members across your teams`}</p>
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
                  members.map((user) => (
                    <UserRow key={user.id} user={user} teamNames={teamNames} teams={[]} showActions={false} />
                  ))
                )}
              </CardContent>
            </Card>
          );
        })
      )}
    </div>
  );
}

// ─── Learner view ─────────────────────────────────────────────────────────────

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

// ─── Main export ──────────────────────────────────────────────────────────────

export function UsersPage() {
  const { user } = useAuthStore();

  if (user?.role === 'backoffice') return <AdminView />;
  if (user?.role === 'manager') return <CoachView />;
  return <LearnerView />;
}
