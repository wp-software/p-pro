import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { clientService } from '@/services/clientService';
import type {
  CreateClientRequest,
  OnboardClientRequest,
  UpdateClientRequest,
} from '@/types';
import toast from 'react-hot-toast';

const QUERY_KEY = ['clients'];

export function useOnboardClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: OnboardClientRequest) => clientService.onboardClient(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
    },
  });
}

export function useResendAdministratorActivation() {
  return useMutation({
    mutationFn: (userId: string) => clientService.resendAdministratorActivation(userId),
    onSuccess: () => toast.success('Convite reenviado com sucesso'),
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Não foi possível reenviar o convite');
    },
  });
}

/**
 * Hook para listar clientes com paginação e busca
 */
export function useClients(params?: {
  page?: number;
  pageSize?: number;
  search?: string;
}) {
  return useQuery({
    queryKey: [...QUERY_KEY, params],
    queryFn: () => clientService.getClients(params),
  });
}

/**
 * Hook para obter um cliente específico
 */
export function useClient(id: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, id],
    queryFn: () => clientService.getClientById(id),
    enabled: !!id,
  });
}

export function useClientUsers(
  clientId: string,
  params?: { page?: number; pageSize?: number; search?: string }
) {
  return useQuery({
    queryKey: [...QUERY_KEY, clientId, 'users', params],
    queryFn: () => clientService.getClientUsers(clientId, params),
    enabled: !!clientId,
  });
}

export function useUpdateClientEmail() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, email }: { id: string; email: string }) =>
      clientService.updateClientEmail(id, email),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('E-mail atualizado com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao atualizar e-mail');
    },
  });
}

/**
 * Hook para obter empresas de um cliente
 */
export function useClientCompanies(clientId: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, clientId, 'companies'],
    queryFn: () => clientService.getClientCompanies(clientId),
    enabled: !!clientId,
  });
}

/**
 * Hook para criar um novo cliente
 */
export function useCreateClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateClientRequest) => clientService.createClient(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Cliente criado com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao criar cliente');
    },
  });
}

/**
 * Hook para atualizar um cliente
 */
export function useUpdateClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateClientRequest }) =>
      clientService.updateClient(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Cliente atualizado com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao atualizar cliente');
    },
  });
}

/**
 * Hook para excluir um cliente
 */
export function useDeleteClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => clientService.deleteClient(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Cliente removido com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao remover cliente');
    },
  });
}

/**
 * Hook para ativar um cliente
 */
export function useActivateClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => clientService.activateClient(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Cliente ativado com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao ativar cliente');
    },
  });
}

/**
 * Hook para desativar um cliente
 */
export function useDeactivateClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => clientService.deactivateClient(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Cliente desativado com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao desativar cliente');
    },
  });
}

/**
 * Hook para suspender um cliente
 */
export function useSuspendClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => clientService.suspendClient(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Cliente suspenso com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao suspender cliente');
    }
  });
}

/**
 * Hook para verificar se um documento existe
 */
export function useCheckDocument(document: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, 'check', document],
    queryFn: () => clientService.checkDocumentExists(document),
    enabled: !!document && document.length >= 11,
  });
}

/**
 * Hook para buscar cliente por documento
 */
export function useClientByDocument(document: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, 'document', document],
    queryFn: () => clientService.getClientByDocument(document),
    enabled: !!document && document.length >= 11,
  });
}
