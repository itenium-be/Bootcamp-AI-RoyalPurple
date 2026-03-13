import { createFileRoute } from '@tanstack/react-router';
import { FeedbackPage } from '@/pages/FeedbackPage';

export const Route = createFileRoute('/_authenticated/reports/feedback')({
  component: FeedbackPage,
});
