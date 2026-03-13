import { createFileRoute } from '@tanstack/react-router';
import { Progress } from '@/pages/Progress';

export const Route = createFileRoute('/_authenticated/progress')({
  component: Progress,
});
