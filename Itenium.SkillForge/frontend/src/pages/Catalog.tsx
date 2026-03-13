import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Input,
  Badge,
  Button,
  Select,
  SelectTrigger,
  SelectContent,
  SelectItem,
  SelectValue,
} from '@itenium-forge/ui';
import { fetchCourses, fetchEnrollments, enrollInCourse } from '@/api/client';

const LEVELS = ['Beginner', 'Intermediate', 'Advanced'];

export function Catalog() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [filterCategory, setFilterCategory] = useState('_all');
  const [filterLevel, setFilterLevel] = useState('_all');

  const { data: allCourses = [], isLoading } = useQuery({
    queryKey: ['courses'],
    queryFn: fetchCourses,
  });

  const { data: enrollments = [] } = useQuery({
    queryKey: ['enrollments'],
    queryFn: fetchEnrollments,
  });

  const enrolledCourseIds = useMemo(
    () => new Set(enrollments.map((e) => e.courseId)),
    [enrollments],
  );

  const enrollMutation = useMutation({
    mutationFn: (courseId: number) => enrollInCourse(courseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments'] });
      toast.success(t('enrollment.enrollSuccess'));
    },
  });

  const published = useMemo(() => allCourses.filter((c) => c.status === 'Published'), [allCourses]);
  const categories = useMemo(() => [...new Set(published.map((c) => c.category).filter(Boolean))], [published]);

  const filtered = useMemo(() => {
    return published.filter((c) => {
      if (search && !c.name.toLowerCase().includes(search.toLowerCase())) return false;
      if (filterCategory !== '_all' && c.category !== filterCategory) return false;
      if (filterLevel !== '_all' && c.level !== filterLevel) return false;
      return true;
    });
  }, [published, search, filterCategory, filterLevel]);

  if (isLoading) return <div>{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('catalog.title')}</h1>
        <p className="text-muted-foreground mt-1">{t('catalog.subtitle')}</p>
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
              <SelectItem key={cat!} value={cat!}>{cat}</SelectItem>
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
              <SelectItem key={l} value={l}>{t(`courses.levels.${l.toLowerCase()}`, { defaultValue: l })}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Course cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filtered.map((course) => {
          const isEnrolled = enrolledCourseIds.has(course.id);
          return (
            <div key={course.id} className="rounded-lg border p-4 space-y-3 hover:bg-muted/30 transition-colors flex flex-col">
              <div className="flex items-start justify-between gap-2">
                <h3 className="font-semibold">{course.name}</h3>
                <div className="flex gap-1 shrink-0">
                  {course.isMandatory && <Badge variant="destructive">{t('courses.mandatory')}</Badge>}
                  {course.level && <Badge variant="outline">{course.level}</Badge>}
                </div>
              </div>
              {course.description && (
                <p className="text-sm text-muted-foreground line-clamp-2">{course.description}</p>
              )}
              {course.category && (
                <p className="text-xs text-muted-foreground">{course.category}</p>
              )}
              <div className="pt-2 mt-auto">
                {isEnrolled ? (
                  <Badge variant="secondary">{t('enrollment.alreadyEnrolled')}</Badge>
                ) : (
                  <Button
                    size="sm"
                    onClick={() => enrollMutation.mutate(course.id)}
                    disabled={enrollMutation.isPending}
                  >
                    {t('enrollment.enroll')}
                  </Button>
                )}
              </div>
            </div>
          );
        })}
        {filtered.length === 0 && (
          <div className="col-span-full text-center text-muted-foreground py-8">
            {t('courses.noCourses')}
          </div>
        )}
      </div>
    </div>
  );
}
