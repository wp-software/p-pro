import { api } from './api';
import type {
  User,
  UserSummary,
  CreateUserRequest,
  UpdateUserRequest,
  ManageUserRoleRequest,
  PagedResult,
  PaginationParams,
} from '@/types';

const USERS_ENDPOINT = '/users';

export const userService = {
  /**
   * Lista usuários com paginação
   */
  getAll: async (params?: PaginationParams): Promise<PagedResult<UserSummary>> => {
    const response = await api.get<{ data: PagedResult<UserSummary> }>(
      USERS_ENDPOINT,
      { params }
    );
    return response.data.data;
  },

  /**
   * Obtém usuário por ID
   */
  getById: async (id: string): Promise<User> => {
    const response = await api.get<{ data: User }>(
      `${USERS_ENDPOINT}/${id}`
    );
    return response.data.data;
  },

  /**
   * Cria novo usuário
   */
  create: async (data: CreateUserRequest): Promise<User> => {
    const response = await api.post<{ data: User }>(USERS_ENDPOINT, data);
    return response.data.data;
  },

  /**
   * Atualiza usuário
   */
  update: async (id: string, data: UpdateUserRequest): Promise<User> => {
    const response = await api.put<{ data: User }>(
      `${USERS_ENDPOINT}/${id}`,
      data
    );
    return response.data.data;
  },

  /**
   * Remove usuário (soft delete)
   */
  delete: async (id: string): Promise<void> => {
    await api.delete(`${USERS_ENDPOINT}/${id}`);
  },

  /**
   * Adiciona papel ao usuário
   */
  addRole: async (
    userId: string,
    data: ManageUserRoleRequest
  ): Promise<void> => {
    await api.post(`${USERS_ENDPOINT}/${userId}/roles`, data);
  },

  /**
   * Remove papel do usuário
   */
  removeRole: async (userId: string, role: string): Promise<void> => {
    await api.delete(`${USERS_ENDPOINT}/${userId}/roles/${role}`);
  },

  /**
   * Ativa usuário
   */
  deactivate: async (id: string): Promise<void> => {
    await api.post(`${USERS_ENDPOINT}/${id}/deactivate`);
  },

  activate: async (id: string): Promise<void> => {
    await api.post(`${USERS_ENDPOINT}/${id}/activate`);
  },

  /**
   * Reenvia e-mail de ativação para usuário pendente
   */
  resendActivation: async (id: string): Promise<void> => {
    await api.post(`${USERS_ENDPOINT}/${id}/resend-activation`);
  },
};
