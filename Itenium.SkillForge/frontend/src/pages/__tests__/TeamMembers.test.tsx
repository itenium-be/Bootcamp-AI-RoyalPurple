import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('sonner', () => ({ toast: { success: vi.fn() } }));

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to }: { children?: React.ReactNode; to?: string }) => <a href={to}>{children}</a>,
}));

const mockUseQuery = vi.fn();
const mockUseMutation = vi.fn();
const mockInvalidateQueries = vi.fn();

vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
  useMutation: (...args: unknown[]) => mockUseMutation(...args),
  useQueryClient: () => ({ invalidateQueries: mockInvalidateQueries }),
}));

vi.mock('@itenium-forge/ui', () => ({
  Button: ({ children, onClick, disabled }: {
    children?: React.ReactNode;
    onClick?: () => void;
    disabled?: boolean;
  }) => <button onClick={onClick} disabled={disabled}>{children}</button>,
  Badge: ({ children }: { children?: React.ReactNode }) => <span>{children}</span>,
}));

vi.mock('lucide-react', () => ({
  ArrowLeft: () => <span data-testid="icon-arrow-left" />,
  UserMinus: () => <span data-testid="icon-user-minus" />,
  UserPlus: () => <span data-testid="icon-user-plus" />,
  Users: () => <span data-testid="icon-users" />,
}));

vi.mock('@/api/client', () => ({
  fetchTeamMembers: vi.fn(),
  fetchUsers: vi.fn(),
  fetchUserTeams: vi.fn(),
  addTeamMember: vi.fn(),
  removeTeamMember: vi.fn(),
}));

vi.mock('@/lib/queryClient', () => ({
  queryClient: { clear: vi.fn() },
}));

// eslint-disable-next-line import-x/order
import { TeamMembers } from '../TeamMembers';

const members = [
  { id: 'u1', username: 'alice', firstName: 'Alice', lastName: 'Smith', email: 'alice@example.com', roles: ['learner'], isActive: true, teams: [1] },
  { id: 'u2', username: 'bob', firstName: 'Bob', lastName: 'Jones', email: 'bob@example.com', roles: ['learner'], isActive: true, teams: [1] },
];

const allUsers = [
  ...members,
  { id: 'u3', username: 'carol', firstName: 'Carol', lastName: 'White', email: 'carol@example.com', roles: [], isActive: true, teams: [] },
];

const teams = [
  { id: 1, name: 'Java' },
  { id: 2, name: '.NET' },
];

function setupQueries(overrides: { members?: typeof members | null; loading?: boolean } = {}) {
  const memberData = overrides.members !== undefined ? overrides.members : members;
  mockUseQuery.mockImplementation(({ queryKey }: { queryKey: unknown[] }) => {
    if (queryKey[0] === 'team-members') return { data: memberData ?? [], isLoading: overrides.loading ?? false };
    if (queryKey[0] === 'users') return { data: allUsers };
    if (queryKey[0] === 'teams') return { data: teams };
    return { data: [] };
  });
}

function setupMutations() {
  mockUseMutation.mockReturnValue({ mutate: vi.fn(), isPending: false });
}

beforeEach(() => {
  vi.clearAllMocks();
  setupQueries();
  setupMutations();
});

describe('TeamMembers', () => {
  it('shows loading state', () => {
    setupQueries({ loading: true });
    render(<TeamMembers teamId={1} />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows team name in header when resolved from teams query', () => {
    render(<TeamMembers teamId={1} />);
    expect(screen.getByText(/Java/)).toBeInTheDocument();
  });

  it('uses teamName prop when provided (manager view)', () => {
    render(<TeamMembers teamId={1} teamName="Custom Team" />);
    expect(screen.getByText(/Custom Team/)).toBeInTheDocument();
  });

  it('renders all members', () => {
    render(<TeamMembers teamId={1} />);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByText('Bob Jones')).toBeInTheDocument();
  });

  it('shows no members message when list is empty', () => {
    setupQueries({ members: [] });
    render(<TeamMembers teamId={1} />);
    expect(screen.getByText('teams.noMembers')).toBeInTheDocument();
  });

  it('shows back link by default', () => {
    render(<TeamMembers teamId={1} />);
    expect(screen.getByTestId('icon-arrow-left')).toBeInTheDocument();
  });

  it('hides back link when hideManage=true', () => {
    render(<TeamMembers teamId={1} teamName="Java" hideManage />);
    expect(screen.queryByTestId('icon-arrow-left')).not.toBeInTheDocument();
  });

  it('shows add member section by default', () => {
    render(<TeamMembers teamId={1} />);
    expect(screen.getAllByText('teams.addMember').length).toBeGreaterThanOrEqual(1);
  });

  it('hides add member section when hideManage=true', () => {
    render(<TeamMembers teamId={1} teamName="Java" hideManage />);
    expect(screen.queryByText('teams.addMember')).not.toBeInTheDocument();
  });

  it('shows remove buttons for each member', () => {
    render(<TeamMembers teamId={1} />);
    expect(screen.getAllByTestId('icon-user-minus')).toHaveLength(2);
  });

  it('hides remove buttons when hideManage=true', () => {
    render(<TeamMembers teamId={1} teamName="Java" hideManage />);
    expect(screen.queryByTestId('icon-user-minus')).not.toBeInTheDocument();
  });

  it('shows remove confirmation when remove button clicked', () => {
    render(<TeamMembers teamId={1} />);
    const removeButtons = screen.getAllByTestId('icon-user-minus');
    fireEvent.click(removeButtons[0].closest('button')!);
    expect(screen.getByText('teams.removeMember')).toBeInTheDocument();
  });

  it('only shows non-members in add dropdown', () => {
    render(<TeamMembers teamId={1} />);
    // Carol is not a member, so she should appear in the select
    expect(screen.getByText(/Carol/)).toBeInTheDocument();
    // Alice and Bob are already members, should not appear in dropdown
    const options = screen.queryAllByRole('option');
    const aliceOption = options.find(o => o.textContent?.includes('Alice'));
    expect(aliceOption).toBeUndefined();
  });
});
