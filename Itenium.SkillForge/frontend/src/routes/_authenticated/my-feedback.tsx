import { createFileRoute } from '@tanstack/react-router';
import { FeedbackPage } from '@/pages/Feedback';

export const Route = createFileRoute('/_authenticated/my-feedback')({ component: FeedbackPage });
