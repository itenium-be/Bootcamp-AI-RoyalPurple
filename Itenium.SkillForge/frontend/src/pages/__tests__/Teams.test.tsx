import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
import type { UserDto } from '@/api/client';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

const mockUseQuery = vi.fn();
const mockUseMutation = vi.fn();
vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
  useMutation: (...args: unknown[]) => mockUseMutation(...args),
  useQueryClient: () => ({ invalidateQueries: vi.fn() }),
}));

vi.mock('@/api/client', () => ({
  fetchUserTeams: vi.fn(),
  fetchUsers: vi.fn(),
  createTeam: vi.fn(),
  updateTeam: vi.fn(),
  deleteTeam: vi.fn(),
}));

vi.mock('@/stores', () => ({
  useAuthStore: () => ({ user: { role: 'backoffice' } }),
}));

// eslint-disable-next-line import-x/order -- must come after vi.mock calls
import { TeamsPage } from '../TeamsPage';

const team = (id: number, name: string) => ({ id, name });

const user = (overrides: Partial<UserDto> = {}): UserDto => ({
  id: '1',
  userName: 'jdoe',
  email: 'j@example.com',
  firstName: 'John',
  lastName: 'Doe',
  role: 'learner',
  teams: [1],
  ...overrides,
});

beforeEach(() => {
  mockUseMutation.mockReturnValue({ mutate: vi.fn(), isPending: false });
  mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
    if (queryKey[0] === 'teams') return { data: [team(1, 'Java'), team(2, 'PO-Analysis')], isLoading: false };
    if (queryKey[0] === 'users') return { data: [], isLoading: false };
    return { data: [], isLoading: false };
  });
});

describe('TeamsPage – team members', () => {
  it('shows member count for each team', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
      if (queryKey[0] === 'teams') return { data: [team(1, 'Java')], isLoading: false };
      if (queryKey[0] === 'users')
        return {
          data: [user({ id: '1', firstName: 'Alice', lastName: 'Smith', teams: [1] })],
          isLoading: false,
        };
      return { data: [], isLoading: false };
    });
    render(<TeamsPage />);
    const btn = screen.getByRole('button', { name: /teams.members/i });
    expect(btn).toHaveTextContent('1');
  });

  it('expands team row to show member names on click', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
      if (queryKey[0] === 'teams') return { data: [team(1, 'Java')], isLoading: false };
      if (queryKey[0] === 'users')
        return {
          data: [
            user({ id: '1', firstName: 'Alice', lastName: 'Smith', teams: [1] }),
            user({ id: '2', firstName: 'Bob', lastName: 'Jones', teams: [1] }),
          ],
          isLoading: false,
        };
      return { data: [], isLoading: false };
    });
    render(<TeamsPage />);
    fireEvent.click(screen.getByRole('button', { name: /teams.members/i }));
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByText('Bob Jones')).toBeInTheDocument();
  });

  it('hides member names again when collapse button is clicked', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
      if (queryKey[0] === 'teams') return { data: [team(1, 'Java')], isLoading: false };
      if (queryKey[0] === 'users')
        return { data: [user({ id: '1', firstName: 'Alice', lastName: 'Smith', teams: [1] })], isLoading: false };
      return { data: [], isLoading: false };
    });
    render(<TeamsPage />);
    const toggleBtn = screen.getByRole('button', { name: /teams.members/i });
    fireEvent.click(toggleBtn);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    fireEvent.click(toggleBtn);
    expect(screen.queryByText('Alice Smith')).not.toBeInTheDocument();
  });

  it('shows no-members message when team has no members', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
      if (queryKey[0] === 'teams') return { data: [team(1, 'Java')], isLoading: false };
      if (queryKey[0] === 'users') return { data: [], isLoading: false };
      return { data: [], isLoading: false };
    });
    render(<TeamsPage />);
    fireEvent.click(screen.getByRole('button', { name: /teams.members/i }));
    expect(screen.getByText('teams.noMembers')).toBeInTheDocument();
  });

  it('does not show members from other teams', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
      if (queryKey[0] === 'teams') return { data: [team(1, 'Java'), team(2, 'PO-Analysis')], isLoading: false };
      if (queryKey[0] === 'users')
        return {
          data: [
            user({ id: '1', firstName: 'Alice', lastName: 'Smith', teams: [1] }),
            user({ id: '2', firstName: 'Bob', lastName: 'Jones', teams: [2] }),
          ],
          isLoading: false,
        };
      return { data: [], isLoading: false };
    });
    render(<TeamsPage />);
    // expand first team row (Java)
    const toggleBtns = screen.getAllByRole('button', { name: /teams.members/i });
    fireEvent.click(toggleBtns[0]);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.queryByText('Bob Jones')).not.toBeInTheDocument();
  });
});
