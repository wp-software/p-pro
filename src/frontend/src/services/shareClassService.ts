import { api } from './api';

// Tipo para resposta wrapper da API
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export interface ShareClass {
  id: string;
  clientId: string;
  companyId: string | null;
  companyName: string | null;
  code: string;
  name: string;
  description?: string;
  hasVotingRights: boolean;
  votesPerShare: number;
  liquidationPreference: number;
  participating: boolean;
  dividendPreference?: number | null;
  isConvertible: boolean;
  convertsToClassId?: string | null;
  convertsToClassName?: string | null;
  conversionRatio?: number | null;
  antiDilutionType?: 'None' | 'FullRatchet' | 'WeightedAverage' | null;
  antiDilutionTypeDescription?: string | null;
  rights?: string | null;
  status: 'Active' | 'Inactive';
  statusDescription: string;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface ShareClassRequest {
  name: string;
  code: string;
  description?: string;
  hasVotingRights: boolean;
  votesPerShare: number;
  liquidationPreference: number;
  participating: boolean;
  dividendPreference: number | null;
  isConvertible: boolean;
  convertsToClassId: string | null;
  conversionRatio: number | null;
  antiDilutionType: 'None' | 'FullRatchet' | 'WeightedAverage' | null;
  rights?: string;
  displayOrder: number;
}

export type CreateShareClassRequest = ShareClassRequest;

export interface ShareClassSummary {
  id: string;
  code: string;
  name: string;
  hasVotingRights: boolean;
  status: 'Active' | 'Inactive';
}

export interface ShareClassListResponse {
  items: ShareClass[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export const shareClassService = {
  /**
   * Listar classes de ações com paginação e filtros
   */
  async getShareClasses(
    companyId?: string,
    page = 1,
    pageSize = 10,
    search?: string,
    status?: string
  ): Promise<ShareClassListResponse> {
    const params: Record<string, string | number | undefined> = {
      page,
      pageSize,
    };
    if (companyId) params.companyId = companyId;
    if (search) params.search = search;
    if (status) params.status = status;
    
    const response = await api.get<ApiResponse<ShareClassListResponse>>('/share-classes', { params });
    return response.data.data;
  },

  async create(data: CreateShareClassRequest): Promise<ShareClass> {
    const response = await api.post<ApiResponse<ShareClass>>('/share-classes', data);
    return response.data.data;
  },

  async update(id: string, data: ShareClassRequest): Promise<ShareClass> {
    const response = await api.put<ApiResponse<ShareClass>>(`/share-classes/${id}`, data);
    return response.data.data;
  },

  async delete(id: string): Promise<void> {
    await api.delete(`/share-classes/${id}`);
  },

  /**
   * Listar classes de ações de uma empresa (resumido)
   */
  async getByCompany(companyId: string): Promise<ShareClassSummary[]> {
    const response = await api.get<ApiResponse<ShareClassSummary[]>>(`/share-classes/by-company/${companyId}`);
    return response.data.data;
  },

  /**
   * Obter classe de ações por ID
   */
  async getById(id: string): Promise<ShareClass> {
    const response = await api.get<ApiResponse<ShareClass>>(`/share-classes/${id}`);
    return response.data.data;
  },
};
