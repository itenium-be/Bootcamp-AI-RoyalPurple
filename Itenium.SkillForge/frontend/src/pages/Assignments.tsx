import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod/v4';
import { toast } from 'sonner';
import { Plus, Trash2 } from 'lucide-react';
import {
  Button,
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
  Label,
  FormItem,
  FormMessage,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@itenium-forge/ui';
import {
  fetchAssignments,
  assignCourse,
  removeAssignment,
  fetchCourses,
  fetchUserTeams,
  fetchConsultants,
  type CourseAssignment,
} from '@/api/client';

const assignSchema = z.object({
  courseId: z.number().int().positive(),
  teamId: z.number().int().positive(),
  userId: z.string().nullable().optional(),
  isRequired: z.boolean(),
});

type AssignFormValues = z.infer<typeof assignSchema>;

export function Assignments() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [sheetOpen, setSheetOpen] = useState(false);

  const { data: assignments, isLoading } = useQuery({
    queryKey: ['assignments'],
    queryFn: fetchAssignments,
  });

  const { data: courses } = useQuery({
    queryKey: ['courses'],
    queryFn: () => fetchCourses(),
  });

  const { data: teams } = useQuery({
    queryKey: ['teams'],
    queryFn: fetchUserTeams,
  });

  const { data: consultants } = useQuery({
    queryKey: ['consultants'],
    queryFn: fetchConsultants,
  });

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<AssignFormValues>({
    resolver: zodResolver(assignSchema),
    defaultValues: { courseId: 0, teamId: 0, userId: null, isRequired: true },
  });

  const selectedTeamId = watch('teamId');
  const teamMembers = consultants?.filter((c) => c.teamId === selectedTeamId) ?? [];

  const createMutation = useMutation({
    mutationFn: assignCourse,
    onSuccess: () => {
      toast.success(t('assignments.created'));
      queryClient.invalidateQueries({ queryKey: ['assignments'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
      setSheetOpen(false);
      reset();
    },
    onError: () => toast.error(t('assignments.createError')),
  });

  const deleteMutation = useMutation({
    mutationFn: removeAssignment,
    onSuccess: () => {
      toast.success(t('assignments.deleted'));
      queryClient.invalidateQueries({ queryKey: ['assignments'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-stats'] });
    },
    onError: () => toast.error(t('assignments.deleteError')),
  });

  const onSubmit = (values: AssignFormValues) => {
    createMutation.mutate({
      courseId: values.courseId,
      teamId: values.teamId,
      userId: values.userId || null,
      isRequired: values.isRequired,
    });
  };

  const handleDelete = (assignment: CourseAssignment) => {
    if (!window.confirm(t('assignments.confirmDelete', { course: assignment.courseName }))) return;
    deleteMutation.mutate(assignment.id);
  };

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">{t('nav.assignments')}</h1>
        <Button onClick={() => setSheetOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          {t('assignments.assign')}
        </Button>
      </div>

      <div className="rounded-md border">
        <table className="w-full">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="p-3 text-left font-medium">{t('assignments.course')}</th>
              <th className="p-3 text-left font-medium">{t('assignments.target')}</th>
              <th className="p-3 text-left font-medium">{t('assignments.type')}</th>
              <th className="p-3 text-left font-medium">{t('common.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {assignments?.map((a) => (
              <tr key={a.id} className="border-b">
                <td className="p-3 font-medium">{a.courseName}</td>
                <td className="p-3 text-muted-foreground">
                  {a.userId
                    ? (() => {
                        const c = consultants?.find((c) => c.userId === a.userId);
                        const name = c ? `${c.firstName} ${c.lastName}` : a.userId;
                        return `${t('assignments.member')}: ${name}`;
                      })()
                    : `${t('assignments.team')}: ${teams?.find((t) => t.id === a.teamId)?.name ?? a.teamId}`}
                </td>
                <td className="p-3">
                  <span
                    className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                      a.isRequired ? 'bg-red-100 text-red-700' : 'bg-blue-100 text-blue-700'
                    }`}
                  >
                    {a.isRequired ? t('assignments.mandatory') : t('assignments.optional')}
                  </span>
                </td>
                <td className="p-3">
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => handleDelete(a)}
                    disabled={deleteMutation.isPending}
                  >
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                </td>
              </tr>
            ))}
            {assignments?.length === 0 && (
              <tr>
                <td colSpan={4} className="p-3 text-center text-muted-foreground">
                  {t('assignments.noAssignments')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
        <SheetContent>
          <SheetHeader>
            <SheetTitle>{t('assignments.assign')}</SheetTitle>
            <SheetDescription>{t('assignments.assignDesc')}</SheetDescription>
          </SheetHeader>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-4">
            <FormItem>
              <Label>{t('assignments.course')}</Label>
              <Controller
                control={control}
                name="courseId"
                render={({ field }) => (
                  <Select value={String(field.value || '')} onValueChange={(v) => field.onChange(Number(v))}>
                    <SelectTrigger>
                      <SelectValue placeholder={t('assignments.selectCourse')} />
                    </SelectTrigger>
                    <SelectContent>
                      {courses?.map((c) => (
                        <SelectItem key={c.id} value={String(c.id)}>
                          {c.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              {errors.courseId && <FormMessage>{errors.courseId.message}</FormMessage>}
            </FormItem>

            <FormItem>
              <Label>{t('assignments.team')}</Label>
              <Controller
                control={control}
                name="teamId"
                render={({ field }) => (
                  <Select value={String(field.value || '')} onValueChange={(v) => field.onChange(Number(v))}>
                    <SelectTrigger>
                      <SelectValue placeholder={t('assignments.selectTeam')} />
                    </SelectTrigger>
                    <SelectContent>
                      {teams?.map((team) => (
                        <SelectItem key={team.id} value={String(team.id)}>
                          {team.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              {errors.teamId && <FormMessage>{errors.teamId.message}</FormMessage>}
            </FormItem>

            {selectedTeamId > 0 && teamMembers.length > 0 && (
              <FormItem>
                <Label>{t('assignments.member')}</Label>
                <Controller
                  control={control}
                  name="userId"
                  render={({ field }) => (
                    <Select
                      value={field.value ?? '__all__'}
                      onValueChange={(v) => field.onChange(v === '__all__' ? null : v)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="__all__">{t('assignments.wholeTeam')}</SelectItem>
                        {teamMembers.map((c) => (
                          <SelectItem key={c.userId} value={c.userId}>
                            {c.firstName} {c.lastName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </FormItem>
            )}

            <FormItem>
              <Label>{t('assignments.type')}</Label>
              <Controller
                control={control}
                name="isRequired"
                render={({ field }) => (
                  <Select
                    value={field.value ? 'true' : 'false'}
                    onValueChange={(v) => field.onChange(v === 'true')}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="true">{t('assignments.mandatory')}</SelectItem>
                      <SelectItem value="false">{t('assignments.optional')}</SelectItem>
                    </SelectContent>
                  </Select>
                )}
              />
            </FormItem>

            <SheetFooter>
              <Button type="button" variant="outline" onClick={() => setSheetOpen(false)}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending ? t('common.saving') : t('common.create')}
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>
    </div>
  );
}
