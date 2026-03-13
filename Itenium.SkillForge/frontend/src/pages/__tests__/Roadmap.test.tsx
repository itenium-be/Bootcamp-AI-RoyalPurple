import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
import type { RoadmapNode } from '@/api/client';
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
  fetchRoadmap: vi.fn(),
}));

// eslint-disable-next-line import-x/order -- must come after vi.mock calls
import Roadmap from '../Roadmap';

const node = (overrides: Partial<RoadmapNode> = {}): RoadmapNode => ({
  id: 1,
  name: 'Java Basics',
  description: null,
  tier: 1,
  teamId: 1,
  ...overrides,
});

beforeEach(() => {
  mockUseQuery.mockReturnValue({ data: [], isLoading: false });
});

describe('Roadmap', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: true });
    render(<Roadmap />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows empty message when no roadmap nodes', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<Roadmap />);
    expect(screen.getByText('roadmap.noRoadmap')).toBeInTheDocument();
  });

  it('renders skill names from API response', () => {
    mockUseQuery.mockReturnValue({
      data: [node({ id: 1, name: 'Java Basics', tier: 1 }), node({ id: 2, name: 'Spring Boot', tier: 2 })],
      isLoading: false,
    });
    render(<Roadmap />);
    expect(screen.getByText('Java Basics')).toBeInTheDocument();
    expect(screen.getByText('Spring Boot')).toBeInTheDocument();
  });

  it('renders skill description when present', () => {
    mockUseQuery.mockReturnValue({
      data: [node({ description: 'Learn core Java syntax' })],
      isLoading: false,
    });
    render(<Roadmap />);
    expect(screen.getByText('Learn core Java syntax')).toBeInTheDocument();
  });

  it('shows tier labels as section headings', () => {
    mockUseQuery.mockReturnValue({
      data: [node({ id: 1, tier: 1 }), node({ id: 2, name: 'Spring Boot', tier: 2 })],
      isLoading: false,
    });
    render(<Roadmap />);
    expect(screen.getByText('roadmap.tier1')).toBeInTheDocument();
    expect(screen.getByText('roadmap.tier2')).toBeInTheDocument();
  });

  it('shows Show All button when data is present', () => {
    mockUseQuery.mockReturnValue({
      data: [node()],
      isLoading: false,
    });
    render(<Roadmap />);
    expect(screen.getByText('roadmap.showAll')).toBeInTheDocument();
  });

  it('hides Show All button after clicking it', () => {
    mockUseQuery.mockReturnValue({
      data: [node()],
      isLoading: false,
    });
    render(<Roadmap />);
    fireEvent.click(screen.getByText('roadmap.showAll'));
    expect(screen.queryByText('roadmap.showAll')).not.toBeInTheDocument();
  });

  it('does not show Show All button when list is empty', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<Roadmap />);
    expect(screen.queryByText('roadmap.showAll')).not.toBeInTheDocument();
  });

  it('passes showAll=false to query by default', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<Roadmap />);
    const callArgs = mockUseQuery.mock.calls[0][0];
    expect(callArgs.queryKey).toContain(false);
  });

  it('passes showAll=true to query after clicking Show All', () => {
    mockUseQuery.mockReturnValue({ data: [node()], isLoading: false });
    render(<Roadmap />);
    fireEvent.click(screen.getByText('roadmap.showAll'));
    const calls = mockUseQuery.mock.calls;
    const lastCallArgs = calls[calls.length - 1][0];
    expect(lastCallArgs.queryKey).toContain(true);
  });
});
