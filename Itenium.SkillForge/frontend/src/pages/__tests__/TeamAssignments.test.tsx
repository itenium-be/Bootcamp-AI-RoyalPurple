import { render, screen, fireEvent } from '@testing-library/react';
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
const mockUseMutation = vi.fn();

vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
  useMutation: (...args: unknown[]) => mockUseMutation(...args),
  useQueryClient: () => ({ invalidateQueries: vi.fn() }),
}));

vi.mock('lucide-react', () => ({
  ClipboardList: () => <span data-testid="icon-clipboard" />,
  BookOpen: () => <span data-testid="icon-book" />,
  CheckCircle2: () => <span data-testid="icon-check" />,
  AlertCircle: () => <span data-testid="icon-alert" />,
}));

vi.mock('@itenium-forge/ui', () => ({
  Badge: ({ children }: { children: React.ReactNode }) => <span>{children}</span>,
  Button: ({ children, onClick }: { children: React.ReactNode; onClick?: () => void }) => (
    <button onClick={onClick}>{children}</button>
  ),
}));

vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

vi.mock('@/api/client', () => ({
  fetchTeamAssignments: vi.fn(),
  fetchCourses: vi.fn(),
  fetchTeamMembers: vi.fn(),
  assignCourse: vi.fn(),
  unassignCourse: vi.fn(),
  updateAssignment: vi.fn(),
}));

vi.mock('@/lib/queryClient', () => ({
  queryClient: { clear: vi.fn() },
}));

vi.mock('@/stores', () => ({
  useTeamStore: () => ({ selectedTeam: { id: 1, name: 'Java' } }),
}));

// eslint-disable-next-line import-x/order
import { TeamAssignments } from '../TeamAssignments';

const teamAssignments = [
  { courseId: 1, courseName: 'C# Basics', isMandatory: true, assignedAt: '2026-01-01T00:00:00Z', userId: null, userFullName: null },
  { courseId: 2, courseName: 'React', isMandatory: false, assignedAt: '2026-01-02T00:00:00Z', userId: null, userFullName: null },
];

const individualAssignment = {
  courseId: 3, courseName: 'Angular', isMandatory: true, assignedAt: '2026-01-03T00:00:00Z',
  userId: 'u1', userFullName: 'Alice Smith',
};

const courses = [
  { id: 1, name: 'C# Basics', status: 'Published', isMandatory: false },
  { id: 2, name: 'React', status: 'Published', isMandatory: false },
  { id: 3, name: 'Angular', status: 'Published', isMandatory: false },
  { id: 4, name: 'Draft Course', status: 'Draft', isMandatory: false },
];

const members = [
  { id: 'u1', username: 'alice', email: 'alice@test.com', firstName: 'Alice', lastName: 'Smith', roles: [], isActive: true, teams: [1] },
  { id: 'u2', username: 'bob', email: 'bob@test.com', firstName: 'Bob', lastName: 'Jones', roles: [], isActive: true, teams: [1] },
];

const noopMutation = { mutate: vi.fn(), isPending: false };

beforeEach(() => {
  vi.clearAllMocks();
  mockUseMutation.mockReturnValue(noopMutation);
  mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
    if (queryKey[0] === 'team-assignments') return { data: teamAssignments, isLoading: false };
    if (queryKey[0] === 'courses') return { data: courses, isLoading: false };
    if (queryKey[0] === 'team-members') return { data: members, isLoading: false };
    return { data: undefined, isLoading: false };
  });
});

describe('TeamAssignments', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<TeamAssignments />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows team name in title', () => {
    render(<TeamAssignments />);
    expect(screen.getByText(/Java/)).toBeInTheDocument();
  });

  it('renders assigned courses', () => {
    render(<TeamAssignments />);
    expect(screen.getByText('C# Basics')).toBeInTheDocument();
    expect(screen.getByText('React')).toBeInTheDocument();
  });

  it('shows mandatory badge for mandatory assignments', () => {
    render(<TeamAssignments />);
    expect(screen.getByText('assignments.mandatory')).toBeInTheDocument();
  });

  it('shows optional badge for optional assignments', () => {
    render(<TeamAssignments />);
    expect(screen.getByText('assignments.optional')).toBeInTheDocument();
  });

  it('shows "entire team" label for team-wide assignments', () => {
    render(<TeamAssignments />);
    expect(screen.getAllByText('assignments.entireTeam').length).toBeGreaterThanOrEqual(1);
  });

  it('shows member name for individual assignments', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
      if (queryKey[0] === 'team-assignments') return { data: [individualAssignment], isLoading: false };
      if (queryKey[0] === 'courses') return { data: courses, isLoading: false };
      if (queryKey[0] === 'team-members') return { data: members, isLoading: false };
      return { data: undefined, isLoading: false };
    });
    render(<TeamAssignments />);
    expect(screen.getAllByText('Alice Smith').length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no assignments', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: string[] }) => {
      if (queryKey[0] === 'team-assignments') return { data: [], isLoading: false };
      if (queryKey[0] === 'courses') return { data: courses, isLoading: false };
      if (queryKey[0] === 'team-members') return { data: members, isLoading: false };
      return { data: undefined, isLoading: false };
    });
    render(<TeamAssignments />);
    expect(screen.getByText('assignments.noAssignments')).toBeInTheDocument();
  });

  it('only shows published unassigned courses in available section', () => {
    render(<TeamAssignments />);
    // Angular is published and not team-wide assigned
    expect(screen.getByText('Angular')).toBeInTheDocument();
    // Draft Course should not appear
    expect(screen.queryByText('Draft Course')).not.toBeInTheDocument();
  });

  it('shows member picker dropdown in available courses section', () => {
    render(<TeamAssignments />);
    // The select should have member options
    expect(screen.getByDisplayValue('assignments.selectMember')).toBeInTheDocument();
  });

  it('shows individual assign buttons after selecting a member', () => {
    render(<TeamAssignments />);
    const select = screen.getByDisplayValue('assignments.selectMember');
    fireEvent.change(select, { target: { value: 'u1' } });
    // After selecting a member, individual assign buttons appear
    expect(screen.getAllByText('assignments.assignMandatory').length).toBeGreaterThan(1);
  });

  it('calls unassign mutation when unassign button clicked', () => {
    const mutate = vi.fn();
    mockUseMutation.mockReturnValue({ mutate, isPending: false });
    render(<TeamAssignments />);
    const unassignButtons = screen.getAllByText('assignments.unassign');
    fireEvent.click(unassignButtons[0]);
    expect(mutate).toHaveBeenCalled();
  });

  it('calls assign mutation when assign as mandatory clicked', () => {
    const mutate = vi.fn();
    mockUseMutation.mockReturnValue({ mutate, isPending: false });
    render(<TeamAssignments />);
    const assignButtons = screen.getAllByText('assignments.assignMandatory');
    fireEvent.click(assignButtons[0]);
    expect(mutate).toHaveBeenCalled();
  });
});
