import { createFileRoute } from '@tanstack/react-router';
import { TeamAssignments } from '@/pages/TeamAssignments';

export const Route = createFileRoute('/_authenticated/team/assignments')({ component: TeamAssignments });
