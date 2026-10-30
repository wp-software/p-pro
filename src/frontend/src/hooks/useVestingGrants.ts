import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { vestingGrantService } from '@/services/vestingService';
import type { VestingGrantFilters, CreateVestingGrantRequest, ExerciseSharesRequest } from '@/types';
import toast from 'react-hot-toast';

const QUERY_KEY = ['vesting-grants'];

export function useVestingGrants(filters?: VestingGrantFilters) {
  return useQuery({
    queryKey: [...QUERY_KEY, filters],
    queryFn: () => vestingGrantService.getGrants(filters),
    staleTime: 30000,
  });
}

export function useMyVestingGrants(companyId?: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, 'me', companyId],
    queryFn: () => vestingGrantService.getMyGrants(companyId),
    staleTime: 30000,
  });
}

export function useVestingGrantsByShareholder(shareholderId: string, companyId?: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, 'by-shareholder', shareholderId, companyId],
    queryFn: () => vestingGrantService.getGrantsByShareholder(shareholderId, companyId),
    staleTime: 30000,
    enabled: !!shareholderId,
  });
}

export function useVestingGrant(id: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, id],
    queryFn: () => vestingGrantService.getGrantById(id),
    enabled: !!id,
  });
}

export function useVestingCalculation(id: string, asOfDate?: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, id, 'calculate', asOfDate],
    queryFn: () => vestingGrantService.calculateVesting(id, asOfDate),
    enabled: !!id,
    staleTime: 60000,
  });
}

export function useVestingProjection(id: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, id, 'projection'],
    queryFn: () => vestingGrantService.getProjection(id),
    enabled: !!id,
    staleTime: 60000,
  });
}

export function useVestingTransactions(grantId: string, page = 1, pageSize = 10) {
  return useQuery({
    queryKey: [...QUERY_KEY, grantId, 'transactions', page, pageSize],
    queryFn: () => vestingGrantService.getTransactions(grantId, page, pageSize),
    enabled: !!grantId,
  });
}

export function useCreateVestingGrant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateVestingGrantRequest) => vestingGrantService.createGrant(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Grant de vesting criado com sucesso');
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        error.response?.data?.errors?.[0] ||
        'Erro ao criar grant de vesting';
      toast.error(message);
    },
  });
}

export function useApproveVestingGrant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => vestingGrantService.approveGrant(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Grant aprovado com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao aprovar grant');
    },
  });
}

export function useActivateVestingGrant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => vestingGrantService.activateGrant(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Grant ativado com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao ativar grant');
    },
  });
}

export function useCancelVestingGrant() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => vestingGrantService.cancelGrant(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Grant cancelado');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao cancelar grant');
    },
  });
}

export function useExerciseShares() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ExerciseSharesRequest }) =>
      vestingGrantService.exerciseShares(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: [...QUERY_KEY, variables.id] });
      toast.success('Ações exercidas com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao exercer ações');
    },
  });
}
