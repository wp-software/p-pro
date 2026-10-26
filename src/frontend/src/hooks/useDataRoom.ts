import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { dataRoomService, type CreateFolderRequest } from '@/services/dataRoomService';
import toast from 'react-hot-toast';

const QUERY_KEY = ['dataroom'];

export function useDataRoom() {
  return useQuery({
    queryKey: [...QUERY_KEY],
    queryFn: () => dataRoomService.getDataRoom(),
    staleTime: 60000,
  });
}

export function useDataRoomFolders(parentId?: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, 'folders', parentId],
    queryFn: () => dataRoomService.getFolders(parentId),
    staleTime: 30000,
  });
}

export function useDocumentsInFolder(folderId: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, 'folder-docs', folderId],
    queryFn: () => dataRoomService.getDocumentsInFolder(folderId),
    enabled: !!folderId,
    staleTime: 30000,
  });
}

export function useCreateFolder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateFolderRequest) => dataRoomService.createFolder(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Pasta criada com sucesso');
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Erro ao criar pasta';
      toast.error(message);
    },
  });
}

export function useDeleteFolder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => dataRoomService.deleteFolder(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Pasta excluída');
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Erro ao excluir pasta';
      toast.error(message);
    },
  });
}

export function useAddDocumentToFolder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ folderId, documentId }: { folderId: string; documentId: string }) =>
      dataRoomService.addDocumentToFolder(folderId, documentId),
    onSuccess: (_, { folderId }) => {
      queryClient.invalidateQueries({ queryKey: [...QUERY_KEY, 'folder-docs', folderId] });
      toast.success('Documento adicionado à pasta');
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Erro ao adicionar documento';
      toast.error(message);
    },
  });
}

export function useUploadDocumentToFolder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ folderId, file }: { folderId: string; file: File }) =>
      dataRoomService.uploadDocumentToFolder(folderId, file),
    onSuccess: (_, { folderId }) => {
      queryClient.invalidateQueries({ queryKey: [...QUERY_KEY, 'folder-docs', folderId] });
      toast.success('Documento enviado com sucesso');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao enviar documento');
    },
  });
}

export function useRemoveDocumentFromFolder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ folderId, documentId }: { folderId: string; documentId: string }) =>
      dataRoomService.removeDocumentFromFolder(folderId, documentId),
    onSuccess: (_, { folderId }) => {
      queryClient.invalidateQueries({ queryKey: [...QUERY_KEY, 'folder-docs', folderId] });
      toast.success('Documento removido da pasta');
    },
    onError: (error: any) => {
      const message = error.response?.data?.message || 'Erro ao remover documento';
      toast.error(message);
    },
  });
}

export function useSetFolderRoles() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ folderId, roles }: { folderId: string; roles: string[] }) =>
      dataRoomService.setFolderRoles(folderId, roles),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Permissões da pasta atualizadas');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao atualizar permissões');
    },
  });
}

export function useDataRoomActivityLogs(enabled: boolean) {
  return useQuery({
    queryKey: [...QUERY_KEY, 'activity-logs'],
    queryFn: () => dataRoomService.getActivityLogs(),
    enabled,
    staleTime: 10000,
  });
}

export function useUploadFinancialDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (params: Parameters<typeof dataRoomService.uploadFinancialDocument>[0]) =>
      dataRoomService.uploadFinancialDocument(params),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success(`Documento enviado para o Data Room (${result.folderPath})`);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao enviar documento ao Data Room');
    },
  });
}

export function useFinancialDocuments(periodId: string) {
  return useQuery({
    queryKey: [...QUERY_KEY, 'financial-docs', periodId],
    queryFn: () => dataRoomService.getFinancialDocuments(periodId),
    enabled: !!periodId,
    staleTime: 15000,
  });
}

export function useRemoveFinancialDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => dataRoomService.removeFinancialDocument(documentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
      toast.success('Documento removido');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Erro ao remover documento');
    },
  });
}
