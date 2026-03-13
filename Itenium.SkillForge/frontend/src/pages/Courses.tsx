import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { Pencil, Trash2, Plus } from 'lucide-react';
import {
  Button,
  Input,
  Badge,
  Sheet,
  SheetTrigger,
  SheetContent,
  SheetHeader,
  SheetFooter,
  SheetTitle,
  Select,
  SelectTrigger,
  SelectContent,
  SelectItem,
  SelectValue,
  Label,
  Checkbox,
} from '@itenium-forge/ui';
import {
  fetchCourses,
  createCourse,
  updateCourse,
  deleteCourse,
  type Course,
  type CourseFormData,
  type CourseStatus,
} from '@/api/client';
import { useAuthStore } from '@/stores';

const courseSchema = z.object({
  name: z.string().min(1),
  description: z.string(),
  category: z.string(),
  level: z.string(),
  status: z.enum(['Draft', 'Published', 'Archived']),
  isMandatory: z.boolean(),
});

type CourseFormValues = z.infer<typeof courseSchema>;

const LEVELS = ['Beginner', 'Intermediate', 'Advanced'];
const STATUSES: CourseStatus[] = ['Draft', 'Published', 'Archived'];

function statusVariant(status: CourseStatus): 'default' | 'secondary' | 'outline' {
  if (status === 'Published') return 'default';
  if (status === 'Archived') return 'secondary';
  return 'outline';
}

interface CourseFormProps {
  course?: Course;
  onClose: () => void;
}

function CourseForm({ course, onClose }: CourseFormProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<CourseFormValues>({
    resolver: zodResolver(courseSchema),
    defaultValues: {
      name: course?.name ?? '',
      description: course?.description ?? '',
      category: course?.category ?? '',
      level: course?.level ?? '',
      status: course?.status ?? 'Draft',
      isMandatory: course?.isMandatory ?? false,
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CourseFormData) => createCourse(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courses'] });
      toast.success(t('courses.created'));
      onClose();
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: CourseFormData) => updateCourse(course!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courses'] });
      toast.success(t('courses.updated'));
      onClose();
    },
  });

  const isLoading = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (data: CourseFormValues) => {
    if (course) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate(data);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-4">
      <div className="space-y-1">
        <Label htmlFor="name">{t('courses.name')} *</Label>
        <Input id="name" {...register('name')} />
        {errors.name && <p className="text-sm text-destructive">{t('common.required')}</p>}
      </div>

      <div className="space-y-1">
        <Label htmlFor="description">{t('courses.description')}</Label>
        <Input id="description" {...register('description')} />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label>{t('courses.category')}</Label>
          <Input {...register('category')} />
        </div>

        <div className="space-y-1">
          <Label>{t('courses.level')}</Label>
          <Select value={watch('level') || '_none'} onValueChange={(v) => setValue('level', v === '_none' ? '' : v)}>
            <SelectTrigger>
              <SelectValue placeholder={t('courses.selectLevel')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="_none">{t('courses.noLevel')}</SelectItem>
              {LEVELS.map((l) => (
                <SelectItem key={l} value={l}>
                  {t(`courses.levels.${l.toLowerCase()}`, { defaultValue: l })}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="space-y-1">
        <Label>{t('courses.status')}</Label>
        <Select value={watch('status')} onValueChange={(v) => setValue('status', v as CourseStatus)}>
          <SelectTrigger>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {t(`courses.statuses.${s.toLowerCase()}`)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex items-center gap-2">
          <Checkbox
            id="isMandatory"
            checked={watch('isMandatory')}
            onCheckedChange={(v) => setValue('isMandatory', !!v)}
          />
          <Label htmlFor="isMandatory">{t('courses.mandatory')}</Label>
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

export function Courses() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const canManage = (user?.isBackOffice || user?.isManager) ?? false;

  const [search, setSearch] = useState('');
  const [filterCategory, setFilterCategory] = useState('_all');
  const [filterLevel, setFilterLevel] = useState('_all');
  const [filterStatus, setFilterStatus] = useState<CourseStatus | '_all'>('_all');
  const [sheetOpen, setSheetOpen] = useState(false);
  const [editCourse, setEditCourse] = useState<Course | undefined>(undefined);
  const [deleteTarget, setDeleteTarget] = useState<Course | null>(null);

  const { data: courses = [], isLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: fetchCourses,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteCourse(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courses'] });
      toast.success(t('courses.deleted'));
      setDeleteTarget(null);
    },
  });

  const categories = useMemo(
    () => [...new Set(courses.map((c) => c.category).filter(Boolean))],
    [courses],
  );

  const filtered = useMemo(() => {
    return courses.filter((c) => {
      if (search && !c.name.toLowerCase().includes(search.toLowerCase())) return false;
      if (filterCategory !== '_all' && c.category !== filterCategory) return false;
      if (filterLevel !== '_all' && c.level !== filterLevel) return false;
      if (filterStatus !== '_all' && c.status !== filterStatus) return false;
      return true;
    });
  }, [courses, search, filterCategory, filterLevel, filterStatus]);

  const openCreate = () => {
    setEditCourse(undefined);
    setSheetOpen(true);
  };

  const openEdit = (course: Course) => {
    setEditCourse(course);
    setSheetOpen(true);
  };

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">{t('courses.title')}</h1>
        {canManage && (
          <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
            <SheetTrigger asChild>
              <Button onClick={openCreate}>
                <Plus className="size-4 mr-2" />
                {t('courses.addCourse')}
              </Button>
            </SheetTrigger>
            <SheetContent>
              <SheetHeader>
                <SheetTitle>{editCourse ? t('courses.editCourse') : t('courses.addCourse')}</SheetTitle>
              </SheetHeader>
              <CourseForm course={editCourse} onClose={() => setSheetOpen(false)} />
            </SheetContent>
          </Sheet>
        )}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3">
        <Input
          placeholder={t('common.search')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-xs"
        />

        <Select value={filterCategory} onValueChange={setFilterCategory}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('courses.category')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="_all">{t('courses.allCategories')}</SelectItem>
            {categories.map((cat) => (
              <SelectItem key={cat!} value={cat!}>
                {cat}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={filterLevel} onValueChange={setFilterLevel}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('courses.level')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="_all">{t('courses.allLevels')}</SelectItem>
            {LEVELS.map((l) => (
              <SelectItem key={l} value={l}>
                {t(`courses.levels.${l.toLowerCase()}`, { defaultValue: l })}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {canManage && (
          <Select value={filterStatus} onValueChange={(v) => setFilterStatus(v as CourseStatus | '_all')}>
            <SelectTrigger className="w-40">
              <SelectValue placeholder={t('courses.status')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="_all">{t('courses.allStatuses')}</SelectItem>
              {STATUSES.map((s) => (
                <SelectItem key={s} value={s}>
                  {t(`courses.statuses.${s.toLowerCase()}`)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      </div>

      {/* Delete confirmation banner */}
      {deleteTarget && (
        <div className="flex items-center justify-between rounded-md border border-destructive bg-destructive/10 p-3">
          <span className="text-sm">
            {t('courses.deleteConfirm', { name: deleteTarget.name })}
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
              <th className="p-3 text-left font-medium">{t('courses.name')}</th>
              <th className="p-3 text-left font-medium">{t('courses.mandatory')}</th>
              <th className="p-3 text-left font-medium">{t('courses.description')}</th>
              <th className="p-3 text-left font-medium">{t('courses.category')}</th>
              <th className="p-3 text-left font-medium">{t('courses.level')}</th>
              {canManage && <th className="p-3 text-left font-medium">{t('courses.status')}</th>}
              {canManage && (
                <th className="p-3 text-right font-medium">{t('courses.actions')}</th>
              )}
            </tr>
          </thead>
          <tbody>
            {filtered.map((course) => (
              <tr key={course.id} className="border-b">
                <td className="p-3 font-medium">{course.name}</td>
                <td className="p-3">
                  {course.isMandatory && <Badge variant="destructive">{t('courses.mandatory')}</Badge>}
                </td>
                <td className="p-3 text-muted-foreground">{course.description || '-'}</td>
                <td className="p-3">{course.category || '-'}</td>
                <td className="p-3">{course.level || '-'}</td>
                {canManage && (
                  <td className="p-3">
                    <Badge variant={statusVariant(course.status)}>
                      {t(`courses.statuses.${course.status.toLowerCase()}`)}
                    </Badge>
                  </td>
                )}
                {canManage && (
                  <td className="p-3 text-right">
                    <div className="flex justify-end gap-2">
                      <Button variant="ghost" size="sm" onClick={() => openEdit(course)}>
                        <Pencil className="size-4" />
                      </Button>
                      <Button variant="ghost" size="sm" onClick={() => setDeleteTarget(course)}>
                        <Trash2 className="size-4 text-destructive" />
                      </Button>
                    </div>
                  </td>
                )}
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td
                  colSpan={canManage ? 7 : 5}
                  className="p-3 text-center text-muted-foreground"
                >
                  {t('courses.noCourses')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
