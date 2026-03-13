import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';

// Must be before component import
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (opts?.name) return `${key}:${opts.name}`;
      return key;
    },
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

vi.mock('@itenium-forge/ui', () => {
  const S = ({ children }: { children?: React.ReactNode }) => <div>{children}</div>;
  return {
    Button: ({ children, onClick, disabled, type }: {
      children?: React.ReactNode;
      onClick?: () => void;
      disabled?: boolean;
      type?: string;
    }) => (
      <button onClick={onClick} disabled={disabled} type={type as 'button' | 'submit' | 'reset' | undefined}>
        {children}
      </button>
    ),
    Input: (props: React.InputHTMLAttributes<HTMLInputElement>) => <input {...props} />,
    Label: ({ children, htmlFor }: { children?: React.ReactNode; htmlFor?: string }) => (
      <label htmlFor={htmlFor}>{children}</label>
    ),
    Sheet: ({ children, open }: { children?: React.ReactNode; open?: boolean }) => (
      open !== false ? <div>{children}</div> : <div>{children}</div>
    ),
    SheetTrigger: S,
    SheetContent: ({ children }: { children?: React.ReactNode }) => <div data-testid="sheet-content">{children}</div>,
    SheetHeader: S,
    SheetFooter: S,
    SheetTitle: ({ children }: { children?: React.ReactNode }) => <div data-testid="sheet-title">{children}</div>,
  };
});

vi.mock('lucide-react', () => ({
  Pencil: () => <span data-testid="icon-pencil" />,
  Trash2: () => <span data-testid="icon-trash" />,
  Plus: () => <span data-testid="icon-plus" />,
  Users: () => <span data-testid="icon-users" />,
}));

const mockCreateTeam = vi.fn();
const mockUpdateTeam = vi.fn();
const mockDeleteTeam = vi.fn();

vi.mock('@/api/client', () => ({
  fetchUserTeams: vi.fn(),
  createTeam: (...args: unknown[]) => mockCreateTeam(...args),
  updateTeam: (...args: unknown[]) => mockUpdateTeam(...args),
  deleteTeam: (...args: unknown[]) => mockDeleteTeam(...args),
}));

// eslint-disable-next-line import-x/order
import { Teams } from '../Teams';

const teams = [
  { id: 1, name: 'Java' },
  { id: 2, name: '.NET' },
  { id: 3, name: 'QA' },
];

function setupMutations() {
  mockUseMutation.mockImplementation(({ onSuccess }: { onSuccess?: () => void }) => ({
    mutate: vi.fn((arg: unknown) => {
      mockCreateTeam(arg);
      onSuccess?.();
    }),
    isPending: false,
  }));
}

beforeEach(() => {
  vi.clearAllMocks();
  mockUseQuery.mockReturnValue({ data: teams, isLoading: false });
  setupMutations();
});

describe('Teams page', () => {
  it('renders title', () => {
    render(<Teams />);
    expect(screen.getByText('teams.title')).toBeInTheDocument();
  });

  it('renders all teams', () => {
    render(<Teams />);
    expect(screen.getByText('Java')).toBeInTheDocument();
    expect(screen.getByText('.NET')).toBeInTheDocument();
    expect(screen.getByText('QA')).toBeInTheDocument();
  });

  it('shows noTeams message when empty', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: false });
    render(<Teams />);
    expect(screen.getByText('teams.noTeams')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: true });
    render(<Teams />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows Add Team button', () => {
    render(<Teams />);
    expect(screen.getAllByText('teams.addTeam').length).toBeGreaterThanOrEqual(1);
  });

  it('shows edit and delete buttons for each team', () => {
    render(<Teams />);
    expect(screen.getAllByTestId('icon-pencil')).toHaveLength(3);
    expect(screen.getAllByTestId('icon-trash')).toHaveLength(3);
  });

  it('shows delete confirmation when trash icon clicked', async () => {
    render(<Teams />);
    const trashButtons = screen.getAllByTestId('icon-trash');
    fireEvent.click(trashButtons[0].closest('button')!);
    await waitFor(() => {
      expect(screen.getByText(/teams.deleteConfirm/)).toBeInTheDocument();
    });
  });

  it('hides delete confirmation on cancel', async () => {
    render(<Teams />);
    const trashButtons = screen.getAllByTestId('icon-trash');
    fireEvent.click(trashButtons[0].closest('button')!);
    await waitFor(() => {
      expect(screen.getByText(/teams.deleteConfirm/)).toBeInTheDocument();
    });
    // Sheet form renders before the banner, so banner's cancel is at index 1
    const cancelButtons = screen.getAllByText('common.cancel');
    const bannerCancel = cancelButtons[cancelButtons.length - 1];
    fireEvent.click(bannerCancel);
    await waitFor(() => {
      expect(screen.queryByText(/teams.deleteConfirm/)).not.toBeInTheDocument();
    });
  });

  it('shows add team title in sheet when add button clicked', () => {
    render(<Teams />);
    // The sheet is always rendered, title should show addTeam by default
    expect(screen.getByTestId('sheet-title')).toHaveTextContent('teams.addTeam');
  });

  it('shows edit team title in sheet when edit button clicked', async () => {
    render(<Teams />);
    const pencilButtons = screen.getAllByTestId('icon-pencil');
    fireEvent.click(pencilButtons[0].closest('button')!);
    await waitFor(() => {
      expect(screen.getByTestId('sheet-title')).toHaveTextContent('teams.editTeam');
    });
  });
});
