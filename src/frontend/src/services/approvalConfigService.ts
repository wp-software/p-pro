import { api } from './api';
import type {
  ApprovalGroup,
  SaveApprovalGroupRequest,
  WorkflowTemplate,
  SaveWorkflowTemplateRequest,
} from '@/types/phase6';

interface ApiResponse<T> {
  success: boolean;
  data: T;
}

export const approvalConfigService = {
  async getGroups(): Promise<ApprovalGroup[]> {
    const r = await api.get<ApiResponse<ApprovalGroup[]>>('/approval-groups');
    return r.data.data;
  },

  async createGroup(data: SaveApprovalGroupRequest): Promise<void> {
    await api.post('/approval-groups', data);
  },

  async updateGroup(id: string, data: SaveApprovalGroupRequest): Promise<void> {
    await api.put(`/approval-groups/${id}`, data);
  },

  async deleteGroup(id: string): Promise<void> {
    await api.delete(`/approval-groups/${id}`);
  },

  async getTemplates(): Promise<WorkflowTemplate[]> {
    const r = await api.get<ApiResponse<WorkflowTemplate[]>>('/workflow-templates');
    return r.data.data;
  },

  async createTemplate(data: SaveWorkflowTemplateRequest): Promise<void> {
    await api.post('/workflow-templates', data);
  },

  async updateTemplate(id: string, data: SaveWorkflowTemplateRequest): Promise<void> {
    await api.put(`/workflow-templates/${id}`, data);
  },

  async deleteTemplate(id: string): Promise<void> {
    await api.delete(`/workflow-templates/${id}`);
  },
};
