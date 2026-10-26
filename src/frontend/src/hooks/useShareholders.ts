import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { shareholderService } from "@/services/shareholderService";
import type {
  ShareholderFilters,
  CreateShareholderRequest,
  UpdateShareholderRequest,
} from "@/types";
import toast from "react-hot-toast";

const QUERY_KEY = ["shareholders"];

/**
 * Hook para listar sócios com paginação e filtros
 */
export function useShareholders(filters?: ShareholderFilters) {
  return useQuery({
    queryKey: [...QUERY_KEY, filters],
    queryFn: () => shareholderService.getShareholders(filters),
    staleTime: 30000, // 30 segundos
  });
}

/**
 * Hook para listar todos os sócios de uma empresa, sem limite de paginação
 */
export function useAllShareholders(companyId: string | undefined) {
  return useQuery({
    queryKey: [...QUERY_KEY, "all", companyId],
    queryFn: () => shareholderService.getAllByCompany(companyId!),
    enabled: !!companyId,
    staleTime: 30000,
    refetchOnMount: "always",
  });
}

/**
 * Hook para obter um sócio específico
 */
export function useShareholder(id: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, id],
    queryFn: () => shareholderService.getShareholderById(id),
    enabled: !!id,
  });
}

/**
 * Hook para buscar sócio por documento
 */
export function useShareholderByDocument(document: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, "document", document],
    queryFn: () => shareholderService.getShareholderByDocument(document),
    enabled: !!document && document.length >= 11, // CPF ou CNPJ
  });
}

/**
 * Hook para criar um novo sócio
 */
export function useCreateShareholder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateShareholderRequest) =>
      shareholderService.createShareholder(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success("Sócio criado com sucesso");
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        error.response?.data?.errors?.[0]?.message ||
        "Erro ao criar sócio";
      toast.error(message);
    },
  });
}

/**
 * Hook para atualizar um sócio
 */
export function useUpdateShareholder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: UpdateShareholderRequest;
    }) => shareholderService.updateShareholder(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: [...QUERY_KEY, variables.id] });
      toast.success("Sócio atualizado com sucesso");
    },
    onError: (error: any) => {
      const message =
        error.response?.data?.message ||
        error.response?.data?.errors?.[0]?.message ||
        "Erro ao atualizar sócio";
      toast.error(message);
    },
  });
}

/**
 * Hook para excluir um sócio
 */
export function useDeleteShareholder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => shareholderService.deleteShareholder(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success("Sócio excluído com sucesso");
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || "Erro ao excluir sócio";
      toast.error(message);
    },
  });
}

/**
 * Hook para ativar um sócio
 */
export function useActivateShareholder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => shareholderService.activateShareholder(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success("Sócio ativado com sucesso");
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || "Erro ao ativar sócio");
    },
  });
}

/**
 * Hook para desativar um sócio
 */
export function useDeactivateShareholder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => shareholderService.deactivateShareholder(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success("Sócio desativado com sucesso");
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || "Erro ao desativar sócio");
    },
  });
}
