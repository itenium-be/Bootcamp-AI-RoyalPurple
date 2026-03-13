import { create } from 'zustand';

export interface Goal {
  id: number;
  skillName: string;
  consultantUserId: string;
  createdByCoachId: string;
  currentNiveau: number;
  targetNiveau: number;
  deadline: string;
  isActive: boolean;
  readinessFlagRaisedAt: string | null;
  linkedResources: string | null;
  createdAt: string;
}

interface GoalState {
  goals: Goal[];
  setGoals: (goals: Goal[]) => void;
  addGoal: (goal: Goal) => void;
  updateGoal: (goal: Goal) => void;
  removeGoal: (id: number) => void;
  raiseReadinessFlag: (id: number, raisedAt: string) => void;
  clearReadinessFlag: (id: number) => void;
  activeGoals: () => Goal[];
  pendingReadinessFlags: () => Goal[];
  reset: () => void;
}

export const useGoalStore = create<GoalState>()((set, get) => ({
  goals: [],

  setGoals: (goals) => set({ goals }),

  addGoal: (goal) => set((state) => ({ goals: [...state.goals, goal] })),

  updateGoal: (goal) =>
    set((state) => ({
      goals: state.goals.map((g) => (g.id === goal.id ? goal : g)),
    })),

  removeGoal: (id) =>
    set((state) => ({
      goals: state.goals.filter((g) => g.id !== id),
    })),

  raiseReadinessFlag: (id, raisedAt) =>
    set((state) => ({
      goals: state.goals.map((g) => (g.id === id ? { ...g, readinessFlagRaisedAt: raisedAt } : g)),
    })),

  clearReadinessFlag: (id) =>
    set((state) => ({
      goals: state.goals.map((g) => (g.id === id ? { ...g, readinessFlagRaisedAt: null } : g)),
    })),

  activeGoals: () => get().goals.filter((g) => g.isActive),

  pendingReadinessFlags: () => get().goals.filter((g) => g.readinessFlagRaisedAt !== null),

  reset: () => set({ goals: [] }),
}));
