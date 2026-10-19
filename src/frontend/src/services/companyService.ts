import { api } from './api';
import type {
  Company,
  CreateCompanyRequest,
  UpdateCompanyRequest,
  UpdateShareInfoRequest,
  CompanyListResponse,
} from '@/types';

// Tipo para resposta wrapper da API
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export const companyService = {
  /**
   * Listar todas as empresas com paginação e busca
   */
  async getCompanies(params?: {
    page?: number;
    pageSize?: number;
    search?: string;
  }): Promise<CompanyListResponse> {
    const response = await api.get<ApiResponse<CompanyListResponse>>('/companies', { params });
    return response.data.data;
  },

  /**
   * Obter detalhes de uma empresa específica
   */
  async getCompanyById(id: string): Promise<Company> {
    const response = await api.get<ApiResponse<Company>>(`/companies/${id}`);
    return response.data.data;
  },

  /**
   * Criar uma nova empresa
   */
  async createCompany(data: CreateCompanyRequest): Promise<Company> {
    const response = await api.post<ApiResponse<Company>>('/companies', data);
    return response.data.data;
  },

  /**
   * Atualizar uma empresa existente
   */
  async updateCompany(id: string, data: UpdateCompanyRequest): Promise<Company> {
    const response = await api.put<ApiResponse<Company>>(`/companies/${id}`, data);
    return response.data.data;
  },

  /**
   * Atualizar informações de ações da empresa
   */
  async updateShareInfo(id: string, data: UpdateShareInfoRequest): Promise<Company> {
    const response = await api.patch<ApiResponse<Company>>(`/companies/${id}/shares`, data);
    return response.data.data;
  },

  async activateCompany(id: string): Promise<void> {
    await api.post(`/companies/${id}/activate`);
  },

  async deactivateCompany(id: string): Promise<void> {
    await api.post(`/companies/${id}/deactivate`);
  },

  /**
   * Excluir uma empresa (soft delete)
   */
  async deleteCompany(id: string): Promise<void> {
    await api.delete(`/companies/${id}`);
  },

  /**
   * Verificar se um CNPJ já existe
   */
  async checkCnpjExists(cnpj: string): Promise<boolean> {
    try {
      const response = await api.get<ApiResponse<boolean>>(`/companies/cnpj/${cnpj}/exists`);
      return response.data.data;
    } catch {
      return false;
    }
  },
};
