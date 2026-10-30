import { api } from './api';
import type {
  VestingPlan,
  VestingPlanListResponse,
  CreateVestingPlanRequest,
  UpdateVestingPlanRequest,
  VestingPlanFilters,
  VestingGrant,
  VestingGrantListResponse,
  CreateVestingGrantRequest,
  ExerciseSharesRequest,
  VestingGrantFilters,
  VestingMilestone,
  VestingMilestoneListResponse,
  CreateVestingMilestoneRequest,
  AchieveMilestoneRequest,
  VestingMilestoneFilters,
  VestingCalculationResult,
  VestingProjectionResponse,
  VestingTransaction,
  MilestoneTemplate,
  MilestoneTemplateListResponse,
  CreateMilestoneTemplateRequest,
  UpdateMilestoneTemplateRequest,
  MilestoneTemplateFilters,
  GrantMilestone,
  GrantMilestoneListResponse,
  CreateGrantMilestoneRequest,
  AchieveGrantMilestoneRequest,
  GrantMilestoneFilters,
  MilestoneProgress,
  RecordMilestoneProgressRequest,
  VestingAcceleration,
  AccelerationPreview,
  MilestoneProgressDashboard,
} from '@/types';

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

// ─── Vesting Plans ────────────────────────────────────────────────────────────

export const vestingPlanService = {
  async getPlans(filters?: VestingPlanFilters): Promise<VestingPlanListResponse> {
    const params: Record<string, string | number | undefined> = {};
    if (filters?.companyId) params.companyId = filters.companyId;
    if (filters?.page) params.page = filters.page;
    if (filters?.pageSize) params.pageSize = filters.pageSize;
    if (filters?.search) params.search = filters.search;
    if (filters?.status) params.status = filters.status;

    const response = await api.get<ApiResponse<VestingPlanListResponse>>('/vesting-plans', { params });
    return response.data.data;
  },

  async getPlansByCompany(companyId: string, status?: string): Promise<VestingPlan[]> {
    const params: Record<string, string | undefined> = {};
    if (status) params.status = status;
    const response = await api.get<ApiResponse<VestingPlan[]>>(
      `/vesting-plans/by-company/${companyId}`,
      { params }
    );
    return response.data.data;
  },

  async getPlanById(id: string): Promise<VestingPlan> {
    const response = await api.get<ApiResponse<VestingPlan>>(`/vesting-plans/${id}`);
    return response.data.data;
  },

  async createPlan(data: CreateVestingPlanRequest): Promise<VestingPlan> {
    const response = await api.post<ApiResponse<VestingPlan>>('/vesting-plans', data);
    return response.data.data;
  },

  async updatePlan(id: string, data: UpdateVestingPlanRequest): Promise<VestingPlan> {
    const response = await api.put<ApiResponse<VestingPlan>>(`/vesting-plans/${id}`, data);
    return response.data.data;
  },

  async activatePlan(id: string): Promise<VestingPlan> {
    const response = await api.patch<ApiResponse<VestingPlan>>(`/vesting-plans/${id}/activate`);
    return response.data.data;
  },

  async deactivatePlan(id: string): Promise<VestingPlan> {
    const response = await api.patch<ApiResponse<VestingPlan>>(`/vesting-plans/${id}/deactivate`);
    return response.data.data;
  },

  async archivePlan(id: string): Promise<VestingPlan> {
    const response = await api.patch<ApiResponse<VestingPlan>>(`/vesting-plans/${id}/archive`);
    return response.data.data;
  },

  async deletePlan(id: string): Promise<void> {
    await api.delete(`/vesting-plans/${id}`);
  },
};

// ─── Vesting Grants ───────────────────────────────────────────────────────────

export const vestingGrantService = {
  async getGrants(filters?: VestingGrantFilters): Promise<VestingGrantListResponse> {
    const params: Record<string, string | number | undefined> = {};
    if (filters?.companyId) params.companyId = filters.companyId;
    if (filters?.vestingPlanId) params.vestingPlanId = filters.vestingPlanId;
    if (filters?.shareholderId) params.shareholderId = filters.shareholderId;
    if (filters?.status) params.status = filters.status;
    if (filters?.page) params.page = filters.page;
    if (filters?.pageSize) params.pageSize = filters.pageSize;

    const response = await api.get<ApiResponse<VestingGrantListResponse>>('/vesting-grants', { params });
    return response.data.data;
  },

  async getMyGrants(companyId?: string): Promise<VestingGrant[]> {
    const params: Record<string, string | undefined> = {};
    if (companyId) params.companyId = companyId;
    const response = await api.get<ApiResponse<VestingGrant[]>>('/vesting-grants/me', { params });
    return response.data.data;
  },

  async getGrantsByShareholder(
    shareholderId: string,
    companyId?: string
  ): Promise<VestingGrant[]> {
    const params: Record<string, string | undefined> = {};
    if (companyId) params.companyId = companyId;
    const response = await api.get<ApiResponse<VestingGrant[]>>(
      `/vesting-grants/by-shareholder/${shareholderId}`,
      { params }
    );
    return response.data.data;
  },

  async getGrantById(id: string): Promise<VestingGrant> {
    const response = await api.get<ApiResponse<VestingGrant>>(`/vesting-grants/${id}`);
    return response.data.data;
  },

  async createGrant(data: CreateVestingGrantRequest): Promise<VestingGrant> {
    const response = await api.post<ApiResponse<VestingGrant>>('/vesting-grants', data);
    return response.data.data;
  },

  async approveGrant(id: string): Promise<VestingGrant> {
    const response = await api.patch<ApiResponse<VestingGrant>>(`/vesting-grants/${id}/approve`);
    return response.data.data;
  },

  async activateGrant(id: string): Promise<VestingGrant> {
    const response = await api.patch<ApiResponse<VestingGrant>>(`/vesting-grants/${id}/activate`);
    return response.data.data;
  },

  async cancelGrant(id: string): Promise<VestingGrant> {
    const response = await api.patch<ApiResponse<VestingGrant>>(`/vesting-grants/${id}/cancel`);
    return response.data.data;
  },

  async exerciseShares(id: string, data: ExerciseSharesRequest): Promise<VestingTransaction> {
    const response = await api.post<ApiResponse<VestingTransaction>>(
      `/vesting-grants/${id}/exercise`,
      data
    );
    return response.data.data;
  },

  async calculateVesting(id: string, asOfDate?: string): Promise<VestingCalculationResult> {
    const params: Record<string, string | undefined> = {};
    if (asOfDate) params.asOfDate = asOfDate;
    const response = await api.get<ApiResponse<VestingCalculationResult>>(
      `/vesting-grants/${id}/calculate`,
      { params }
    );
    return response.data.data;
  },

  async getProjection(id: string): Promise<VestingProjectionResponse> {
    const response = await api.get<ApiResponse<VestingProjectionResponse>>(
      `/vesting-grants/${id}/projection`
    );
    return response.data.data;
  },

  async getTransactions(
    id: string,
    page = 1,
    pageSize = 10
  ): Promise<VestingTransaction[]> {
    const response = await api.get<ApiResponse<VestingTransaction[]>>(
      `/vesting-grants/${id}/transactions`,
      { params: { page, pageSize } }
    );
    return response.data.data ?? [];
  },

  async deleteGrant(id: string): Promise<void> {
    await api.delete(`/vesting-grants/${id}`);
  },
};

// ─── Milestones ───────────────────────────────────────────────────────────────

export const vestingMilestoneService = {
  async getMilestones(filters?: VestingMilestoneFilters): Promise<VestingMilestoneListResponse> {
    const params: Record<string, string | number | undefined> = {};
    if (filters?.vestingPlanId) params.vestingPlanId = filters.vestingPlanId;
    if (filters?.companyId) params.companyId = filters.companyId;
    if (filters?.page) params.page = filters.page;
    if (filters?.pageSize) params.pageSize = filters.pageSize;

    const response = await api.get<ApiResponse<VestingMilestoneListResponse>>('/milestones', { params });
    return response.data.data;
  },

  async getMilestonesByPlan(vestingPlanId: string): Promise<VestingMilestone[]> {
    const response = await api.get<ApiResponse<VestingMilestone[]>>(
      `/milestones/by-plan/${vestingPlanId}`
    );
    return response.data.data;
  },

  async getMilestoneById(id: string): Promise<VestingMilestone> {
    const response = await api.get<ApiResponse<VestingMilestone>>(`/milestones/${id}`);
    return response.data.data;
  },

  async createMilestone(data: CreateVestingMilestoneRequest): Promise<VestingMilestone> {
    const response = await api.post<ApiResponse<VestingMilestone>>('/milestones', data);
    return response.data.data;
  },

  async achieveMilestone(id: string, data: AchieveMilestoneRequest): Promise<VestingMilestone> {
    const response = await api.patch<ApiResponse<VestingMilestone>>(
      `/milestones/${id}/achieve`,
      data
    );
    return response.data.data;
  },

  async failMilestone(id: string): Promise<VestingMilestone> {
    const response = await api.patch<ApiResponse<VestingMilestone>>(`/milestones/${id}/fail`);
    return response.data.data;
  },

  async deleteMilestone(id: string): Promise<void> {
    await api.delete(`/milestones/${id}`);
  },
};

// ─── Milestone Templates ──────────────────────────────────────────────────────

export const milestoneTemplateService = {
  async getTemplates(filters?: MilestoneTemplateFilters): Promise<MilestoneTemplateListResponse> {
    const params: Record<string, string | number | boolean | undefined> = {};
    if (filters?.companyId) params.companyId = filters.companyId;
    if (filters?.category) params.category = filters.category;
    if (filters?.isActive !== undefined) params.isActive = filters.isActive;
    if (filters?.page) params.page = filters.page;
    if (filters?.pageSize) params.pageSize = filters.pageSize;
    const response = await api.get<ApiResponse<MilestoneTemplateListResponse>>('/milestone-templates', { params });
    return response.data.data;
  },

  async getAllByCompany(companyId: string, activeOnly = true): Promise<MilestoneTemplate[]> {
    const response = await api.get<ApiResponse<MilestoneTemplate[]>>(
      '/milestone-templates/all',
      { params: { companyId, activeOnly } }
    );
    return response.data.data;
  },

  async getTemplateById(id: string): Promise<MilestoneTemplate> {
    const response = await api.get<ApiResponse<MilestoneTemplate>>(`/milestone-templates/${id}`);
    return response.data.data;
  },

  async createTemplate(companyId: string, data: CreateMilestoneTemplateRequest): Promise<MilestoneTemplate> {
    const response = await api.post<ApiResponse<MilestoneTemplate>>(
      `/milestone-templates?companyId=${companyId}`,
      data
    );
    return response.data.data;
  },

  async updateTemplate(id: string, data: UpdateMilestoneTemplateRequest): Promise<MilestoneTemplate> {
    const response = await api.put<ApiResponse<MilestoneTemplate>>(`/milestone-templates/${id}`, data);
    return response.data.data;
  },

  async activateTemplate(id: string): Promise<void> {
    await api.patch(`/milestone-templates/${id}/activate`);
  },

  async deactivateTemplate(id: string): Promise<void> {
    await api.patch(`/milestone-templates/${id}/deactivate`);
  },

  async deleteTemplate(id: string): Promise<void> {
    await api.delete(`/milestone-templates/${id}`);
  },
};

// ─── Grant Milestones ─────────────────────────────────────────────────────────

export const grantMilestoneService = {
  async getMilestones(filters?: GrantMilestoneFilters): Promise<GrantMilestoneListResponse> {
    const params: Record<string, string | number | undefined> = {};
    if (filters?.companyId) params.companyId = filters.companyId;
    if (filters?.grantId) params.grantId = filters.grantId;
    if (filters?.status) params.status = filters.status;
    if (filters?.category) params.category = filters.category;
    if (filters?.page) params.page = filters.page;
    if (filters?.pageSize) params.pageSize = filters.pageSize;
    const response = await api.get<ApiResponse<GrantMilestoneListResponse>>('/grant-milestones', { params });
    return response.data.data;
  },

  async getMilestonesByGrant(grantId: string): Promise<GrantMilestone[]> {
    const response = await api.get<ApiResponse<GrantMilestone[]>>(`/grants/${grantId}/milestones`);
    return response.data.data;
  },

  async getMilestoneById(id: string): Promise<GrantMilestone> {
    const response = await api.get<ApiResponse<GrantMilestone>>(`/grant-milestones/${id}`);
    return response.data.data;
  },

  async createMilestone(companyId: string, data: CreateGrantMilestoneRequest): Promise<GrantMilestone> {
    const response = await api.post<ApiResponse<GrantMilestone>>(
      `/grant-milestones?companyId=${companyId}`,
      data
    );
    return response.data.data;
  },

  async recordProgress(id: string, data: RecordMilestoneProgressRequest): Promise<GrantMilestone> {
    const response = await api.post<ApiResponse<GrantMilestone>>(
      `/grant-milestones/${id}/progress`,
      data
    );
    return response.data.data;
  },

  async getProgressHistory(id: string): Promise<MilestoneProgress[]> {
    const response = await api.get<ApiResponse<MilestoneProgress[]>>(
      `/grant-milestones/${id}/progress`
    );
    return response.data.data;
  },

  async getProgressTimeline(
    id: string,
    from?: string,
    to?: string
  ): Promise<MilestoneProgress[]> {
    const response = await api.get<ApiResponse<MilestoneProgress[]>>(
      `/grant-milestones/${id}/progress/timeline`,
      { params: { from, to } }
    );
    return response.data.data;
  },

  async achieveMilestone(id: string, data: AchieveGrantMilestoneRequest): Promise<GrantMilestone> {
    const response = await api.patch<ApiResponse<GrantMilestone>>(
      `/grant-milestones/${id}/achieve`,
      data
    );
    return response.data.data;
  },

  async verifyMilestone(id: string): Promise<GrantMilestone> {
    const response = await api.patch<ApiResponse<GrantMilestone>>(
      `/grant-milestones/${id}/verify`,
      {}
    );
    return response.data.data;
  },

  async failMilestone(id: string): Promise<GrantMilestone> {
    const response = await api.patch<ApiResponse<GrantMilestone>>(`/grant-milestones/${id}/fail`);
    return response.data.data;
  },

  async cancelMilestone(id: string): Promise<GrantMilestone> {
    const response = await api.patch<ApiResponse<GrantMilestone>>(`/grant-milestones/${id}/cancel`);
    return response.data.data;
  },

  async deleteMilestone(id: string): Promise<void> {
    await api.delete(`/grant-milestones/${id}`);
  },

  async getDashboard(grantId: string): Promise<MilestoneProgressDashboard> {
    const response = await api.get<ApiResponse<MilestoneProgressDashboard>>(
      `/grants/${grantId}/milestones/dashboard`
    );
    return response.data.data;
  },
};

// ─── Vesting Accelerations ────────────────────────────────────────────────────

export const vestingAccelerationService = {
  async getByGrant(grantId: string): Promise<VestingAcceleration[]> {
    const response = await api.get<ApiResponse<VestingAcceleration[]>>(
      `/grants/${grantId}/accelerations`
    );
    return response.data.data;
  },

  async getPreview(milestoneId: string): Promise<AccelerationPreview> {
    const response = await api.get<ApiResponse<AccelerationPreview>>(
      `/grant-milestones/${milestoneId}/acceleration-preview`
    );
    return response.data.data;
  },

  async applyAcceleration(milestoneId: string): Promise<VestingAcceleration> {
    const response = await api.post<ApiResponse<VestingAcceleration>>(
      `/grant-milestones/${milestoneId}/apply-acceleration`
    );
    return response.data.data;
  },
};
