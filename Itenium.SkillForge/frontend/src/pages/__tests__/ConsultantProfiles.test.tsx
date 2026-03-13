/* eslint-disable import-x/order */
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';
/* eslint-enable import-x/order */
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
  useMutation: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
  useQueryClient: vi.fn(() => ({ invalidateQueries: vi.fn() })),
}));

vi.mock('@/api/client', () => ({
  fetchConsultants: vi.fn(),
  fetchUserTeams: vi.fn(),
  assignProfile: vi.fn(),
  removeProfile: vi.fn(),
}));

vi.mock('@itenium-forge/ui', () => {
  const S = ({ children }: { children?: React.ReactNode }) => <div>{children}</div>;
  return {
    Button: ({
      children,
      onClick,
      disabled,
    }: {
      children?: React.ReactNode;
      onClick?: () => void;
      disabled?: boolean;
    }) => (
      <button onClick={onClick} disabled={disabled}>
        {children}
      </button>
    ),
    Select: S,
    SelectContent: S,
    SelectItem: ({ children }: { children?: React.ReactNode }) => <div>{children}</div>,
    SelectTrigger: S,
    SelectValue: () => <span />,
  };
});

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ConsultantProfiles } from '../ConsultantProfiles';

const mockUseQuery = useQuery as ReturnType<typeof vi.fn>;
const mockUseMutation = useMutation as ReturnType<typeof vi.fn>;
const mockUseQueryClient = useQueryClient as ReturnType<typeof vi.fn>;

describe('ConsultantProfiles', () => {
  beforeEach(() => {
    mockUseMutation.mockReturnValue({ mutate: vi.fn(), isPending: false });
    mockUseQueryClient.mockReturnValue({ invalidateQueries: vi.fn() });
  });

  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<ConsultantProfiles />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('renders consultant list with assigned profile', () => {
    mockUseQuery
      .mockReturnValueOnce({
        data: [{ userId: 'user-1', teamId: 2, teamName: '.NET', firstName: 'John', lastName: 'Doe' }],
        isLoading: false,
      })
      .mockReturnValueOnce({
        data: [{ id: 2, name: '.NET' }],
        isLoading: false,
      });

    render(<ConsultantProfiles />);

    expect(screen.getByText('profile.title')).toBeInTheDocument();
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('.NET')).toBeInTheDocument();
  });

  it('shows empty state message when no consultants', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: [], isLoading: false })
      .mockReturnValueOnce({ data: [], isLoading: false });

    render(<ConsultantProfiles />);

    expect(screen.getByText('profile.noConsultants')).toBeInTheDocument();
  });

  it('shows no profile text when consultant has no assignment', () => {
    mockUseQuery
      .mockReturnValueOnce({
        data: [{ userId: 'user-2', teamId: null, teamName: null, firstName: 'Jane', lastName: 'Smith' }],
        isLoading: false,
      })
      .mockReturnValueOnce({ data: [], isLoading: false });

    render(<ConsultantProfiles />);

    expect(screen.getByText('profile.noProfile')).toBeInTheDocument();
  });
});
