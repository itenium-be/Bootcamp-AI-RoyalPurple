import { createFileRoute } from '@tanstack/react-router';
import { MySuggestions } from '@/pages/MySuggestions';

export const Route = createFileRoute('/_authenticated/my-suggestions')({ component: MySuggestions });
