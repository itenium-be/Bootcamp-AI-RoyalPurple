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
}));

vi.mock('@/api/client', () => ({
  fetchMyProfile: vi.fn(),
  fetchCourses: vi.fn(),
}));

import { useQuery } from '@tanstack/react-query';
import { MyProfile } from '../MyProfile';

const mockUseQuery = useQuery as ReturnType<typeof vi.fn>;

describe('MyProfile', () => {
  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: undefined, isLoading: true });
    render(<MyProfile />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows not assigned message when no profile', () => {
    mockUseQuery
      .mockReturnValueOnce({ data: undefined, isLoading: false })
      .mockReturnValueOnce({ data: [], isLoading: false });

    render(<MyProfile />);

    expect(screen.getByText('profile.myProfileTitle')).toBeInTheDocument();
    expect(screen.getByText('profile.notAssigned')).toBeInTheDocument();
  });

  it('shows assigned profile and filtered courses', () => {
    mockUseQuery
      .mockReturnValueOnce({
        data: { userId: 'user-1', teamId: 2, teamName: '.NET' },
        isLoading: false,
      })
      .mockReturnValueOnce({
        data: [{ id: 2, name: 'Advanced C#', category: 'Development', level: 'Advanced', teamId: 2 }],
        isLoading: false,
      });

    render(<MyProfile />);

    expect(screen.getByText('profile.myProfileTitle')).toBeInTheDocument();
    expect(screen.getByText('.NET')).toBeInTheDocument();
    expect(screen.getByText('Advanced C#')).toBeInTheDocument();
    expect(screen.getByText('Development')).toBeInTheDocument();
    expect(screen.getByText('Advanced')).toBeInTheDocument();
  });

  it('shows no courses message when profile has no matching courses', () => {
    mockUseQuery
      .mockReturnValueOnce({
        data: { userId: 'user-1', teamId: 4, teamName: 'QA' },
        isLoading: false,
      })
      .mockReturnValueOnce({ data: [], isLoading: false });

    render(<MyProfile />);

    expect(screen.getByText('QA')).toBeInTheDocument();
    expect(screen.getByText('courses.noCourses')).toBeInTheDocument();
  });
});
