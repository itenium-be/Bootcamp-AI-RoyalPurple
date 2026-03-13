import { useGoalStore, type Goal } from '../goalStore';

const goal1: Goal = {
  id: 1,
  skillName: 'Clean Code',
  consultantUserId: 'user-1',
  createdByCoachId: 'coach-1',
  currentNiveau: 1,
  targetNiveau: 3,
  deadline: '2026-06-01T00:00:00Z',
  isActive: true,
  readinessFlagRaisedAt: null,
  linkedResources: null,
  createdAt: '2026-03-01T00:00:00Z',
};

const goal2: Goal = {
  id: 2,
  skillName: 'Entity Framework',
  consultantUserId: 'user-1',
  createdByCoachId: 'coach-1',
  currentNiveau: 1,
  targetNiveau: 2,
  deadline: '2026-05-01T00:00:00Z',
  isActive: true,
  readinessFlagRaisedAt: null,
  linkedResources: null,
  createdAt: '2026-03-01T00:00:00Z',
};

function resetStore() {
  useGoalStore.setState({ goals: [] });
}

beforeEach(() => {
  resetStore();
});

describe('useGoalStore', () => {
  describe('setGoals', () => {
    it('sets goals list', () => {
      useGoalStore.getState().setGoals([goal1, goal2]);
      expect(useGoalStore.getState().goals).toHaveLength(2);
    });

    it('replaces existing goals', () => {
      useGoalStore.getState().setGoals([goal1]);
      useGoalStore.getState().setGoals([goal2]);
      expect(useGoalStore.getState().goals).toHaveLength(1);
      expect(useGoalStore.getState().goals[0].skillName).toBe('Entity Framework');
    });
  });

  describe('addGoal', () => {
    it('adds a goal to the list', () => {
      useGoalStore.getState().setGoals([goal1]);
      useGoalStore.getState().addGoal(goal2);
      expect(useGoalStore.getState().goals).toHaveLength(2);
    });
  });

  describe('updateGoal', () => {
    it('updates an existing goal by id', () => {
      useGoalStore.getState().setGoals([goal1, goal2]);
      const updated = { ...goal1, targetNiveau: 4 };
      useGoalStore.getState().updateGoal(updated);
      const goals = useGoalStore.getState().goals;
      expect(goals.find((g) => g.id === 1)?.targetNiveau).toBe(4);
      expect(goals).toHaveLength(2);
    });
  });

  describe('removeGoal', () => {
    it('removes a goal by id', () => {
      useGoalStore.getState().setGoals([goal1, goal2]);
      useGoalStore.getState().removeGoal(1);
      expect(useGoalStore.getState().goals).toHaveLength(1);
      expect(useGoalStore.getState().goals[0].id).toBe(2);
    });
  });

  describe('raiseReadinessFlag', () => {
    it('sets readinessFlagRaisedAt on the goal', () => {
      useGoalStore.getState().setGoals([goal1]);
      useGoalStore.getState().raiseReadinessFlag(1, '2026-03-13T10:00:00Z');
      const updated = useGoalStore.getState().goals.find((g) => g.id === 1);
      expect(updated?.readinessFlagRaisedAt).toBe('2026-03-13T10:00:00Z');
    });
  });

  describe('clearReadinessFlag', () => {
    it('clears readinessFlagRaisedAt on the goal', () => {
      const goalWithFlag: Goal = { ...goal1, readinessFlagRaisedAt: '2026-03-10T00:00:00Z' };
      useGoalStore.getState().setGoals([goalWithFlag]);
      useGoalStore.getState().clearReadinessFlag(1);
      const updated = useGoalStore.getState().goals.find((g) => g.id === 1);
      expect(updated?.readinessFlagRaisedAt).toBeNull();
    });
  });

  describe('activeGoals', () => {
    it('returns only active goals', () => {
      const inactiveGoal: Goal = { ...goal2, isActive: false };
      useGoalStore.getState().setGoals([goal1, inactiveGoal]);
      expect(useGoalStore.getState().activeGoals()).toHaveLength(1);
      expect(useGoalStore.getState().activeGoals()[0].id).toBe(1);
    });
  });

  describe('pendingReadinessFlags', () => {
    it('returns goals with a readiness flag raised', () => {
      const goalWithFlag: Goal = { ...goal1, readinessFlagRaisedAt: '2026-03-10T00:00:00Z' };
      useGoalStore.getState().setGoals([goalWithFlag, goal2]);
      expect(useGoalStore.getState().pendingReadinessFlags()).toHaveLength(1);
      expect(useGoalStore.getState().pendingReadinessFlags()[0].id).toBe(1);
    });
  });

  describe('reset', () => {
    it('clears all goals', () => {
      useGoalStore.getState().setGoals([goal1, goal2]);
      useGoalStore.getState().reset();
      expect(useGoalStore.getState().goals).toHaveLength(0);
    });
  });
});
