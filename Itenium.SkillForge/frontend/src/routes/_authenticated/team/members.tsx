import { createFileRoute } from '@tanstack/react-router';
import { useTeamStore } from '@/stores';
import { useTranslation } from 'react-i18next';
import { TeamMembers } from '@/pages/TeamMembers';

function ManagerTeamMembers() {
  const { t } = useTranslation();
  const { selectedTeam } = useTeamStore();

  if (!selectedTeam) {
    return <div className="text-muted-foreground p-6">{t('common.noResults')}</div>;
  }

  return <TeamMembers teamId={selectedTeam.id} teamName={selectedTeam.name} hideManage />;
}

export const Route = createFileRoute('/_authenticated/team/members')({ component: ManagerTeamMembers });
