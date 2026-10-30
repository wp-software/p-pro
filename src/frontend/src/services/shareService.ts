import { api } from './api';
import { TransactionType } from '@/types';
import type {
  Share,
  ShareListResponse,
  ShareTransaction,
  TransactionListResponse,
  IssueSharesRequest,
  IssueSharesBatchRequest,
  TransferSharesRequest,
  CancelSharesRequest,
  ShareFilters,
  TransactionFilters,
} from '@/types';

// Tipo para resposta wrapper da API
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

const transactionTypesByApiValue: Record<string, TransactionType> = {
  Issue: TransactionType.Issue,
  Transfer: TransactionType.Transfer,
  Cancel: TransactionType.Cancel,
  Convert: TransactionType.Convert,
  Split: TransactionType.Split,
  ReverseSplit: TransactionType.Reverse,
  '1': TransactionType.Issue,
  '2': TransactionType.Transfer,
  '3': TransactionType.Cancel,
  '4': TransactionType.Convert,
  '5': TransactionType.Split,
  '6': TransactionType.Reverse,
};

const normalizeTransaction = (transaction: ShareTransaction): ShareTransaction => {
  const transactionType: unknown = transaction.transactionType;
  const normalizedType = transactionTypesByApiValue[String(transactionType)];

  if (normalizedType === undefined) {
    throw new Error(`Tipo de movimentação inválido recebido da API: ${String(transactionType)}`);
  }

  return { ...transaction, transactionType: normalizedType };
};

export const shareService = {
  // ============================================================================
  // SHARES
  // ============================================================================

  /**
   * Listar ações/participações com paginação e filtros
   */
  async getShares(filters?: ShareFilters): Promise<ShareListResponse> {
    const params: Record<string, string | number | undefined> = {};
    
    if (filters?.pageNumber) params.page = filters.pageNumber;
    if (filters?.pageSize) params.pageSize = filters.pageSize;
    if (filters?.companyId) params.companyId = filters.companyId;
    if (filters?.shareholderId) params.shareholderId = filters.shareholderId;
    if (filters?.shareClassId) params.shareClassId = filters.shareClassId;
    if (filters?.status !== undefined) params.status = filters.status;
    
    const response = await api.get<ApiResponse<ShareListResponse>>('/shares', { params });
    return response.data.data;
  },

  /**
   * Obter uma participação por ID
   */
  async getShareById(id: string): Promise<Share> {
    const response = await api.get<ApiResponse<Share>>(`/shares/${id}`);
    return response.data.data;
  },

  /**
   * Listar participações de um sócio específico
   */
  async getSharesByShareholder(shareholderId: string): Promise<Share[]> {
    const response = await api.get<ApiResponse<Share[]>>(`/shares/by-shareholder/${shareholderId}`);
    return response.data.data;
  },

  /**
   * Consultar saldo de ações de um sócio em uma classe específica
   */
  async getShareholderBalance(shareholderId: string, shareClassId: string): Promise<number> {
    const response = await api.get<ApiResponse<number>>('/shares/balance', {
      params: { shareholderId, shareClassId }
    });
    return response.data.data;
  },

  // ============================================================================
  // TRANSACTIONS
  // ============================================================================

  /**
   * Listar transações de ações com paginação e filtros
   */
  async getTransactions(filters?: TransactionFilters): Promise<TransactionListResponse> {
    const params: Record<string, string | number | undefined> = {};
    
    if (filters?.pageNumber) params.page = filters.pageNumber;
    if (filters?.pageSize) params.pageSize = filters.pageSize;
    if (filters?.companyId) params.companyId = filters.companyId;
    if (filters?.shareholderId) params.shareholderId = filters.shareholderId;
    if (filters?.shareClassId) params.shareClassId = filters.shareClassId;
    if (filters?.transactionType !== undefined) params.transactionType = filters.transactionType;
    if (filters?.fromDate) params.fromDate = filters.fromDate;
    if (filters?.toDate) params.toDate = filters.toDate;
    
    const response = await api.get<ApiResponse<TransactionListResponse>>('/shares/transactions', { params });
    return {
      ...response.data.data,
      items: response.data.data.items.map(normalizeTransaction),
    };
  },

  /**
   * Obter uma transação por ID
   */
  async getTransactionById(id: string): Promise<ShareTransaction> {
    const response = await api.get<ApiResponse<ShareTransaction>>(`/shares/transactions/${id}`);
    return normalizeTransaction(response.data.data);
  },

  /**
   * Listar transações de um sócio específico
   */
  async getTransactionsByShareholder(shareholderId: string): Promise<ShareTransaction[]> {
    const response = await api.get<ApiResponse<ShareTransaction[]>>(`/shares/transactions/by-shareholder/${shareholderId}`);
    return response.data.data.map(normalizeTransaction);
  },

  // ============================================================================
  // OPERATIONS
  // ============================================================================

  /**
   * Emitir novas ações para um sócio
   */
  async issueShares(data: IssueSharesRequest): Promise<Share> {
    const response = await api.post<ApiResponse<Share>>('/shares/issue', data);
    return response.data.data;
  },

  /**
   * Emitir ações para vários sócios em uma única operação
   */
  async issueSharesBatch(data: IssueSharesBatchRequest): Promise<Share[]> {
    const response = await api.post<ApiResponse<Share[]>>('/shares/issue-batch', data);
    return response.data.data;
  },

  /**
   * Transferir ações entre sócios
   */
  async transferShares(data: TransferSharesRequest): Promise<Share> {
    const response = await api.post<ApiResponse<Share>>('/shares/transfer', data);
    return response.data.data;
  },

  /**
   * Cancelar ações de um sócio
   */
  async cancelShares(data: CancelSharesRequest): Promise<void> {
    await api.post('/shares/cancel', data);
  },
};
