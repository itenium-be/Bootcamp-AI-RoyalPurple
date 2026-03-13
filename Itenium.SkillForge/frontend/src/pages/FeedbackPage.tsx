import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod/v4';
import { MessageSquare, Plus, Send, BookOpen, ChevronRight } from 'lucide-react';
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
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
  Badge,
} from '@itenium-forge/ui';
import { toast } from 'sonner';
import { useAuthStore } from '@/stores';
import {
  fetchFeedback,
  fetchFeedbackById,
  createFeedback,
  addFeedbackComment,
  fetchMyCoaches,
  fetchAdmins,
  fetchCourses,
  type FeedbackItem,
  type FeedbackDetail as FeedbackDetailData,
  type FeedbackComment,
} from '@/api/client';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' });
}

function getApiErrorDetail(error: unknown): string | undefined {
  return (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
}

// ─── Create feedback schema ───────────────────────────────────────────────────

const feedbackSchema = z.object({
  recipientId: z.string().min(1, 'Required'),
  content: z.string().min(1, 'Required').max(2000),
  courseId: z.string().optional(),
});

type FeedbackFormValues = z.infer<typeof feedbackSchema>;

// ─── Comment form schema ──────────────────────────────────────────────────────

const commentSchema = z.object({ content: z.string().min(1, 'Required').max(2000) });
type CommentFormValues = z.infer<typeof commentSchema>;

// ─── Create feedback sheet ────────────────────────────────────────────────────

function CreateFeedbackSheet({ open, onOpenChange }: { open: boolean; onOpenChange: (v: boolean) => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const isManager = user?.role === 'manager';

  const { data: recipients = [] } = useQuery({
    queryKey: isManager ? ['admins'] : ['coaches'],
    queryFn: isManager ? fetchAdmins : fetchMyCoaches,
  });

  const { data: courses = [] } = useQuery({ queryKey: ['courses'], queryFn: fetchCourses });

  const form = useForm<FeedbackFormValues>({
    resolver: zodResolver(feedbackSchema),
    defaultValues: { recipientId: '', content: '', courseId: '' },
  });

  const mutation = useMutation({
    mutationFn: (values: FeedbackFormValues) =>
      createFeedback(values.recipientId, values.content, values.courseId ? Number(values.courseId) : undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feedback'] });
      toast.success(t('feedback.created', 'Feedback submitted'));
      form.reset();
      onOpenChange(false);
    },
    onError: (error: unknown) => {
      toast.error(t('feedback.createError', 'Failed to submit feedback'), { description: getApiErrorDetail(error) });
    },
  });

  const recipientLabel = isManager ? t('feedback.recipientAdmin', 'Admin') : t('feedback.recipientCoach', 'Coach');

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-[480px] pl-4">
        <SheetHeader>
          <SheetTitle>{t('feedback.createTitle', 'Submit Feedback')}</SheetTitle>
          <SheetDescription>
            {isManager
              ? t('feedback.createDescManager', 'Submit feedback to an admin.')
              : t('feedback.createDescLearner', 'Submit feedback to your coach.')}
          </SheetDescription>
        </SheetHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit((v) => mutation.mutate(v))} className="space-y-4 py-4">
            <FormField
              control={form.control}
              name="recipientId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{recipientLabel}</FormLabel>
                  <FormControl>
                    <Select onValueChange={field.onChange} value={field.value}>
                      <SelectTrigger>
                        <SelectValue placeholder={t('feedback.selectRecipient', 'Select recipient')} />
                      </SelectTrigger>
                      <SelectContent>
                        {recipients.map((r) => (
                          <SelectItem key={r.id} value={r.id}>
                            {r.firstName} {r.lastName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="courseId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('feedback.course', 'Course (optional)')}</FormLabel>
                  <FormControl>
                    <Select onValueChange={field.onChange} value={field.value}>
                      <SelectTrigger>
                        <SelectValue placeholder={t('feedback.selectCourse', 'Select a course')} />
                      </SelectTrigger>
                      <SelectContent>
                        {courses.map((c) => (
                          <SelectItem key={c.id} value={String(c.id)}>
                            {c.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="content"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('feedback.content', 'Feedback')}</FormLabel>
                  <FormControl>
                    <textarea
                      {...field}
                      rows={5}
                      placeholder={t('feedback.contentPlaceholder', 'Write your feedback…')}
                      className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    />
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

// ─── Feedback detail panel ────────────────────────────────────────────────────

function FeedbackDetail({ feedbackId, onClose }: { feedbackId: number; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();

  const { data: detail, isLoading } = useQuery<FeedbackDetailData>({
    queryKey: ['feedback', feedbackId],
    queryFn: () => fetchFeedbackById(feedbackId),
  });

  const form = useForm<CommentFormValues>({
    resolver: zodResolver(commentSchema),
    defaultValues: { content: '' },
  });

  const commentMutation = useMutation({
    mutationFn: (values: CommentFormValues) => addFeedbackComment(feedbackId, values.content),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feedback', feedbackId] });
      form.reset();
    },
    onError: (error: unknown) => {
      toast.error(t('feedback.commentError', 'Failed to add comment'), { description: getApiErrorDetail(error) });
    },
  });

  if (isLoading) {
    return <div className="p-6 text-sm text-muted-foreground">{t('common.loading', 'Loading...')}</div>;
  }

  if (!detail) return null;

  const isAuthor = detail.authorId === user?.id;

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between p-4 border-b">
        <h2 className="font-semibold">{t('feedback.detail', 'Feedback Detail')}</h2>
        <Button variant="ghost" size="sm" onClick={onClose}>
          ✕
        </Button>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {/* Original feedback */}
        <Card>
          <CardContent className="pt-4 space-y-2">
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <span className="font-medium">
                {isAuthor ? t('feedback.you', 'You') : (detail.authorName ?? detail.authorId)}
              </span>
              <ChevronRight className="size-3" />
              <span>
                {detail.recipientId === user?.id
                  ? t('feedback.you', 'You')
                  : (detail.recipientName ?? detail.recipientId)}
              </span>
              <span className="ml-auto">{formatDate(detail.createdAt)}</span>
            </div>
            {detail.courseId != null && (
              <div className="flex items-center gap-1 text-xs text-muted-foreground">
                <BookOpen className="size-3" />
                <span>{detail.courseName ?? `${t('feedback.courseContext', 'Course')} #${detail.courseId}`}</span>
              </div>
            )}
            <p className="text-sm whitespace-pre-wrap">{detail.content}</p>
          </CardContent>
        </Card>

        {/* Comments */}
        {detail.comments.length > 0 && (
          <div className="space-y-2">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
              {t('feedback.comments', 'Comments')}
            </p>
            {detail.comments.map((c: FeedbackComment) => (
              <div key={c.id} className={`flex gap-2 ${c.authorId === user?.id ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-[80%] rounded-lg px-3 py-2 text-sm ${c.authorId === user?.id ? 'bg-primary text-primary-foreground' : 'bg-muted'}`}
                >
                  {c.authorId !== user?.id && (
                    <p className="text-xs font-medium opacity-80 mb-1">{c.authorName ?? c.authorId}</p>
                  )}
                  <p>{c.content}</p>
                  <p className="text-xs opacity-60 mt-1">{formatDate(c.createdAt)}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Add comment */}
      <div className="border-t p-4">
        <Form {...form}>
          <form onSubmit={form.handleSubmit((v) => commentMutation.mutate(v))} className="flex gap-2">
            <FormField
              control={form.control}
              name="content"
              render={({ field }) => (
                <FormItem className="flex-1">
                  <FormControl>
                    <Input {...field} placeholder={t('feedback.addComment', 'Add a comment…')} />
                  </FormControl>
                </FormItem>
              )}
            />
            <Button type="submit" size="sm" disabled={commentMutation.isPending}>
              <Send className="size-4" />
            </Button>
          </form>
        </Form>
      </div>
    </div>
  );
}

// ─── Feedback list item ───────────────────────────────────────────────────────

function FeedbackListItem({
  item,
  selected,
  onClick,
  currentUserId,
}: {
  item: FeedbackItem;
  selected: boolean;
  onClick: () => void;
  currentUserId?: string;
}) {
  const { t } = useTranslation();
  const isAuthor = item.authorId === currentUserId;

  return (
    <button
      onClick={onClick}
      className={`w-full text-left p-3 rounded-lg border transition-colors hover:bg-accent ${selected ? 'bg-accent border-primary' : 'border-transparent'}`}
    >
      <div className="flex items-center gap-2 mb-1">
        <Badge variant={isAuthor ? 'default' : 'secondary'} className="text-xs">
          {isAuthor ? t('feedback.sent', 'Sent') : t('feedback.received', 'Received')}
        </Badge>
        <span className="text-xs text-muted-foreground truncate">
          {isAuthor ? (item.recipientName ?? item.recipientId) : (item.authorName ?? item.authorId)}
        </span>
        {item.courseId != null && (
          <>
            <BookOpen className="size-3 text-muted-foreground shrink-0" />
            <span className="text-xs text-muted-foreground truncate">{item.courseName ?? `#${item.courseId}`}</span>
          </>
        )}
        <span className="ml-auto text-xs text-muted-foreground shrink-0">{formatDate(item.createdAt)}</span>
      </div>
      <p className="text-sm line-clamp-2 text-muted-foreground">{item.content}</p>
    </button>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function FeedbackPage() {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const { data: items = [], isLoading } = useQuery({
    queryKey: ['feedback'],
    queryFn: fetchFeedback,
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-lg bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
            <MessageSquare className="size-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold">{t('feedback.title', 'Feedback')}</h1>
            <p className="text-sm text-muted-foreground">{isLoading ? '…' : `${items.length} items`}</p>
          </div>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="size-4 mr-2" />
          {t('feedback.new', 'New Feedback')}
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-[320px_1fr] gap-4 h-[calc(100vh-200px)]">
        {/* List */}
        <Card className="overflow-hidden">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">{t('feedback.allFeedback', 'All Feedback')}</CardTitle>
          </CardHeader>
          <CardContent className="p-2 overflow-y-auto h-full">
            {isLoading ? (
              <p className="text-sm text-muted-foreground text-center py-4">{t('common.loading', 'Loading...')}</p>
            ) : items.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-4">
                {t('feedback.noFeedback', 'No feedback yet')}
              </p>
            ) : (
              <div className="space-y-1">
                {items.map((item) => (
                  <FeedbackListItem
                    key={item.id}
                    item={item}
                    selected={selectedId === item.id}
                    onClick={() => setSelectedId(item.id)}
                    currentUserId={user?.id}
                  />
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Detail */}
        <Card className="overflow-hidden">
          {selectedId == null ? (
            <div className="flex flex-col items-center justify-center h-full text-muted-foreground gap-2">
              <MessageSquare className="size-10 opacity-30" />
              <p className="text-sm">{t('feedback.selectItem', 'Select an item to view details')}</p>
            </div>
          ) : (
            <FeedbackDetail feedbackId={selectedId} onClose={() => setSelectedId(null)} />
          )}
        </Card>
      </div>

      <CreateFeedbackSheet open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}
