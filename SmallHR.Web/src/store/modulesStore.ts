import { create } from 'zustand';
import type { ModuleNode } from '../services/modules';
import { fetchModulesForCurrentUser } from '../services/modules';

interface ModulesState {
  modules: ModuleNode[];
  loading: boolean;
  error?: string;
  refresh: () => Promise<void>;
}

export const useModulesStore = create<ModulesState>((set) => ({
  modules: [],
  loading: false,
  error: undefined,
  refresh: async () => {
    set({ loading: true, error: undefined });
    try {
      const mods = await fetchModulesForCurrentUser();
      set({ modules: mods, loading: false });
    } catch (e: any) {
      set({ error: e?.message || 'Failed to load modules', loading: false });
    }
  },
}));


