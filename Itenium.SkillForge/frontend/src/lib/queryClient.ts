import { AxiosError } from 'axios';
import { QueryCache, QueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import i18n from '../i18n';
import { useAuthStore } from '../stores';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        // eslint-disable-next-line no-console
        if (import.meta.env.DEV) console.log({ failureCount, error });

        if (failureCount >= 0 && import.meta.env.DEV) return false;
        if (failureCount > 3 && import.meta.env.PROD) return false;

        return !(error instanceof AxiosError && [401, 403].includes(error.response?.status ?? 0));
      },
      refetchOnWindowFocus: import.meta.env.PROD,
      staleTime: 10 * 1000, // 10s
    },
    mutations: {
      onError: (error) => {
        if (error instanceof AxiosError) {
          const message = error.response?.data?.error_description || error.response?.data?.message || error.message;
          toast.error(message);
        }
      },
    },
  },
  queryCache: new QueryCache({
    onError: (error) => {
      if (error instanceof AxiosError) {
        if (error.response?.status === 401) {
          toast.error(i18n.t('errors.sessionExpired'));
          useAuthStore.getState().logout();
          queryClient.clear();
          window.location.href = '/sign-in';
        }
        if (error.response?.status === 500) {
          toast.error(i18n.t('errors.internalServerError'));
        }
      }
    },
  }),
});
