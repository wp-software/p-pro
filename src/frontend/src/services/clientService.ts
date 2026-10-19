import { api } from './api';
import {
  Client,
  ClientDetail,
  CreateClientRequest,
  UpdateClientRequest,
  ClientCompany,
  OnboardClientRequest,
  ClientOnboardingResponse,
  CoreClientPage,
} from '@/types';
import type { PagedResult, UserSummary } from '@/types';

// Tipo para resposta wrapper da API
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export const clientService = {
  async onboardClient(
    data: OnboardClientRequest
  ): Promise<ClientOnboardingResponse> {
    const response = await api.post<ApiResponse<ClientOnboardingResponse>>(
      '/clients/onboard',
      data
    );
    return response.data.data;
  },

  async resendAdministratorActivation(userId: string): Promise<void> {
    await api.post(`/users/${userId}/resend-activation`);
  },

  /**
   * Listar todos os clientes com paginação e busca
   */
  async getClients(params?: {
    page?: number;
    pageSize?: number;
    search?: string;
  }): Promise<CoreClientPage> {
    const response = await api.get<ApiResponse<CoreClientPage>>('/clients', { params });
    return response.data.data;
  },

  /**
   * Obter detalhes de um cliente específico
   */
  async getClientById(id: string): Promise<ClientDetail> {
    const response = await api.get<ApiResponse<ClientDetail>>(`/clients/${id}`);
    return response.data.data;
  },

  /**
   * Criar um novo cliente
   */
  async createClient(data: CreateClientRequest): Promise<Client> {
    const response = await api.post<ApiResponse<Client>>('/clients', data);
    return response.data.data;
  },

  /**
   * Atualizar um cliente existente
   */
  async updateClient(id: string, data: UpdateClientRequest): Promise<ClientDetail> {
    const response = await api.put<ApiResponse<ClientDetail>>(`/clients/${id}`, data);
    return response.data.data;
  },

  async updateClientEmail(id: string, email: string): Promise<ClientDetail> {
    const response = await api.patch<ApiResponse<ClientDetail>>(`/clients/${id}/email`, { email });
    return response.data.data;
  },

  async createClientUser(
    id: string,
    data: { name: string; email: string; companyId: string; role: number }
  ): Promise<UserSummary> {
    const response = await api.post<ApiResponse<UserSummary>>(`/clients/${id}/users`, data);
    return response.data.data;
  },

  async getClientUsers(
    id: string,
    params?: { page?: number; pageSize?: number; search?: string }
  ): Promise<PagedResult<UserSummary>> {
    const response = await api.get<ApiResponse<PagedResult<UserSummary>>>(
      `/clients/${id}/users`,
      { params }
    );
    return response.data.data;
  },

  /**
   * Excluir um cliente (soft delete)
   */
  async deleteClient(id: string): Promise<void> {
    await api.delete(`/clients/${id}`);
  },

  /**
   * Ativar um cliente
   */
  async activateClient(id: string): Promise<Client> {
    const response = await api.post<ApiResponse<Client>>(`/clients/${id}/activate`);
    return response.data.data;
  },

  /**
   * Desativar um cliente
   */
  async deactivateClient(id: string): Promise<Client> {
    const response = await api.post<ApiResponse<Client>>(`/clients/${id}/deactivate`);
    return response.data.data;
  },

  /**
   * Suspender um cliente
   */
  async suspendClient(id: string): Promise<Client> {
    const response = await api.post<ApiResponse<Client>>(`/clients/${id}/suspend`);
    return response.data.data;
  },

  /**
   * Listar empresas de um cliente
   */
  async getClientCompanies(id: string): Promise<ClientCompany[]> {
    const response = await api.get<ApiResponse<ClientCompany[]>>(`/clients/${id}/companies`);
    return response.data.data || [];
  },

  /**
   * Verificar se um documento já existe
   */
  async checkDocumentExists(document: string): Promise<boolean> {
    const response = await api.get<ApiResponse<boolean>>(`/clients/document/${document}/exists`);
    return response.data.data;
  },

  /**
   * Buscar cliente por documento
   */
  async getClientByDocument(document: string): Promise<Client | null> {
    try {
      const response = await api.get<ApiResponse<Client>>(`/clients/document/${document}`);
      return response.data.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },
};
