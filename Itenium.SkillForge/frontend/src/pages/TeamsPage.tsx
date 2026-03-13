import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod/v4';
import { Component, Plus, MoreHorizontal, Pencil, Trash2, Briefcase } from 'lucide-react';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
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
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
} from '@itenium-forge/ui';
import { toast } from 'sonner';
import { useAuthStore } from '@/stores';
import { fetchUserTeams, createTeam, updateTeam, deleteTeam } from '@/api/client';

// ─── Types ────────────────────────────────────────────────────────────────────

interface Team {
  id: number;
  name: string;
}

// ─── Team name schema ─────────────────────────────────────────────────────────

const teamSchema = z.object({
  name: z.string().min(1, 'Required'),
});

type TeamFormValues = z.infer<typeof teamSchema>;

// ─── Create team sheet ────────────────────────────────────────────────────────

function CreateTeamSheet({ open, onOpenChange }: { open: boolean; onOpenChange: (v: boolean) => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const form = useForm<TeamFormValues>({
    resolver: zodResolver(teamSchema),
    defaultValues: { name: '' },
  });

  const mutation = useMutation({
    mutationFn: (values: TeamFormValues) => createTeam(values.name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success(t('teams.created', 'Team created'));
      form.reset();
      onOpenChange(false);
    },
    onError: () => {
      toast.error(t('teams.createError', 'Failed to create team'));
    },
  });

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-[420px] pl-4">
        <SheetHeader>
          <SheetTitle>{t('teams.createTitle', 'Create Team')}</SheetTitle>
          <SheetDescription>{t('teams.createDesc', 'Add a new competence centre team.')}</SheetDescription>
        </SheetHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit((v) => mutation.mutate(v))} className="space-y-4 py-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('teams.name', 'Name')}</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

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

// ─── Rename team sheet ────────────────────────────────────────────────────────

function RenameTeamSheet({ team, onOpenChange }: { team: Team; onOpenChange: (v: boolean) => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const form = useForm<TeamFormValues>({
    resolver: zodResolver(teamSchema),
    defaultValues: { name: team.name },
  });

  const mutation = useMutation({
    mutationFn: (values: TeamFormValues) => updateTeam(team.id, values.name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success(t('teams.updated', 'Team updated'));
      onOpenChange(false);
    },
    onError: () => {
      toast.error(t('teams.updateError', 'Failed to update team'));
    },
  });

  return (
    <Sheet open onOpenChange={onOpenChange}>
      <SheetContent className="w-[420px] pl-4">
        <SheetHeader>
          <SheetTitle>{t('teams.renameTitle', 'Rename Team')}</SheetTitle>
          <SheetDescription>{team.name}</SheetDescription>
        </SheetHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit((v) => mutation.mutate(v))} className="space-y-4 py-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('teams.name', 'Name')}</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <SheetFooter className="pt-4">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t('common.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('common.saving', 'Saving…') : t('common.save', 'Save')}
              </Button>
            </SheetFooter>
          </form>
        </Form>
      </SheetContent>
    </Sheet>
  );
}

// ─── Team row ─────────────────────────────────────────────────────────────────

function TeamRow({ team, canDelete }: { team: Team; canDelete: boolean }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [renameOpen, setRenameOpen] = useState(false);

  const deleteMutation = useMutation({
    mutationFn: () => deleteTeam(team.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success(t('teams.deleted', 'Team deleted'));
    },
    onError: () => {
      toast.error(t('teams.deleteError', 'Failed to delete team'));
    },
  });

  return (
    <>
      <div className="flex items-center gap-4 py-3 border-b last:border-0">
        <div className="flex size-8 items-center justify-center rounded-lg bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200 shrink-0">
          <Component className="size-4" />
        </div>
        <p className="flex-1 text-sm font-medium">{team.name}</p>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="sm" className="size-8 p-0">
              <MoreHorizontal className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuLabel>{t('common.actions', 'Actions')}</DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={() => setRenameOpen(true)}>
              <Pencil className="size-4 mr-2" />
              {t('teams.rename', 'Rename')}
            </DropdownMenuItem>
            {canDelete && (
              <DropdownMenuItem
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
                className="text-destructive focus:text-destructive"
              >
                <Trash2 className="size-4 mr-2" />
                {t('common.delete', 'Delete')}
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {renameOpen && <RenameTeamSheet team={team} onOpenChange={setRenameOpen} />}
    </>
  );
}

// ─── Admin view ───────────────────────────────────────────────────────────────

function AdminView() {
  const { t } = useTranslation();
  const [createOpen, setCreateOpen] = useState(false);

  const { data: teams = [], isLoading } = useQuery({ queryKey: ['teams'], queryFn: fetchUserTeams });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200">
            <Briefcase className="size-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold">{t('teams.title', 'Teams')}</h1>
            <p className="text-sm text-muted-foreground">{isLoading ? '…' : `${teams.length} teams`}</p>
          </div>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="size-4 mr-2" />
          {t('teams.addTeam', 'Add Team')}
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Component className="size-4" />
            {t('teams.allTeams', 'All Teams')}
            <span className="ml-auto text-xs font-normal text-muted-foreground">{teams.length}</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('common.loading', 'Loading…')}</p>
          ) : teams.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('teams.noTeams', 'No teams yet')}</p>
          ) : (
            teams.map((team) => <TeamRow key={team.id} team={team} canDelete />)
          )}
        </CardContent>
      </Card>

      <CreateTeamSheet open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}

// ─── Manager view ─────────────────────────────────────────────────────────────

function ManagerView() {
  const { t } = useTranslation();

  const { data: teams = [], isLoading } = useQuery({ queryKey: ['teams'], queryFn: fetchUserTeams });

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
          <Component className="size-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">{t('teams.myTeams', 'My Teams')}</h1>
          <p className="text-sm text-muted-foreground">{isLoading ? '…' : `${teams.length} teams`}</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Component className="size-4" />
            {t('teams.myTeams', 'My Teams')}
            <span className="ml-auto text-xs font-normal text-muted-foreground">{teams.length}</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('common.loading', 'Loading…')}</p>
          ) : teams.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{t('teams.noTeams', 'No teams yet')}</p>
          ) : (
            teams.map((team) => <TeamRow key={team.id} team={team} canDelete={false} />)
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// ─── Main export ──────────────────────────────────────────────────────────────

export function TeamsPage() {
  const { user } = useAuthStore();

  if (user?.role === 'backoffice') return <AdminView />;
  if (user?.role === 'manager') return <ManagerView />;
  return null;
}
