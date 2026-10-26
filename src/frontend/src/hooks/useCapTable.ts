import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { capTableService } from '@/services/capTableService';
import { shareService } from '@/services/shareService';
import { AxiosError } from 'axios';
import type {
  ShareFilters,
  TransactionFilters,
  IssueSharesRequest,
  IssueSharesBatchRequest,
  TransferSharesRequest,
  CancelSharesRequest,
} from '@/types';
import toast from 'react-hot-toast';

// ============================================================================
// QUERY KEYS
// ============================================================================

const CAP_TABLE_KEY = ['capTable'];
const SHARES_KEY = ['shares'];
const TRANSACTIONS_KEY = ['transactions'];

// ============================================================================
// CAP TABLE HOOKS
// ============================================================================

/**
 * Hook para obter o Cap Table completo de uma empresa
 */
export function useCapTable(companyId: string | undefined) {
  return useQuery({
    queryKey: [...CAP_TABLE_KEY, companyId],
    queryFn: () => capTableService.getCapTable(companyId!),
    enabled: !!companyId,
    staleTime: 30000, // 30 segundos
  });
}

/**
 * Hook para obter o resumo do Cap Table por tipo de sócio
 */
export function useCapTableSummaryByType(companyId: string | undefined) {
  return useQuery({
    queryKey: [...CAP_TABLE_KEY, companyId, 'summary-by-type'],
    queryFn: () => capTableService.getSummaryByType(companyId!),
    enabled: !!companyId,
    staleTime: 30000,
  });
}

/**
 * Hook para obter o resumo do Cap Table por classe de ações
 */
export function useCapTableSummaryByClass(companyId: string | undefined) {
  return useQuery({
    queryKey: [...CAP_TABLE_KEY, companyId, 'summary-by-class'],
    queryFn: () => capTableService.getSummaryByClass(companyId!),
    enabled: !!companyId,
    staleTime: 30000,
  });
}

// ============================================================================
// SHARES HOOKS
// ============================================================================

/**
 * Hook para listar ações com paginação e filtros
 */
export function useShares(filters?: ShareFilters) {
  return useQuery({
    queryKey: [...SHARES_KEY, filters],
    queryFn: () => shareService.getShares(filters),
    staleTime: 30000,
  });
}

/**
 * Hook para obter uma participação específica
 */
export function useShare(id: string | undefined) {
  return useQuery({
    queryKey: [...SHARES_KEY, id],
    queryFn: () => shareService.getShareById(id!),
    enabled: !!id,
  });
}

/**
 * Hook para listar ações de um sócio específico
 */
export function useSharesByShareholder(shareholderId: string | undefined) {
  return useQuery({
    queryKey: [...SHARES_KEY, 'by-shareholder', shareholderId],
    queryFn: () => shareService.getSharesByShareholder(shareholderId!),
    enabled: !!shareholderId,
  });
}

/**
 * Hook para consultar saldo de ações
 */
export function useShareholderBalance(shareholderId: string | undefined, shareClassId: string | undefined) {
  return useQuery({
    queryKey: [...SHARES_KEY, 'balance', shareholderId, shareClassId],
    queryFn: () => shareService.getShareholderBalance(shareholderId!, shareClassId!),
    enabled: !!shareholderId && !!shareClassId,
  });
}

// ============================================================================
// TRANSACTIONS HOOKS
// ============================================================================

/**
 * Hook para listar transações com paginação e filtros
 */
export function useTransactions(filters?: TransactionFilters) {
  return useQuery({
    queryKey: [...TRANSACTIONS_KEY, filters],
    queryFn: () => shareService.getTransactions(filters),
    staleTime: 30000,
  });
}

/**
 * Hook para obter uma transação específica
 */
export function useTransaction(id: string | undefined) {
  return useQuery({
    queryKey: [...TRANSACTIONS_KEY, id],
    queryFn: () => shareService.getTransactionById(id!),
    enabled: !!id,
  });
}

/**
 * Hook para listar transações de um sócio específico
 */
export function useTransactionsByShareholder(shareholderId: string | undefined) {
  return useQuery({
    queryKey: [...TRANSACTIONS_KEY, 'by-shareholder', shareholderId],
    queryFn: () => shareService.getTransactionsByShareholder(shareholderId!),
    enabled: !!shareholderId,
  });
}

// ============================================================================
// MUTATIONS HOOKS
// ============================================================================

/**
 * Hook para emitir novas ações
 */
export function useIssueShares() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: IssueSharesRequest) => shareService.issueShares(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: SHARES_KEY });
      queryClient.invalidateQueries({ queryKey: TRANSACTIONS_KEY });
      queryClient.invalidateQueries({ queryKey: [...CAP_TABLE_KEY, variables.companyId] });
      toast.success('Ações emitidas com sucesso');
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        error.response?.data?.errors?.[0] ||
        'Erro ao emitir ações';
      toast.error(message);
    },
  });
}

/**
 * Hook para emitir ações para vários sócios em uma operação atômica
 */
export function useIssueSharesBatch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: IssueSharesBatchRequest) => shareService.issueSharesBatch(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: SHARES_KEY });
      queryClient.invalidateQueries({ queryKey: TRANSACTIONS_KEY });
      queryClient.invalidateQueries({ queryKey: [...CAP_TABLE_KEY, variables.companyId] });
      toast.success('Emissão em lote realizada com sucesso');
    },
    onError: (error: unknown) => {
      const responseData =
        error instanceof AxiosError
          ? (error.response?.data as { message?: string; errors?: string[] } | undefined)
          : undefined;
      toast.error(responseData?.message || responseData?.errors?.[0] || 'Erro ao emitir ações em lote');
    },
  });
}

/**
 * Hook para transferir ações entre sócios
 */
export function useTransferShares() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: TransferSharesRequest) => shareService.transferShares(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: SHARES_KEY });
      queryClient.invalidateQueries({ queryKey: TRANSACTIONS_KEY });
      queryClient.invalidateQueries({ queryKey: [...CAP_TABLE_KEY, variables.companyId] });
      toast.success('Ações transferidas com sucesso');
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        error.response?.data?.errors?.[0] ||
        'Erro ao transferir ações';
      toast.error(message);
    },
  });
}

/**
 * Hook para cancelar ações
 */
export function useCancelShares() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CancelSharesRequest) => shareService.cancelShares(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: SHARES_KEY });
      queryClient.invalidateQueries({ queryKey: TRANSACTIONS_KEY });
      queryClient.invalidateQueries({ queryKey: [...CAP_TABLE_KEY, variables.companyId] });
      toast.success('Ações canceladas com sucesso');
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        error.response?.data?.errors?.[0] ||
        'Erro ao cancelar ações';
      toast.error(message);
    },
  });
}
