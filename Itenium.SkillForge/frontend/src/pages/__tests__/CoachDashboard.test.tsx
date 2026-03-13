import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
import type { ConsultantSummary, ReadinessFlagDto } from '@/api/client';
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

const mockUseQuery = vi.fn();
vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
}));

vi.mock('@/api/client', () => ({
  fetchCoachDashboard: vi.fn(),
  fetchReadinessFlags: vi.fn(),
}));

// eslint-disable-next-line import-x/order -- must come after vi.mock calls
import CoachDashboard from '../CoachDashboard';

const consultant = (overrides: Partial<ConsultantSummary> = {}): ConsultantSummary => ({
  id: 'id1',
  firstName: 'Alice',
  lastName: 'Smith',
  email: 'alice@test.local',
  teams: [1],
  lastActivityAt: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(),
  isInactive: false,
  activeGoalCount: 0,
  isReady: false,
  ...overrides,
});

describe('CoachDashboard', () => {
  beforeEach(() => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: false });
  });

  test('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<CoachDashboard />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  test('shows empty state when no consultants', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('dashboard.noConsultants')).toBeInTheDocument();
  });

  test('renders consultant name', () => {
    mockUseQuery.mockReturnValue({ data: [consultant()], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
  });

  test('renders consultant email', () => {
    mockUseQuery.mockReturnValue({ data: [consultant()], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('alice@test.local')).toBeInTheDocument();
  });

  test('shows inactive warning for inactive consultant', () => {
    const inactive = consultant({ isInactive: true, lastActivityAt: null });
    mockUseQuery.mockReturnValue({ data: [inactive], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('dashboard.inactive')).toBeInTheDocument();
  });

  test('does not show inactive warning for active consultant', () => {
    mockUseQuery.mockReturnValue({ data: [consultant({ isInactive: false })], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.queryByText('dashboard.inactive')).not.toBeInTheDocument();
  });

  test('shows goal count badge', () => {
    mockUseQuery.mockReturnValue({ data: [consultant({ activeGoalCount: 3 })], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText(/dashboard\.goals/)).toBeInTheDocument();
  });

  test('query uses fetchCoachDashboard', () => {
    render(<CoachDashboard />);
    expect(mockUseQuery).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ['coach-dashboard'] }));
  });

  test('shows readiness flags section when flags exist', () => {
    const flag: ReadinessFlagDto = {
      goalId: 1,
      skillName: 'Java Basics',
      consultantId: 'user-1',
      raisedAt: new Date(Date.now() - 2 * 86400000).toISOString(),
      ageDays: 2,
    };
    mockUseQuery
      .mockReturnValueOnce({ data: [], isLoading: false })
      .mockReturnValueOnce({ data: [flag], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('Java Basics')).toBeInTheDocument();
  });

  test('shows flag age indicator', () => {
    const flag: ReadinessFlagDto = {
      goalId: 1,
      skillName: 'Java Basics',
      consultantId: 'user-1',
      raisedAt: new Date(Date.now() - 5 * 86400000).toISOString(),
      ageDays: 5,
    };
    mockUseQuery
      .mockReturnValueOnce({ data: [], isLoading: false })
      .mockReturnValueOnce({ data: [flag], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText(/dashboard\.flagAge/)).toBeInTheDocument();
  });

  test('shows no flags message when no readiness flags', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: [], isLoading: false })
      .mockReturnValueOnce({ data: [], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.getByText('dashboard.noFlags')).toBeInTheDocument();
  });

  test('query uses fetchReadinessFlags', () => {
    render(<CoachDashboard />);
    expect(mockUseQuery).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ['readiness-flags'] }));
  });
});
