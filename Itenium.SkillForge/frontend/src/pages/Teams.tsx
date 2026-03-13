import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { Pencil, Trash2, Plus, Users } from 'lucide-react';
import { Link } from '@tanstack/react-router';
import {
  Button,
  Input,
  Sheet,
  SheetTrigger,
  SheetContent,
  SheetHeader,
  SheetFooter,
  SheetTitle,
  Label,
} from '@itenium-forge/ui';
import { fetchUserTeams, createTeam, updateTeam, deleteTeam, type Team } from '@/api/client';

const teamSchema = z.object({
  name: z.string().min(1),
});

type TeamFormValues = z.infer<typeof teamSchema>;

interface TeamFormProps {
  team?: Team;
  onClose: () => void;
}

function TeamForm({ team, onClose }: TeamFormProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<TeamFormValues>({
    resolver: zodResolver(teamSchema),
    defaultValues: { name: team?.name ?? '' },
  });

  const createMutation = useMutation({
    mutationFn: (data: TeamFormValues) => createTeam(data.name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success(t('teams.created'));
      onClose();
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: TeamFormValues) => updateTeam(team!.id, data.name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success(t('teams.updated'));
      onClose();
    },
  });

  const isLoading = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (data: TeamFormValues) => {
    if (team) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate(data);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
      <div className="space-y-1">
        <Label htmlFor="name">{t('teams.name')} *</Label>
        <Input id="name" {...register('name')} />
        {errors.name && <p className="text-sm text-destructive">{t('common.required')}</p>}
      </div>

      <SheetFooter className="pt-4">
        <Button type="button" variant="outline" onClick={onClose}>
          {t('common.cancel')}
        </Button>
        <Button type="submit" disabled={isLoading}>
          {t('common.save')}
        </Button>
      </SheetFooter>
    </form>
  );
}

export function Teams() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [sheetOpen, setSheetOpen] = useState(false);
  const [editTeam, setEditTeam] = useState<Team | undefined>(undefined);
  const [deleteTarget, setDeleteTarget] = useState<Team | null>(null);

  const { data: teams = [], isLoading } = useQuery({
    queryKey: ['teams'],
    queryFn: fetchUserTeams,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteTeam(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      toast.success(t('teams.deleted'));
      setDeleteTarget(null);
    },
  });

  const openCreate = () => {
    setEditTeam(undefined);
    setSheetOpen(true);
  };

  const openEdit = (team: Team) => {
    setEditTeam(team);
    setSheetOpen(true);
  };

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">{t('teams.title')}</h1>
        <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
          <SheetTrigger asChild>
            <Button onClick={openCreate}>
              <Plus className="size-4 mr-2" />
              {t('teams.addTeam')}
            </Button>
          </SheetTrigger>
          <SheetContent>
            <SheetHeader>
              <SheetTitle>{editTeam ? t('teams.editTeam') : t('teams.addTeam')}</SheetTitle>
            </SheetHeader>
            <TeamForm team={editTeam} onClose={() => setSheetOpen(false)} />
          </SheetContent>
        </Sheet>
      </div>

      {/* Delete confirmation banner */}
      {deleteTarget && (
        <div className="flex items-center justify-between rounded-md border border-destructive bg-destructive/10 p-3">
          <span className="text-sm">
            {t('teams.deleteConfirm', { name: deleteTarget.name })}
          </span>
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={() => setDeleteTarget(null)}>
              {t('common.cancel')}
            </Button>
            <Button
              size="sm"
              variant="destructive"
              onClick={() => deleteMutation.mutate(deleteTarget.id)}
              disabled={deleteMutation.isPending}
            >
              {t('common.delete')}
            </Button>
          </div>
        </div>
      )}

      {/* Table */}
      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('teams.name')}</th>
              <th className="p-3 text-right font-medium">{t('courses.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {teams.map((team) => (
              <tr key={team.id} className="border-b">
                <td className="p-3 font-medium">{team.name}</td>
                <td className="p-3 text-right">
                  <div className="flex justify-end gap-2">
                    <Button variant="ghost" size="sm" asChild>
                      <Link to="/admin/teams/$teamId/members" params={{ teamId: String(team.id) }}>
                        <Users className="size-4" />
                      </Link>
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => openEdit(team)}>
                      <Pencil className="size-4" />
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => setDeleteTarget(team)}>
                      <Trash2 className="size-4 text-destructive" />
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
            {teams.length === 0 && (
              <tr>
                <td colSpan={2} className="p-3 text-center text-muted-foreground">
                  {t('teams.noTeams')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
