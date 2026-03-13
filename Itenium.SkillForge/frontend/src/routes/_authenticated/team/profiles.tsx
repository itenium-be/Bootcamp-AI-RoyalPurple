import { createFileRoute } from '@tanstack/react-router';
import { ConsultantProfiles } from '@/pages/ConsultantProfiles';

export const Route = createFileRoute('/_authenticated/team/profiles')({
  component: ConsultantProfiles,
});
