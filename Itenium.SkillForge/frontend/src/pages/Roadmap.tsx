import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { fetchRoadmap } from '@/api/client';

const TIER_KEYS: Record<number, string> = {
  1: 'roadmap.tier1',
  2: 'roadmap.tier2',
  3: 'roadmap.tier3',
  4: 'roadmap.tier4',
};

export default function Roadmap() {
  const { t } = useTranslation();
  const [showAll, setShowAll] = useState(false);

  const { data: nodes = [], isLoading } = useQuery({
    queryKey: ['roadmap', showAll],
    queryFn: () => fetchRoadmap(showAll),
  });

  if (isLoading) {
    return <p>{t('common.loading')}</p>;
  }

  const tiers = [...new Set(nodes.map((n) => n.tier))].sort((a, b) => a - b);

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">{t('roadmap.title')}</h1>

      {nodes.length === 0 ? (
        <p className="text-muted-foreground">{t('roadmap.noRoadmap')}</p>
      ) : (
        <>
          {tiers.map((tier) => (
            <div key={tier} className="mb-8">
              <h2 className="text-lg font-semibold mb-3">{t(TIER_KEYS[tier] ?? `Tier ${tier}`)}</h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {nodes
                  .filter((n) => n.tier === tier)
                  .map((node) => (
                    <div key={node.id} className="border rounded-lg p-4">
                      <h3 className="font-medium">{node.name}</h3>
                      {node.description && <p className="text-sm text-muted-foreground mt-1">{node.description}</p>}
                    </div>
                  ))}
              </div>
            </div>
          ))}

          {!showAll && (
            <button onClick={() => setShowAll(true)} className="mt-2 px-4 py-2 border rounded hover:bg-accent">
              {t('roadmap.showAll')}
            </button>
          )}
        </>
      )}
    </div>
  );
}
