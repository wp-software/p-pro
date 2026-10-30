import { useQuery } from '@tanstack/react-query';
import { authService } from '@/services/authService';

/**
 * Indica se o usuário logado está vinculado a um sócio.
 * Consulta /auth/me para refletir vínculos feitos após o login.
 */
export function useShareholderLink() {
  const { data, isLoading } = useQuery({
    queryKey: ['auth', 'me', 'shareholder-link'],
    queryFn: () => authService.getMe(),
    staleTime: 0,
    refetchOnMount: 'always',
  });

  return {
    isLoading,
    isLinked: !!data?.shareholderId,
  };
}
