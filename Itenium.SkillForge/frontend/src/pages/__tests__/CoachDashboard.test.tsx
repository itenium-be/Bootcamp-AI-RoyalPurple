import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
import type { ConsultantSummary } from '@/api/client';
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
  readinessFlagAgeInDays: null,
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

  test('shows readiness flag badge when consultant is ready', () => {
    mockUseQuery.mockReturnValue({
      data: [consultant({ isReady: true, readinessFlagAgeInDays: 2 })],
      isLoading: false,
    });
    render(<CoachDashboard />);
    expect(screen.getByText('dashboard.ready')).toBeInTheDocument();
  });

  test('shows readiness flag age when flag is raised today', () => {
    mockUseQuery.mockReturnValue({
      data: [consultant({ isReady: true, readinessFlagAgeInDays: 0 })],
      isLoading: false,
    });
    render(<CoachDashboard />);
    expect(screen.getByText(/dashboard\.readinessFlagAge/)).toBeInTheDocument();
  });

  test('does not show readiness flag badge when consultant is not ready', () => {
    mockUseQuery.mockReturnValue({ data: [consultant({ isReady: false })], isLoading: false });
    render(<CoachDashboard />);
    expect(screen.queryByText('dashboard.ready')).not.toBeInTheDocument();
  });
});
