import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type Skin = 'classic' | 'wouter';

interface SkinState {
  skin: Skin;
  setSkin: (skin: Skin) => void;
}

function applySkin(skin: Skin) {
  document.documentElement.classList.toggle('skin-wouter', skin === 'wouter');
}

export const useSkinStore = create<SkinState>()(
  persist(
    (set) => ({
      skin: 'classic',
      setSkin: (skin) => {
        applySkin(skin);
        set({ skin });
      },
    }),
    {
      name: 'skin-storage',
      onRehydrateStorage: () => (state) => {
        if (state) applySkin(state.skin);
      },
    },
  ),
);
