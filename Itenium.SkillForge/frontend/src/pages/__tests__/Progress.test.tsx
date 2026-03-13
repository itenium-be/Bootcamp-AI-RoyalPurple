import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
import type { Enrollment } from '@/api/client';

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
  fetchMyEnrollments: vi.fn(),
}));

// eslint-disable-next-line import-x/order -- must come after vi.mock calls
import { Progress } from '../Progress';

const enrollment = (overrides: Partial<Enrollment> = {}): Enrollment => ({
  id: 1,
  courseId: 10,
  courseName: 'C# Basics',
  enrolledAt: new Date().toISOString(),
  ...overrides,
});

describe('Progress', () => {
  beforeEach(() => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: false });
  });

  test('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<Progress />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  test('shows empty state when no enrollments', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<Progress />);
    expect(screen.getByText('progress.noEnrollments')).toBeInTheDocument();
  });

  test('renders course name', () => {
    mockUseQuery.mockReturnValue({ data: [enrollment()], isLoading: false });
    render(<Progress />);
    expect(screen.getByText('C# Basics')).toBeInTheDocument();
  });

  test('query uses fetchMyEnrollments', () => {
    render(<Progress />);
    expect(mockUseQuery).toHaveBeenCalledWith(expect.objectContaining({ queryKey: ['my-enrollments'] }));
  });
});
