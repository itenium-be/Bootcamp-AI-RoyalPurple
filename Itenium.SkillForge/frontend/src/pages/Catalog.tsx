import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod/v4';
import { toast } from 'sonner';
import {
  Video,
  FileText,
  Dumbbell,
  BookOpen,
  ExternalLink,
  HelpCircle,
  Clock,
  ChevronDown,
  ChevronUp,
  Plus,
  Pencil,
  Trash2,
  MoreHorizontal,
} from 'lucide-react';
import {
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
  DropdownMenuSeparator,
} from '@itenium-forge/ui';
import {
  fetchCourses,
  fetchCourseResources,
  createCourse,
  updateCourse,
  deleteCourse,
  createCourseResource,
  updateCourseResource,
  deleteCourseResource,
  fetchMyEnrollments,
  enrollCourse,
  unenrollCourse,
  setCourseStatus,
  type Course,
  type CourseRequest,
  type CourseStatus,
  type CourseResource,
  type CourseResourceRequest,
  type CourseResourceType,
} from '@/api/client';
import { useAuthStore } from '@/stores';

// ─── Constants ────────────────────────────────────────────────────────────────

const RESOURCE_TYPE_OPTIONS: CourseResourceType[] = ['Video', 'Article', 'Exercise', 'Book', 'Link', 'Other'];

const RESOURCE_ICONS: Record<CourseResourceType, React.ReactNode> = {
  Video: <Video className="size-4" />,
  Article: <FileText className="size-4" />,
  Exercise: <Dumbbell className="size-4" />,
  Book: <BookOpen className="size-4" />,
  Link: <ExternalLink className="size-4" />,
  Other: <HelpCircle className="size-4" />,
};

// ─── Supported values ─────────────────────────────────────────────────────────

const COURSE_CATEGORIES = ['Development', 'Architecture', 'Management', 'Quality'] as const;
const COURSE_LEVELS = ['Beginner', 'Intermediate', 'Advanced'] as const;

// ─── Schemas ──────────────────────────────────────────────────────────────────

const courseSchema = z.object({
  name: z.string().min(1, 'Required').max(200, 'Max 200 characters'),
  description: z.string().max(2000, 'Max 2000 characters').nullable().optional(),
  category: z.string().nullable().optional(),
  level: z.string().nullable().optional(),
});

type CourseFormValues = z.infer<typeof courseSchema>;

const resourceSchema = z.object({
  title: z.string().min(1, 'Required').max(200, 'Max 200 characters'),
  type: z.enum(['Video', 'Article', 'Exercise', 'Book', 'Link', 'Other']),
  url: z
    .string()
    .url('Must be a valid URL')
    .nullable()
    .optional()
    .or(z.literal('').transform(() => null)),
  description: z.string().max(2000, 'Max 2000 characters').nullable().optional(),
  durationMinutes: z.number().int().min(1, 'Must be at least 1').nullable().optional(),
  order: z.number().int().min(0),
});

type ResourceFormValues = z.infer<typeof resourceSchema>;

// ─── Status helpers ────────────────────────────────────────────────────────────

const STATUS_BADGE_CLASSES: Record<CourseStatus, string> = {
  Draft: 'bg-yellow-500/10 text-yellow-600 dark:text-yellow-400',
  Published: 'bg-green-500/10 text-green-600 dark:text-green-400',
  Archived: 'bg-muted text-muted-foreground',
};

// ─── Course sheet (create / edit) ─────────────────────────────────────────────

function CourseSheet({
  course,
  open,
  onOpenChange,
}: {
  course?: Course;
  open: boolean;
  onOpenChange: (v: boolean) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const isEdit = !!course;

  const form = useForm<CourseFormValues>({
    resolver: zodResolver(courseSchema),
    defaultValues: {
      name: course?.name ?? '',
      description: course?.description ?? '',
      category: course?.category ?? '',
      level: course?.level ?? '',
    },
  });

  const mutation = useMutation({
    mutationFn: (values: CourseFormValues) => {
      const req: CourseRequest = {
        name: values.name,
        description: values.description || null,
        category: values.category || null,
        level: values.level || null,
      };
      return isEdit && course ? updateCourse(course.id, req) : createCourse(req);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-courses'] });
      toast.success(isEdit ? t('catalog.courseUpdated') : t('catalog.courseCreated'));
      form.reset();
      onOpenChange(false);
    },
    onError: () => {
      toast.error(isEdit ? t('catalog.courseUpdateError') : t('catalog.courseCreateError'));
    },
  });

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-[420px] pl-4">
        <SheetHeader>
          <SheetTitle>{isEdit ? t('catalog.editCourse') : t('catalog.addCourse')}</SheetTitle>
          <SheetDescription>{isEdit ? course?.name : t('catalog.addCourseDesc')}</SheetDescription>
        </SheetHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit((v) => mutation.mutate(v))} className="space-y-4 py-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courses.name')}</FormLabel>
                  <FormControl>
                    <Input {...field} value={field.value ?? ''} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courses.description')}</FormLabel>
                  <FormControl>
                    <Input {...field} value={field.value ?? ''} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="category"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courses.category')}</FormLabel>
                  <FormControl>
                    <select
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value || null)}
                      className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    >
                      <option value="">{t('catalog.selectCategory')}</option>
                      {COURSE_CATEGORIES.map((cat) => (
                        <option key={cat} value={cat}>
                          {cat}
                        </option>
                      ))}
                    </select>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="level"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courses.level')}</FormLabel>
                  <FormControl>
                    <select
                      value={field.value ?? ''}
                      onChange={(e) => field.onChange(e.target.value || null)}
                      className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    >
                      <option value="">{t('catalog.selectLevel')}</option>
                      {COURSE_LEVELS.map((lvl) => (
                        <option key={lvl} value={lvl}>
                          {lvl}
                        </option>
                      ))}
                    </select>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <SheetFooter className="pt-4">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('common.saving') : isEdit ? t('common.save') : t('common.create')}
              </Button>
            </SheetFooter>
          </form>
        </Form>
      </SheetContent>
    </Sheet>
  );
}

// ─── Resource sheet (create / edit) ───────────────────────────────────────────

function ResourceSheet({
  courseId,
  resource,
  nextOrder,
  open,
  onOpenChange,
}: {
  courseId: number;
  resource?: CourseResource;
  nextOrder: number;
  open: boolean;
  onOpenChange: (v: boolean) => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const isEdit = !!resource;

  const form = useForm<ResourceFormValues>({
    resolver: zodResolver(resourceSchema),
    defaultValues: {
      title: resource?.title ?? '',
      type: resource?.type ?? 'Article',
      url: resource?.url ?? '',
      description: resource?.description ?? '',
      durationMinutes: resource?.durationMinutes ?? undefined,
      order: resource?.order ?? nextOrder,
    },
  });

  const mutation = useMutation({
    mutationFn: (values: ResourceFormValues) => {
      const req: CourseResourceRequest = {
        title: values.title,
        type: values.type,
        url: values.url || null,
        description: values.description || null,
        durationMinutes: values.durationMinutes || null,
        order: values.order,
        skillId: null,
        toLevel: null,
      };
      return isEdit && resource
        ? updateCourseResource(courseId, resource.id, req)
        : createCourseResource(courseId, req);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['course-resources', courseId] });
      toast.success(isEdit ? t('catalog.resourceUpdated') : t('catalog.resourceCreated'));
      form.reset();
      onOpenChange(false);
    },
    onError: () => {
      toast.error(isEdit ? t('catalog.resourceUpdateError') : t('catalog.resourceCreateError'));
    },
  });

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-[420px] pl-4">
        <SheetHeader>
          <SheetTitle>{isEdit ? t('catalog.editResource') : t('catalog.addResource')}</SheetTitle>
          <SheetDescription>{isEdit ? resource?.title : t('catalog.addResourceDesc')}</SheetDescription>
        </SheetHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit((v) => mutation.mutate(v))} className="space-y-4 py-4">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('catalog.resourceTitle')}</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="type"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('catalog.resourceType')}</FormLabel>
                  <FormControl>
                    <select
                      {...field}
                      className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    >
                      {RESOURCE_TYPE_OPTIONS.map((opt) => (
                        <option key={opt} value={opt}>
                          {opt}
                        </option>
                      ))}
                    </select>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="url"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('catalog.resourceUrl')}</FormLabel>
                  <FormControl>
                    <Input {...field} value={field.value ?? ''} placeholder="https://..." />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('courses.description')}</FormLabel>
                  <FormControl>
                    <Input {...field} value={field.value ?? ''} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <div className="grid grid-cols-2 gap-3">
              <FormField
                control={form.control}
                name="durationMinutes"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('catalog.duration')}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="number"
                        min={1}
                        value={field.value ?? ''}
                        onChange={(e) => field.onChange(e.target.value === '' ? null : Number(e.target.value))}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="order"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('catalog.order')}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="number"
                        min={0}
                        onChange={(e) => field.onChange(Number(e.target.value))}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>
            <SheetFooter className="pt-4">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t('common.cancel')}
              </Button>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('common.saving') : isEdit ? t('common.save') : t('common.create')}
              </Button>
            </SheetFooter>
          </form>
        </Form>
      </SheetContent>
    </Sheet>
  );
}

// ─── Resource list ─────────────────────────────────────────────────────────────

function ResourceList({ courseId, canEdit }: { courseId: number; canEdit: boolean }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [editingResource, setEditingResource] = useState<CourseResource | null>(null);
  const [addOpen, setAddOpen] = useState(false);

  const { data: resources = [], isLoading } = useQuery({
    queryKey: ['course-resources', courseId],
    queryFn: () => fetchCourseResources(courseId),
  });

  const deleteMutation = useMutation({
    mutationFn: (resourceId: number) => deleteCourseResource(courseId, resourceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['course-resources', courseId] });
      toast.success(t('catalog.resourceDeleted'));
    },
    onError: () => toast.error(t('catalog.resourceDeleteError')),
  });

  if (isLoading) return <p className="text-sm text-muted-foreground px-1 py-2">{t('common.loading')}</p>;

  const nextOrder = resources.length > 0 ? Math.max(...resources.map((r) => r.order)) + 1 : 1;

  return (
    <div>
      {resources.length === 0 && <p className="text-sm text-muted-foreground py-2">{t('catalog.noResources')}</p>}
      <ul className="mt-1 space-y-2">
        {resources.map((r: CourseResource) => (
          <li key={r.id} className="flex items-start gap-3 rounded-md border px-3 py-2 text-sm">
            <span className="mt-0.5 text-muted-foreground">{RESOURCE_ICONS[r.type]}</span>
            <div className="min-w-0 flex-1">
              {r.url ? (
                <a href={r.url} target="_blank" rel="noopener noreferrer" className="font-medium hover:underline">
                  {r.title}
                </a>
              ) : (
                <span className="font-medium">{r.title}</span>
              )}
              {r.description && <p className="text-muted-foreground mt-0.5">{r.description}</p>}
            </div>
            {r.durationMinutes && (
              <span className="flex items-center gap-1 text-muted-foreground shrink-0">
                <Clock className="size-3" />
                {r.durationMinutes} {t('catalog.minutes')}
              </span>
            )}
            {canEdit && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" className="size-7 shrink-0">
                    <MoreHorizontal className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => setEditingResource(r)}>
                    <Pencil className="mr-2 size-4" />
                    {t('common.edit')}
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="text-destructive focus:text-destructive"
                    onClick={() => window.confirm('Delete this resource?') && deleteMutation.mutate(r.id)}
                  >
                    <Trash2 className="mr-2 size-4" />
                    {t('common.delete')}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </li>
        ))}
      </ul>

      {canEdit && (
        <Button variant="ghost" size="sm" className="mt-2 gap-1" onClick={() => setAddOpen(true)}>
          <Plus className="size-4" />
          {t('catalog.addResource')}
        </Button>
      )}

      {addOpen && <ResourceSheet courseId={courseId} nextOrder={nextOrder} open={addOpen} onOpenChange={setAddOpen} />}
      {editingResource && (
        <ResourceSheet
          courseId={courseId}
          resource={editingResource}
          nextOrder={nextOrder}
          open={!!editingResource}
          onOpenChange={(v) => !v && setEditingResource(null)}
        />
      )}
    </div>
  );
}

// ─── Course card ───────────────────────────────────────────────────────────────

function CourseCard({
  course,
  canEdit,
  isEnrolled,
}: {
  course: Course;
  canEdit: boolean;
  isEnrolled: boolean;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);
  const [editOpen, setEditOpen] = useState(false);

  const deleteMutation = useMutation({
    mutationFn: () => deleteCourse(course.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-courses'] });
      toast.success(t('catalog.courseDeleted'));
    },
    onError: () => toast.error(t('catalog.courseDeleteError')),
  });

  const enrollMutation = useMutation({
    mutationFn: () => enrollCourse(course.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-enrollments'] });
      toast.success(t('progress.enrollSuccess'));
    },
    onError: () => toast.error(t('progress.enrollError')),
  });

  const unenrollMutation = useMutation({
    mutationFn: () => unenrollCourse(course.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-enrollments'] });
      toast.success(t('progress.unenrollSuccess'));
    },
    onError: () => toast.error(t('progress.unenrollError')),
  });

  const statusMutation = useMutation({
    mutationFn: (status: CourseStatus) => setCourseStatus(course.id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['catalog-courses'] }),
    onError: () => toast.error(t('catalog.statusUpdateError')),
  });

  return (
    <div className="rounded-lg border bg-card">
      <div className="p-4">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0 flex-1">
            <h3 className="font-semibold leading-tight">{course.name}</h3>
            {course.description && (
              <p className="mt-1 text-sm text-muted-foreground line-clamp-2">{course.description}</p>
            )}
          </div>
          <div className="flex shrink-0 items-start gap-2">
            <div className="flex flex-col items-end gap-1">
              <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_BADGE_CLASSES[course.status]}`}>
                {t(`catalog.status_${course.status.toLowerCase()}`)}
              </span>
              {course.category && (
                <span className="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                  {course.category}
                </span>
              )}
              {course.level && (
                <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">{course.level}</span>
              )}
            </div>
            {!canEdit && (
              isEnrolled ? (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => unenrollMutation.mutate()}
                  disabled={unenrollMutation.isPending}
                >
                  {t('progress.enrolled')}
                </Button>
              ) : (
                <Button
                  size="sm"
                  onClick={() => enrollMutation.mutate()}
                  disabled={enrollMutation.isPending}
                >
                  {t('progress.enroll')}
                </Button>
              )
            )}
            {canEdit && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" className="size-7 -mt-1">
                    <MoreHorizontal className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => setEditOpen(true)}>
                    <Pencil className="mr-2 size-4" />
                    {t('common.edit')}
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  {course.status !== 'Published' && (
                    <DropdownMenuItem onClick={() => statusMutation.mutate('Published')}>
                      {t('catalog.publish')}
                    </DropdownMenuItem>
                  )}
                  {course.status !== 'Draft' && (
                    <DropdownMenuItem onClick={() => statusMutation.mutate('Draft')}>
                      {t('catalog.setDraft')}
                    </DropdownMenuItem>
                  )}
                  {course.status !== 'Archived' && (
                    <DropdownMenuItem onClick={() => statusMutation.mutate('Archived')}>
                      {t('catalog.archive')}
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="text-destructive focus:text-destructive"
                    onClick={() => window.confirm(`Delete course "${course.name}"?`) && deleteMutation.mutate()}
                    disabled={deleteMutation.isPending}
                  >
                    <Trash2 className="mr-2 size-4" />
                    {t('common.delete')}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>
        </div>

        <button
          onClick={() => setExpanded((v) => !v)}
          className="mt-3 flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          {expanded ? <ChevronUp className="size-4" /> : <ChevronDown className="size-4" />}
          {expanded ? t('catalog.hideResources') : t('catalog.showResources')}
        </button>
      </div>

      {expanded && (
        <div className="border-t px-4 pb-4">
          <ResourceList courseId={course.id} canEdit={canEdit} />
        </div>
      )}

      {editOpen && <CourseSheet course={course} open={editOpen} onOpenChange={setEditOpen} />}
    </div>
  );
}

// ─── Catalog page ──────────────────────────────────────────────────────────────

export function Catalog() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState<CourseStatus | ''>('');
  const [addCourseOpen, setAddCourseOpen] = useState(false);

  const canEdit = user?.isBackOffice || user?.role === 'manager';
  const isManager = canEdit;

  const { data: courses = [], isLoading } = useQuery({
    queryKey: ['catalog-courses', statusFilter || undefined],
    queryFn: () => fetchCourses(undefined, statusFilter || undefined),
  });

  const { data: enrollments = [] } = useQuery({
    queryKey: ['my-enrollments'],
    queryFn: fetchMyEnrollments,
    enabled: !canEdit,
  });

  const enrolledCourseIds = new Set(enrollments.map((e) => e.courseId));

  const categories = [...new Set(courses.map((c) => c.category).filter(Boolean))] as string[];

  const filtered = courses.filter((c) => {
    const matchesSearch =
      !search ||
      c.name.toLowerCase().includes(search.toLowerCase()) ||
      c.description?.toLowerCase().includes(search.toLowerCase());
    const matchesCategory = !categoryFilter || c.category === categoryFilter;
    return matchesSearch && matchesCategory;
  });

  if (isLoading) return <p>{t('common.loading')}</p>;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">{t('nav.catalog')}</h1>
          <p className="mt-1 text-muted-foreground">{t('catalog.subtitle')}</p>
        </div>
        {canEdit && (
          <Button onClick={() => setAddCourseOpen(true)} className="shrink-0">
            <Plus className="mr-2 size-4" />
            {t('catalog.addCourse')}
          </Button>
        )}
      </div>

      <div className="flex flex-wrap gap-3">
        <input
          type="text"
          placeholder={t('common.search')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="rounded-md border bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-ring w-64"
        />
        <select
          value={categoryFilter}
          onChange={(e) => setCategoryFilter(e.target.value)}
          className="rounded-md border bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
        >
          <option value="">{t('catalog.allCategories')}</option>
          {categories.map((cat) => (
            <option key={cat} value={cat}>
              {cat}
            </option>
          ))}
        </select>
        {isManager && (
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as CourseStatus | '')}
            className="rounded-md border bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">{t('catalog.allStatuses')}</option>
            <option value="Draft">{t('catalog.status_draft')}</option>
            <option value="Published">{t('catalog.status_published')}</option>
            <option value="Archived">{t('catalog.status_archived')}</option>
          </select>
        )}
      </div>

      {filtered.length === 0 ? (
        <p className="text-muted-foreground">{t('common.noResults')}</p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((course) => (
            <CourseCard key={course.id} course={course} canEdit={!!canEdit} isEnrolled={enrolledCourseIds.has(course.id)} />
          ))}
        </div>
      )}

      {addCourseOpen && <CourseSheet open={addCourseOpen} onOpenChange={setAddCourseOpen} />}
    </div>
  );
}
