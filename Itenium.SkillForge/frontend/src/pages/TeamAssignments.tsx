import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ClipboardList, BookOpen, CheckCircle2, AlertCircle } from 'lucide-react';
import { Badge, Button } from '@itenium-forge/ui';
import { toast } from 'sonner';
import {
  fetchTeamAssignments,
  fetchCourses,
  fetchTeamMembers,
  assignCourse,
  unassignCourse,
  updateAssignment,
} from '@/api/client';
import { useTeamStore } from '@/stores';

export function TeamAssignments() {
  const { t } = useTranslation();
  const { selectedTeam } = useTeamStore();
  const queryClient = useQueryClient();

  // courseId → userId selected for individual assignment
  const [memberPicker, setMemberPicker] = useState<Record<number, string>>({});

  const { data: assignments = [], isLoading: loadingAssignments } = useQuery({
    queryKey: ['team-assignments', selectedTeam?.id],
    queryFn: () => fetchTeamAssignments(selectedTeam?.id ?? 0),
    enabled: !!selectedTeam,
  });

  const { data: allCourses = [], isLoading: loadingCourses } = useQuery({
    queryKey: ['courses'],
    queryFn: fetchCourses,
  });

  const { data: members = [] } = useQuery({
    queryKey: ['team-members', selectedTeam?.id],
    queryFn: () => fetchTeamMembers(selectedTeam?.id ?? 0),
    enabled: !!selectedTeam,
  });

  const teamWideAssignedIds = new Set(
    assignments.filter((a) => a.userId === null).map((a) => a.courseId),
  );
  const availableCourses = allCourses.filter(
    (c) => c.status === 'Published' && !teamWideAssignedIds.has(c.id),
  );

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ['team-assignments', selectedTeam?.id] });

  const assignMutation = useMutation({
    mutationFn: ({ courseId, isMandatory, userId }: { courseId: number; isMandatory: boolean; userId?: string }) =>
      assignCourse(selectedTeam?.id ?? 0, courseId, isMandatory, userId),
    onSuccess: () => {
      toast.success(t('assignments.assigned'));
      void invalidate();
    },
  });

  const unassignMutation = useMutation({
    mutationFn: ({ courseId, userId }: { courseId: number; userId?: string }) =>
      unassignCourse(selectedTeam?.id ?? 0, courseId, userId ?? undefined),
    onSuccess: () => {
      toast.success(t('assignments.unassigned'));
      void invalidate();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ courseId, isMandatory, userId }: { courseId: number; isMandatory: boolean; userId?: string }) =>
      updateAssignment(selectedTeam?.id ?? 0, courseId, isMandatory, userId),
    onSuccess: () => {
      toast.success(t('assignments.updated'));
      void invalidate();
    },
  });

  if (loadingAssignments || loadingCourses) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <ClipboardList className="size-6 text-muted-foreground" />
        <h1 className="text-3xl font-bold">
          {selectedTeam ? `${selectedTeam.name} — ${t('assignments.title')}` : t('assignments.title')}
        </h1>
      </div>

      {/* Assigned courses */}
      <section className="space-y-3">
        <h2 className="text-lg font-semibold">{t('assignments.assignedCourses')}</h2>
        {assignments.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <BookOpen className="size-10 mx-auto mb-2 opacity-30" />
            <p>{t('assignments.noAssignments')}</p>
          </div>
        ) : (
          <div className="rounded-md border divide-y">
            {assignments.map((a) => (
              <div key={`${a.courseId}-${a.userId ?? 'team'}`} className="flex items-center justify-between p-3">
                <div className="flex items-center gap-3">
                  {a.isMandatory ? (
                    <AlertCircle className="size-4 text-destructive" />
                  ) : (
                    <CheckCircle2 className="size-4 text-muted-foreground" />
                  )}
                  <div>
                    <span className="font-medium">{a.courseName}</span>
                    <span className="ml-2 text-xs text-muted-foreground">
                      {a.userFullName ?? t('assignments.entireTeam')}
                    </span>
                  </div>
                  <Badge variant={a.isMandatory ? 'destructive' : 'secondary'}>
                    {a.isMandatory ? t('assignments.mandatory') : t('assignments.optional')}
                  </Badge>
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() =>
                      updateMutation.mutate({
                        courseId: a.courseId,
                        isMandatory: !a.isMandatory,
                        userId: a.userId ?? undefined,
                      })
                    }
                  >
                    {a.isMandatory ? t('assignments.assignOptional') : t('assignments.assignMandatory')}
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => unassignMutation.mutate({ courseId: a.courseId, userId: a.userId ?? undefined })}
                  >
                    {t('assignments.unassign')}
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Available courses to assign */}
      {availableCourses.length > 0 && (
        <section className="space-y-3">
          <h2 className="text-lg font-semibold">{t('assignments.availableCourses')}</h2>
          <div className="rounded-md border divide-y">
            {availableCourses.map((c) => (
              <div key={c.id} className="space-y-2 p-3">
                <div className="flex items-center justify-between">
                  <span className="font-medium">{c.name}</span>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => assignMutation.mutate({ courseId: c.id, isMandatory: true })}
                    >
                      {t('assignments.assignMandatory')}
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => assignMutation.mutate({ courseId: c.id, isMandatory: false })}
                    >
                      {t('assignments.assignOptional')}
                    </Button>
                  </div>
                </div>

                {/* Individual member assignment */}
                {members.length > 0 && (
                  <div className="flex items-center gap-2 pl-1">
                    <select
                      className="text-sm border rounded px-2 py-1 bg-background"
                      value={memberPicker[c.id] ?? ''}
                      onChange={(e) => setMemberPicker((prev) => ({ ...prev, [c.id]: e.target.value }))}
                      aria-label={t('assignments.selectMember')}
                    >
                      <option value="">{t('assignments.selectMember')}</option>
                      {members.map((m) => (
                        <option key={m.id} value={m.id}>
                          {m.firstName} {m.lastName}
                        </option>
                      ))}
                    </select>
                    {memberPicker[c.id] && (
                      <>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() =>
                            assignMutation.mutate({ courseId: c.id, isMandatory: true, userId: memberPicker[c.id] })
                          }
                        >
                          {t('assignments.assignMandatory')}
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() =>
                            assignMutation.mutate({ courseId: c.id, isMandatory: false, userId: memberPicker[c.id] })
                          }
                        >
                          {t('assignments.assignOptional')}
                        </Button>
                      </>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
