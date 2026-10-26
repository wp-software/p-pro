import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { shareClassService } from "@/services/shareClassService";
import type {
  CreateShareClassRequest,
  ShareClassRequest,
} from "@/services/shareClassService";
import { AxiosError } from "axios";
import toast from "react-hot-toast";

const SHARE_CLASSES_KEY = ["shareClasses"];

interface ApiError {
  message?: string;
  errors?: string[];
}

function showMutationError(error: unknown, fallback: string) {
  const responseData =
    error instanceof AxiosError
      ? (error.response?.data as ApiError | undefined)
      : undefined;
  const message =
    responseData?.message || responseData?.errors?.[0] || fallback;
  toast.error(message);
}

/**
 * Hook para listar classes de ações
 */
export function useShareClasses(
  companyId?: string,
  page = 1,
  pageSize = 10,
  search?: string,
  status?: string,
  enabled = true,
) {
  return useQuery({
    queryKey: [...SHARE_CLASSES_KEY, companyId, page, pageSize, search, status],
    queryFn: () =>
      shareClassService.getShareClasses(
        companyId,
        page,
        pageSize,
        search,
        status,
      ),
    enabled,
    staleTime: 60000, // 1 minuto
  });
}

/**
 * Hook para listar classes de ações de uma empresa (resumido)
 */
export function useShareClassesByCompany(companyId: string | undefined) {
  return useQuery({
    queryKey: [...SHARE_CLASSES_KEY, "by-company", companyId],
    queryFn: () => shareClassService.getByCompany(companyId!),
    enabled: !!companyId,
    staleTime: 60000,
    refetchOnMount: "always",
  });
}

/**
 * Hook para obter uma classe de ações específica
 */
export function useShareClass(id: string | undefined) {
  return useQuery({
    queryKey: [...SHARE_CLASSES_KEY, id],
    queryFn: () => shareClassService.getById(id!),
    enabled: !!id,
  });
}

export function useCreateShareClass() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateShareClassRequest) =>
      shareClassService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SHARE_CLASSES_KEY });
      toast.success("Classe de ações criada com sucesso");
    },
    onError: (error: unknown) =>
      showMutationError(error, "Erro ao criar classe de ações"),
  });
}

export function useUpdateShareClass() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ShareClassRequest }) =>
      shareClassService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SHARE_CLASSES_KEY });
      toast.success("Classe de ações atualizada com sucesso");
    },
    onError: (error: unknown) =>
      showMutationError(error, "Erro ao atualizar classe de ações"),
  });
}

export function useDeleteShareClass() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => shareClassService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SHARE_CLASSES_KEY });
      toast.success("Classe de ações excluída com sucesso");
    },
    onError: (error: unknown) =>
      showMutationError(error, "Erro ao excluir classe de ações"),
  });
}
