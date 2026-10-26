import { api } from './api';
import { ShareholderType, ShareholderStatus, DocumentType, Gender, MaritalStatus } from '@/types';
import type {
  Shareholder,
  ShareholderListResponse,
  CreateShareholderRequest,
  UpdateShareholderRequest,
  ShareholderFilters,
} from '@/types';

// Tipo para resposta wrapper da API
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

// A API serializa enums como texto ("Active"); o frontend usa enums numéricos.
const toEnum = (enumObj: object, value: unknown): number | undefined => {
  if (value === null || value === undefined || value === '') return undefined;
  if (typeof value === 'number') return value;
  const byName = (enumObj as Record<string, unknown>)[String(value)];
  if (typeof byName === 'number') return byName;
  const numeric = Number(value);
  return Number.isNaN(numeric) ? undefined : numeric;
};

const normalizeShareholder = (raw: Shareholder): Shareholder => ({
  ...raw,
  type: toEnum(ShareholderType, raw.type) as ShareholderType,
  status: toEnum(ShareholderStatus, raw.status) as ShareholderStatus,
  documentType: toEnum(DocumentType, raw.documentType) as DocumentType,
  gender: toEnum(Gender, raw.gender) as Gender | undefined,
  maritalStatus: toEnum(MaritalStatus, raw.maritalStatus) as MaritalStatus | undefined,
  companies: raw.companies?.map((c) => ({ ...c, type: toEnum(ShareholderType, c.type) as ShareholderType })),
});

export const shareholderService = {
  /**
   * Listar sócios com paginação e filtros
   */
  async getShareholders(filters?: ShareholderFilters): Promise<ShareholderListResponse> {
    const params: Record<string, string | number | undefined> = {};
    
    if (filters?.page) params.page = filters.page;
    if (filters?.pageSize) params.pageSize = filters.pageSize;
    if (filters?.search) params.search = filters.search;
    if (filters?.type !== undefined) params.type = ShareholderType[filters.type];
    if (filters?.status !== undefined) params.status = ShareholderStatus[filters.status];
    if (filters?.companyId) params.companyId = filters.companyId;
    
    const response = await api.get<ApiResponse<ShareholderListResponse>>('/shareholders', { params });
    const list = response.data.data;
    return { ...list, items: list.items.map(normalizeShareholder) };
  },

  /**
   * Obter um sócio específico pelo ID
   */
  async getShareholderById(id: string): Promise<Shareholder> {
    const response = await api.get<ApiResponse<Shareholder>>(`/shareholders/${id}`);
    return normalizeShareholder(response.data.data);
  },

  /**
   * Criar um novo sócio
   */
  async createShareholder(data: CreateShareholderRequest): Promise<Shareholder> {
    const response = await api.post<ApiResponse<Shareholder>>('/shareholders', data);
    return normalizeShareholder(response.data.data);
  },

  /**
   * Atualizar um sócio existente
   */
  async updateShareholder(id: string, data: UpdateShareholderRequest): Promise<Shareholder> {
    const response = await api.put<ApiResponse<Shareholder>>(`/shareholders/${id}`, data);
    return normalizeShareholder(response.data.data);
  },

  /**
   * Excluir um sócio (soft delete)
   */
  async deleteShareholder(id: string): Promise<void> {
    await api.delete(`/shareholders/${id}`);
  },

  /**
   * Ativar um sócio
   */
  async activateShareholder(id: string): Promise<Shareholder> {
    const response = await api.patch<ApiResponse<Shareholder>>(`/shareholders/${id}/activate`);
    return normalizeShareholder(response.data.data);
  },

  /**
   * Desativar um sócio
   */
  async deactivateShareholder(id: string): Promise<Shareholder> {
    const response = await api.patch<ApiResponse<Shareholder>>(`/shareholders/${id}/deactivate`);
    return normalizeShareholder(response.data.data);
  },

  /**
   * Buscar sócio por documento (CPF/CNPJ)
   */
  async getShareholderByDocument(document: string): Promise<Shareholder | null> {
    try {
      const response = await api.get<ApiResponse<Shareholder>>(`/shareholders/document/${document}`);
      return normalizeShareholder(response.data.data);
    } catch {
      return null;
    }
  },
};
