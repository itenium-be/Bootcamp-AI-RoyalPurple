import { useTranslation } from 'react-i18next';
import { Award } from 'lucide-react';

export function MyCertificates() {
  const { t } = useTranslation();
  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">{t('nav.myCertificates')}</h1>
      <div className="text-center py-12 text-muted-foreground">
        <Award className="size-12 mx-auto mb-3 opacity-30" />
        <p>{t('common.comingSoon')}</p>
      </div>
    </div>
  );
}
