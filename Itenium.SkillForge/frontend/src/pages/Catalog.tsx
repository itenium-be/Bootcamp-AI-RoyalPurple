import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { fetchCourses, fetchCourseResources, type CourseResource, type CourseResourceType } from '@/api/client';
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
} from 'lucide-react';

const RESOURCE_ICONS: Record<CourseResourceType, React.ReactNode> = {
  Video: <Video className="size-4" />,
  Article: <FileText className="size-4" />,
  Exercise: <Dumbbell className="size-4" />,
  Book: <BookOpen className="size-4" />,
  Link: <ExternalLink className="size-4" />,
  Other: <HelpCircle className="size-4" />,
};

function ResourceList({ courseId }: { courseId: number }) {
  const { t } = useTranslation();
  const { data: resources = [], isLoading } = useQuery({
    queryKey: ['course-resources', courseId],
    queryFn: () => fetchCourseResources(courseId),
  });

  if (isLoading) return <p className="text-sm text-muted-foreground px-1 py-2">{t('common.loading')}</p>;
  if (resources.length === 0)
    return <p className="text-sm text-muted-foreground px-1 py-2">{t('catalog.noResources')}</p>;

  return (
    <ul className="mt-3 space-y-2">
      {resources.map((r: CourseResource) => (
        <li key={r.id} className="flex items-start gap-3 rounded-md border px-3 py-2 text-sm">
          <span className="mt-0.5 text-muted-foreground">{RESOURCE_ICONS[r.type]}</span>
          <div className="min-w-0 flex-1">
            {r.url ? (
              <a
                href={r.url}
                target="_blank"
                rel="noopener noreferrer"
                className="font-medium hover:underline"
              >
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
        </li>
      ))}
    </ul>
  );
}

function CourseCard({ course }: { course: { id: number; name: string; description: string | null; category: string | null; level: string | null } }) {
  const { t } = useTranslation();
  const [expanded, setExpanded] = useState(false);

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
          <div className="flex shrink-0 flex-col items-end gap-1">
            {course.category && (
              <span className="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                {course.category}
              </span>
            )}
            {course.level && (
              <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
                {course.level}
              </span>
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
          <ResourceList courseId={course.id} />
        </div>
      )}
    </div>
  );
}

export function Catalog() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');

  const { data: courses = [], isLoading } = useQuery({
    queryKey: ['catalog-courses'],
    queryFn: () => fetchCourses(),
  });

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
      <div>
        <h1 className="text-3xl font-bold">{t('nav.catalog')}</h1>
        <p className="mt-1 text-muted-foreground">{t('catalog.subtitle')}</p>
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
      </div>

      {filtered.length === 0 ? (
        <p className="text-muted-foreground">{t('common.noResults')}</p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((course) => (
            <CourseCard key={course.id} course={course} />
          ))}
        </div>
      )}
    </div>
  );
}
