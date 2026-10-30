import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { approvalConfigService } from '@/services/approvalConfigService';
import type { SaveApprovalGroupRequest, SaveWorkflowTemplateRequest } from '@/types/phase6';

const GROUPS_KEY = ['approval-groups'];
const TEMPLATES_KEY = ['workflow-templates'];

function errorMessage(error: unknown, fallback: string): string {
  const message = (error as { response?: { data?: { message?: string } } })?.response?.data?.message;
  return message || fallback;
}

export function useApprovalGroups() {
  return useQuery({ queryKey: GROUPS_KEY, queryFn: approvalConfigService.getGroups, staleTime: 30000 });
}

export function useSaveApprovalGroup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id?: string; data: SaveApprovalGroupRequest }) =>
      id ? approvalConfigService.updateGroup(id, data) : approvalConfigService.createGroup(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: GROUPS_KEY });
      toast.success('Grupo salvo');
    },
    onError: (e) => toast.error(errorMessage(e, 'Erro ao salvar grupo')),
  });
}

export function useDeleteApprovalGroup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => approvalConfigService.deleteGroup(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: GROUPS_KEY });
      qc.invalidateQueries({ queryKey: TEMPLATES_KEY });
      toast.success('Grupo removido');
    },
    onError: (e) => toast.error(errorMessage(e, 'Erro ao remover grupo')),
  });
}

export function useWorkflowTemplates() {
  return useQuery({ queryKey: TEMPLATES_KEY, queryFn: approvalConfigService.getTemplates, staleTime: 30000 });
}

export function useSaveWorkflowTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id?: string; data: SaveWorkflowTemplateRequest }) =>
      id ? approvalConfigService.updateTemplate(id, data) : approvalConfigService.createTemplate(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TEMPLATES_KEY });
      toast.success('Modelo salvo');
    },
    onError: (e) => toast.error(errorMessage(e, 'Erro ao salvar modelo')),
  });
}

export function useDeleteWorkflowTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => approvalConfigService.deleteTemplate(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TEMPLATES_KEY });
      toast.success('Modelo removido');
    },
    onError: (e) => toast.error(errorMessage(e, 'Erro ao remover modelo')),
  });
}
