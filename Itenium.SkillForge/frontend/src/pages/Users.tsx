import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { Pencil, Plus, UserCheck, UserX, History } from 'lucide-react';
import {
  Button,
  Badge,
  Sheet,
  SheetTrigger,
  SheetContent,
  SheetHeader,
  SheetFooter,
  SheetTitle,
  Label,
  Checkbox,
  Input,
} from '@itenium-forge/ui';
import {
  fetchUsers,
  updateUserRoles,
  setUserActive,
  createUser,
  fetchLoginHistory,
  type UserDto,
  type CreateUserRequest,
  type LoginHistoryEntry,
} from '@/api/client';

const ROLES = ['backoffice', 'manager', 'learner'];

// ─── Edit Roles Form ─────────────────────────────────────────────────────────

const rolesSchema = z.object({ roles: z.array(z.string()) });
type RolesFormValues = z.infer<typeof rolesSchema>;

function EditRolesForm({ user, onClose }: { user: UserDto; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { control, handleSubmit } = useForm<RolesFormValues>({
    resolver: zodResolver(rolesSchema),
    defaultValues: { roles: user.roles },
  });

  const mutation = useMutation({
    mutationFn: (data: RolesFormValues) => updateUserRoles(user.id, data.roles),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success(t('users.rolesUpdated'));
      onClose();
    },
  });

  return (
    <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4 mt-4">
      <div className="space-y-2">
        <Label>{t('users.roles')}</Label>
        {ROLES.map((role) => (
          <Controller
            key={role}
            name="roles"
            control={control}
            render={({ field }) => (
              <div className="flex items-center gap-2">
                <Checkbox
                  id={`role-${role}`}
                  checked={field.value.includes(role)}
                  onCheckedChange={(checked) => {
                    if (checked) {
                      field.onChange([...field.value, role]);
                    } else {
                      field.onChange(field.value.filter((r) => r !== role));
                    }
                  }}
                />
                <Label htmlFor={`role-${role}`}>{role}</Label>
              </div>
            )}
          />
        ))}
      </div>
      <SheetFooter className="pt-4">
        <Button type="button" variant="outline" onClick={onClose}>{t('common.cancel')}</Button>
        <Button type="submit" disabled={mutation.isPending}>{t('common.save')}</Button>
      </SheetFooter>
    </form>
  );
}

// ─── Create User Form ─────────────────────────────────────────────────────────

const createUserSchema = z.object({
  username: z.string().min(1),
  email: z.string().email(),
  firstName: z.string().min(1),
  lastName: z.string().min(1),
  password: z.string().min(6),
});
type CreateUserFormValues = z.infer<typeof createUserSchema>;

function CreateUserForm({ onClose }: { onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [serverErrors, setServerErrors] = useState<string[]>([]);

  const { register, handleSubmit, formState: { errors } } = useForm<CreateUserFormValues>({
    resolver: zodResolver(createUserSchema),
  });

  const mutation = useMutation({
    mutationFn: (data: CreateUserRequest) => createUser(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success(t('users.created'));
      onClose();
    },
    onError: (err: unknown) => {
      const data = (err as { response?: { data?: { description?: string }[] } })?.response?.data;
      if (Array.isArray(data)) {
        setServerErrors(data.map((e) => e.description ?? String(e)));
      }
    },
  });

  return (
    <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4 mt-4">
      <div className="space-y-1">
        <Label htmlFor="username">{t('users.username')}</Label>
        <Input id="username" {...register('username')} />
        {errors.username && <p className="text-xs text-destructive">{t('common.required')}</p>}
      </div>
      <div className="space-y-1">
        <Label htmlFor="email">{t('users.email')}</Label>
        <Input id="email" type="email" {...register('email')} />
        {errors.email && <p className="text-xs text-destructive">{t('common.required')}</p>}
      </div>
      <div className="space-y-1">
        <Label htmlFor="firstName">{t('users.firstName')}</Label>
        <Input id="firstName" {...register('firstName')} />
        {errors.firstName && <p className="text-xs text-destructive">{t('common.required')}</p>}
      </div>
      <div className="space-y-1">
        <Label htmlFor="lastName">{t('users.lastName')}</Label>
        <Input id="lastName" {...register('lastName')} />
        {errors.lastName && <p className="text-xs text-destructive">{t('common.required')}</p>}
      </div>
      <div className="space-y-1">
        <Label htmlFor="password">{t('users.password')}</Label>
        <Input id="password" type="password" {...register('password')} />
        {errors.password && <p className="text-xs text-destructive">{t('common.required')}</p>}
      </div>
      {serverErrors.length > 0 && (
        <ul className="text-sm text-destructive space-y-1">
          {serverErrors.map((e, i) => <li key={i}>• {e}</li>)}
        </ul>
      )}
      <SheetFooter className="pt-4">
        <Button type="button" variant="outline" onClick={onClose}>{t('common.cancel')}</Button>
        <Button type="submit" disabled={mutation.isPending}>{t('common.save')}</Button>
      </SheetFooter>
    </form>
  );
}

// ─── Users Page ───────────────────────────────────────────────────────────────

type SheetMode = 'create' | 'editRoles';

export function Users() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [editUser, setEditUser] = useState<UserDto | undefined>(undefined);
  const [sheetMode, setSheetMode] = useState<SheetMode>('create');
  const [sheetOpen, setSheetOpen] = useState(false);
  const [historyUser, setHistoryUser] = useState<UserDto | null>(null);
  const { data: loginHistory = [] } = useQuery<LoginHistoryEntry[]>({
    queryKey: ['login-history', historyUser?.id],
    queryFn: () => fetchLoginHistory(historyUser!.id),
    enabled: !!historyUser,
  });
  const { data: users = [], isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: fetchUsers,
  });

  const activeMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => setUserActive(id, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success(t('users.activeUpdated'));
    },
  });

  const openCreate = () => {
    setSheetMode('create');
    setSheetOpen(true);
  };

  const openEdit = (user: UserDto) => {
    setEditUser(user);
    setSheetMode('editRoles');
    setSheetOpen(true);
  };

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">{t('users.title')}</h1>
        <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
          <SheetTrigger asChild>
            <Button onClick={openCreate}>
              <Plus className="size-4 mr-2" />
              {t('users.addUser')}
            </Button>
          </SheetTrigger>
          <SheetContent>
            <SheetHeader>
              <SheetTitle>
                {sheetMode === 'create' ? t('users.addUser') : t('users.editRoles')}
              </SheetTitle>
            </SheetHeader>
            {sheetMode === 'create' && (
              <CreateUserForm onClose={() => setSheetOpen(false)} />
            )}
            {sheetMode === 'editRoles' && editUser && (
              <EditRolesForm user={editUser} onClose={() => setSheetOpen(false)} />
            )}
          </SheetContent>
        </Sheet>
      </div>

      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('users.name')}</th>
              <th className="p-3 text-left font-medium">{t('users.email')}</th>
              <th className="p-3 text-left font-medium">{t('users.roles')}</th>
              <th className="p-3 text-left font-medium">{t('users.status')}</th>
              <th className="p-3 text-left font-medium">{t('users.lastLogin')}</th>
              <th className="p-3 text-right font-medium">{t('courses.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} className="border-b">
                <td className="p-3 font-medium">{user.firstName} {user.lastName}</td>
                <td className="p-3 text-muted-foreground">{user.email}</td>
                <td className="p-3">
                  <div className="flex flex-wrap gap-1">
                    {user.roles.map((role) => (
                      <Badge key={role} variant="secondary">{role}</Badge>
                    ))}
                    {user.roles.length === 0 && <span className="text-muted-foreground text-sm">—</span>}
                  </div>
                </td>
                <td className="p-3">
                  <Badge variant={user.isActive ? 'default' : 'outline'}>
                    {user.isActive ? t('common.active') : t('users.inactive')}
                  </Badge>
                </td>
                <td className="p-3 text-sm text-muted-foreground">
                  {user.lastLoginAt
                    ? new Date(user.lastLoginAt).toLocaleString()
                    : '—'}
                </td>
                <td className="p-3 text-right">
                  <div className="flex justify-end gap-2">
                    <Button variant="ghost" size="sm" onClick={() => openEdit(user)}>
                      <Pencil className="size-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setHistoryUser(historyUser?.id === user.id ? null : user)}
                    >
                      <History className="size-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => activeMutation.mutate({ id: user.id, isActive: !user.isActive })}
                      disabled={activeMutation.isPending}
                    >
                      {user.isActive
                        ? <UserX className="size-4 text-destructive" />
                        : <UserCheck className="size-4 text-green-600" />}
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
            {users.length === 0 && (
              <tr>
                <td colSpan={6} className="p-3 text-center text-muted-foreground">
                  {t('users.noUsers')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Login history panel */}
      {historyUser && (
        <div className="rounded-lg border p-4 space-y-3">
          <h2 className="font-semibold">
            {t('users.loginHistory')}: {historyUser.firstName} {historyUser.lastName}
          </h2>
          {loginHistory.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('users.noLoginHistory')}</p>
          ) : (
            <ul className="space-y-1">
              {loginHistory.map((entry) => (
                <li key={entry.id} className="flex items-center gap-2 text-sm">
                  <History className="size-3 text-muted-foreground" />
                  <span>{new Date(entry.loggedInAt).toLocaleString()}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
