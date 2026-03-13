import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
import type { GoalDto } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

const mockUseQuery = vi.fn();
vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
}));

vi.mock('@/api/client', () => ({ fetchGoals: vi.fn() }));

// eslint-disable-next-line import-x/order -- must come after vi.mock calls
import Goals from '../Goals';

const goal = (overrides: Partial<GoalDto> = {}): GoalDto => ({
  id: 1,
  skillName: 'Java & JVM',
  currentLevel: 1,
  targetLevel: 3,
  deadline: '2026-06-01T00:00:00Z',
  resources: [],
  ...overrides,
});

beforeEach(() => {
  mockUseQuery.mockReturnValue({ data: [], isLoading: false });
});

describe('Goals', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: true });
    render(<Goals />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows empty message when no goals', () => {
    render(<Goals />);
    expect(screen.getByText('goals.noGoals')).toBeInTheDocument();
  });

  it('renders skill name', () => {
    mockUseQuery.mockReturnValue({ data: [goal()], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('Java & JVM')).toBeInTheDocument();
  });

  it('renders current and target niveau', () => {
    mockUseQuery.mockReturnValue({ data: [goal({ currentLevel: 2, targetLevel: 5 })], isLoading: false });
    render(<Goals />);
    expect(screen.getByText(/2 → 5/)).toBeInTheDocument();
  });

  it('renders deadline', () => {
    mockUseQuery.mockReturnValue({ data: [goal({ deadline: '2026-06-01T00:00:00Z' })], isLoading: false });
    render(<Goals />);
    expect(screen.getByText(/2026/)).toBeInTheDocument();
  });

  it('renders resource links', () => {
    mockUseQuery.mockReturnValue({
      data: [
        goal({
          resources: [{ id: 1, title: 'Effective Java', url: 'https://example.com', type: 'book' }],
        }),
      ],
      isLoading: false,
    });
    render(<Goals />);
    expect(screen.getByText('Effective Java')).toBeInTheDocument();
  });

  it('shows no resources message when goal has none', () => {
    mockUseQuery.mockReturnValue({ data: [goal({ resources: [] })], isLoading: false });
    render(<Goals />);
    expect(screen.getByText('goals.noResources')).toBeInTheDocument();
  });
});
