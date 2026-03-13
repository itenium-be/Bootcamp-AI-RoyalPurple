import { createFileRoute } from '@tanstack/react-router';
import { TeamMembers } from '@/pages/TeamMembers';

export const Route = createFileRoute('/_authenticated/admin/teams/$teamId/members')({
  component: function TeamMembersRoute() {
    const { teamId } = Route.useParams();
    return <TeamMembers teamId={Number(teamId)} />;
  },
});
