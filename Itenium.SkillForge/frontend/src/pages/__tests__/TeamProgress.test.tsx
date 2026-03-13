import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('@tanstack/react-router', () => ({}));

const mockUseQuery = vi.fn();

vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
}));

vi.mock('lucide-react', () => ({
  TrendingUp: () => <span data-testid="icon-trending-up" />,
  CheckCircle: () => <span data-testid="icon-check" />,
  Clock: () => <span data-testid="icon-clock" />,
  BookOpen: () => <span data-testid="icon-book" />,
}));

vi.mock('@/api/client', () => ({
  fetchTeamProgress: vi.fn(),
}));

vi.mock('@/lib/queryClient', () => ({
  queryClient: { clear: vi.fn() },
}));

vi.mock('@/stores', () => ({
  useTeamStore: () => ({ selectedTeam: { id: 1, name: 'Java' } }),
}));

// eslint-disable-next-line import-x/order
import { TeamProgress } from '../TeamProgress';

const progressData = [
  {
    userId: 'u1',
    fullName: 'Alice Smith',
    email: 'alice@example.com',
    enrollments: [
      { courseId: 1, courseName: 'C# Basics', status: 'Completed', enrolledAt: '2026-01-01', completedAt: '2026-02-01' },
      { courseId: 2, courseName: 'React', status: 'InProgress', enrolledAt: '2026-02-01', completedAt: null },
    ],
  },
  {
    userId: 'u2',
    fullName: 'Bob Jones',
    email: 'bob@example.com',
    enrollments: [
      { courseId: 1, courseName: 'C# Basics', status: 'Enrolled', enrolledAt: '2026-01-15', completedAt: null },
    ],
  },
  {
    userId: 'u3',
    fullName: 'Carol White',
    email: 'carol@example.com',
    enrollments: [],
  },
];

beforeEach(() => {
  vi.clearAllMocks();
  mockUseQuery.mockReturnValue({ data: progressData, isLoading: false });
});

describe('TeamProgress', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<TeamProgress />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows team name in title', () => {
    render(<TeamProgress />);
    expect(screen.getByText(/Java/)).toBeInTheDocument();
  });

  it('renders all team members', () => {
    render(<TeamProgress />);
    expect(screen.getAllByText('Alice Smith').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Bob Jones').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Carol White')).toBeInTheDocument(); // Carol has no enrollments, only in table
  });

  it('shows no members message when empty', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<TeamProgress />);
    expect(screen.getByText('teams.noMembers')).toBeInTheDocument();
  });

  it('shows correct completed count for each member', () => {
    render(<TeamProgress />);
    // Alice has 1 completed out of 2 total
    expect(screen.getByText('1 / 2')).toBeInTheDocument();
    // Bob has 0 completed out of 1 total
    expect(screen.getByText('0 / 1')).toBeInTheDocument();
  });

  it('shows no results for member with no enrollments', () => {
    render(<TeamProgress />);
    // Carol has 0 enrollments
    expect(screen.getByText('0 / 0')).toBeInTheDocument();
  });
});
