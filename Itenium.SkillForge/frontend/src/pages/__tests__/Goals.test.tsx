import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
import { useGoalStore } from '@/stores/goalStore';
import type { Goal } from '@/stores/goalStore';
import { useAuthStore } from '@/stores/authStore';
import { useTeamStore } from '@/stores/teamStore';

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

// Mock TanStack Query
const mockUseQuery = vi.fn();
const mockUseMutation = vi.fn();
vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
  useMutation: (...args: unknown[]) => mockUseMutation(...args),
  useQueryClient: () => ({ invalidateQueries: vi.fn() }),
}));

// Mock API client
const mockFetchGoals = vi.fn();
const mockCreateGoal = vi.fn();
vi.mock('@/api/client', () => ({
  fetchGoals: (...args: unknown[]) => mockFetchGoals(...args),
  createGoal: (...args: unknown[]) => mockCreateGoal(...args),
}));

// eslint-disable-next-line import-x/order -- must come after vi.mock calls
import { Goals } from '../Goals';

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
  createdAt: '2026-03-01T00:00:00Z',
  linkedResources: null,
};

const goal2: Goal = {
  id: 2,
  skillName: 'Entity Framework',
  consultantUserId: 'user-2',
  createdByCoachId: 'coach-1',
  currentNiveau: 2,
  targetNiveau: 4,
  deadline: '2026-05-01T00:00:00Z',
  isActive: false,
  readinessFlagRaisedAt: null,
  createdAt: '2026-03-01T00:00:00Z',
  linkedResources: 'https://example.com/ef',
};

function setupStores(isManager = false) {
  useAuthStore.setState({
    accessToken: 'fake-token',
    isAuthenticated: true,
    user: { id: 'user-1', email: 'test@test.com', name: 'Test User', isBackOffice: false },
  });
  useTeamStore.setState({
    mode: isManager ? 'manager' : 'backoffice',
    teams: isManager ? [{ id: 1, name: 'Team A' }] : [],
    selectedTeam: isManager ? { id: 1, name: 'Team A' } : null,
  });
}

beforeEach(() => {
  localStorage.clear();
  useGoalStore.setState({ goals: [] });
  useAuthStore.setState({ accessToken: null, user: null, isAuthenticated: false });
  useTeamStore.setState({ mode: 'backoffice', selectedTeam: null, teams: [] });
  mockUseQuery.mockReturnValue({ data: [], isLoading: false });
  mockUseMutation.mockReturnValue({ mutate: vi.fn(), isPending: false });
});

describe('Goals page', () => {
  describe('loading state', () => {
    it('shows loading indicator while fetching', () => {
      setupStores();
      mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });

      render(<Goals />);

      expect(screen.getByText('common.loading')).toBeInTheDocument();
    });
  });

  describe('goals list', () => {
    it('renders the page title', () => {
      setupStores();
      mockUseQuery.mockReturnValue({ data: [], isLoading: false });

      render(<Goals />);

      expect(screen.getByText('goals.title')).toBeInTheDocument();
    });

    it('shows goals in a table', () => {
      setupStores();
      mockUseQuery.mockReturnValue({ data: [goal1, goal2], isLoading: false });

      render(<Goals />);

      expect(screen.getByText('Clean Code')).toBeInTheDocument();
      expect(screen.getByText('Entity Framework')).toBeInTheDocument();
    });

    it('shows empty state when no goals', () => {
      setupStores();
      mockUseQuery.mockReturnValue({ data: [], isLoading: false });

      render(<Goals />);

      expect(screen.getByText('goals.noGoals')).toBeInTheDocument();
    });

    it('shows consultant user ID column', () => {
      setupStores();
      mockUseQuery.mockReturnValue({ data: [goal1], isLoading: false });

      render(<Goals />);

      expect(screen.getByText('user-1')).toBeInTheDocument();
    });

    it('shows niveau range', () => {
      setupStores();
      mockUseQuery.mockReturnValue({ data: [goal1], isLoading: false });

      render(<Goals />);

      expect(screen.getByText('1 → 3')).toBeInTheDocument();
    });

    it('shows active badge for active goals', () => {
      setupStores();
      mockUseQuery.mockReturnValue({ data: [goal1], isLoading: false });

      render(<Goals />);

      expect(screen.getByText('common.active')).toBeInTheDocument();
    });
  });

  describe('manager actions', () => {
    it('shows Assign Goal button for managers', () => {
      setupStores(true);
      mockUseQuery.mockReturnValue({ data: [], isLoading: false });

      render(<Goals />);

      expect(screen.getByRole('button', { name: 'goals.assignGoal' })).toBeInTheDocument();
    });

    it('hides Assign Goal button for non-managers', () => {
      setupStores(false);
      mockUseQuery.mockReturnValue({ data: [], isLoading: false });

      render(<Goals />);

      expect(screen.queryByRole('button', { name: 'goals.assignGoal' })).not.toBeInTheDocument();
    });

    it('shows assign form when button is clicked', () => {
      setupStores(true);
      mockUseQuery.mockReturnValue({ data: [], isLoading: false });

      render(<Goals />);
      fireEvent.click(screen.getByRole('button', { name: 'goals.assignGoal' }));

      expect(screen.getByLabelText('goals.consultantUserId')).toBeInTheDocument();
      expect(screen.getByLabelText('goals.skillName')).toBeInTheDocument();
      expect(screen.getByLabelText('goals.currentNiveau')).toBeInTheDocument();
      expect(screen.getByLabelText('goals.targetNiveau')).toBeInTheDocument();
      expect(screen.getByLabelText('goals.deadline')).toBeInTheDocument();
      expect(screen.getByLabelText('goals.linkedResources')).toBeInTheDocument();
    });

    it('submits the form with all fields', async () => {
      const mockMutate = vi.fn();
      setupStores(true);
      mockUseQuery.mockReturnValue({ data: [], isLoading: false });
      mockUseMutation.mockReturnValue({ mutate: mockMutate, isPending: false });

      render(<Goals />);
      fireEvent.click(screen.getByRole('button', { name: 'goals.assignGoal' }));

      fireEvent.change(screen.getByLabelText('goals.consultantUserId'), { target: { value: 'user-1' } });
      fireEvent.change(screen.getByLabelText('goals.skillName'), { target: { value: 'TypeScript' } });
      fireEvent.change(screen.getByLabelText('goals.currentNiveau'), { target: { value: '2' } });
      fireEvent.change(screen.getByLabelText('goals.targetNiveau'), { target: { value: '4' } });
      fireEvent.change(screen.getByLabelText('goals.deadline'), { target: { value: '2026-12-01' } });
      fireEvent.change(screen.getByLabelText('goals.linkedResources'), {
        target: { value: 'https://example.com/ts' },
      });

      fireEvent.click(screen.getByRole('button', { name: 'common.save' }));

      await waitFor(() => {
        expect(mockMutate).toHaveBeenCalledWith(
          expect.objectContaining({
            consultantUserId: 'user-1',
            skillName: 'TypeScript',
            currentNiveau: 2,
            targetNiveau: 4,
            linkedResources: 'https://example.com/ts',
          }),
        );
      });
    });

    it('hides form when cancel is clicked', () => {
      setupStores(true);
      mockUseQuery.mockReturnValue({ data: [], isLoading: false });

      render(<Goals />);
      fireEvent.click(screen.getByRole('button', { name: 'goals.assignGoal' }));
      expect(screen.getByLabelText('goals.skillName')).toBeInTheDocument();

      fireEvent.click(screen.getByRole('button', { name: 'common.cancel' }));
      expect(screen.queryByLabelText('goals.skillName')).not.toBeInTheDocument();
    });
  });
});
