import { createFileRoute } from '@tanstack/react-router';
import { TeamSuggestions } from '@/pages/TeamSuggestions';

export const Route = createFileRoute('/_authenticated/team/suggestions')({ component: TeamSuggestions });
