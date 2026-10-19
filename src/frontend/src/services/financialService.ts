import { api } from './api';
import type {
  FinancialPeriod,
  FinancialPeriodListResponse,
  FinancialFilters,
  CreateFinancialPeriodRequest,
  UpdateFinancialPeriodRequest,
  UpsertRevenueRequest,
  UpsertCashBurnRequest,
  UpsertUnitEconomicsRequest,
  UpsertProfitabilityRequest,
  FinancialDashboard,
  FinancialMetricDefinition,
  CreateMetricDefinitionRequest,
  UpdateMetricDefinitionRequest,
  PeriodMetricValues,
  MetricValues,
  FinancialMetricSeries,
  FinancialDashboardConfig,
  DashboardWidget,
} from '@/types';

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

// ─── Financial Periods ────────────────────────────────────────────────────────

export const financialService = {
  async list(filters?: FinancialFilters): Promise<FinancialPeriodListResponse> {
    const params: Record<string, string | number | undefined> = {};
    if (filters?.companyId) params.companyId = filters.companyId;
    if (filters?.page) params.page = filters.page;
    if (filters?.pageSize) params.pageSize = filters.pageSize;
    if (filters?.year) params.year = filters.year;
    if (filters?.status) params.status = filters.status;
    const response = await api.get<ApiResponse<FinancialPeriodListResponse>>('/financial', { params });
    return response.data.data;
  },

  async getById(id: string): Promise<FinancialPeriod> {
    const response = await api.get<ApiResponse<FinancialPeriod>>(`/financial/${id}`);
    return response.data.data;
  },

  async getDashboard(companyId: string, year: number): Promise<FinancialDashboard> {
    const response = await api.get<ApiResponse<FinancialDashboard>>('/financial/dashboard', {
      params: { companyId, year },
    });
    return response.data.data;
  },

  async create(data: CreateFinancialPeriodRequest): Promise<FinancialPeriod> {
    const response = await api.post<ApiResponse<FinancialPeriod>>('/financial', data);
    return response.data.data;
  },

  async update(id: string, data: UpdateFinancialPeriodRequest): Promise<FinancialPeriod> {
    const response = await api.put<ApiResponse<FinancialPeriod>>(`/financial/${id}`, data);
    return response.data.data;
  },

  async submit(id: string): Promise<FinancialPeriod> {
    const response = await api.post<ApiResponse<FinancialPeriod>>(`/financial/${id}/submit`);
    return response.data.data;
  },

  async approve(id: string): Promise<FinancialPeriod> {
    const response = await api.post<ApiResponse<FinancialPeriod>>(`/financial/${id}/approve`);
    return response.data.data;
  },

  async lock(id: string): Promise<FinancialPeriod> {
    const response = await api.post<ApiResponse<FinancialPeriod>>(`/financial/${id}/lock`);
    return response.data.data;
  },

  async returnToSubmitted(id: string): Promise<FinancialPeriod> {
    const response = await api.post<ApiResponse<FinancialPeriod>>(
      `/financial/${id}/return-to-submitted`
    );
    return response.data.data;
  },

  async delete(id: string): Promise<void> {
    await api.delete(`/financial/${id}`);
  },

  // ─── Metrics upserts ─────────────────────────────────────────────────────────

  async upsertRevenue(id: string, data: UpsertRevenueRequest): Promise<FinancialPeriod> {
    const response = await api.put<ApiResponse<FinancialPeriod>>(
      `/financial/${id}/metrics/revenue`,
      data
    );
    return response.data.data;
  },

  async upsertCashBurn(id: string, data: UpsertCashBurnRequest): Promise<FinancialPeriod> {
    const response = await api.put<ApiResponse<FinancialPeriod>>(
      `/financial/${id}/metrics/cash-burn`,
      data
    );
    return response.data.data;
  },

  async upsertUnitEconomics(id: string, data: UpsertUnitEconomicsRequest): Promise<FinancialPeriod> {
    const response = await api.put<ApiResponse<FinancialPeriod>>(
      `/financial/${id}/metrics/unit-economics`,
      data
    );
    return response.data.data;
  },

  async upsertProfitability(id: string, data: UpsertProfitabilityRequest): Promise<FinancialPeriod> {
    const response = await api.put<ApiResponse<FinancialPeriod>>(
      `/financial/${id}/metrics/profitability`,
      data
    );
    return response.data.data;
  },

  // ─── Configurable metrics ──────────────────────────────────

  async listMetricDefinitions(companyId: string, includeInactive = false): Promise<FinancialMetricDefinition[]> {
    const response = await api.get<ApiResponse<FinancialMetricDefinition[]>>('/financial/metric-definitions', {
      params: { companyId, includeInactive },
    });
    return response.data.data;
  },

  async createMetricDefinition(data: CreateMetricDefinitionRequest): Promise<FinancialMetricDefinition> {
    const response = await api.post<ApiResponse<FinancialMetricDefinition>>('/financial/metric-definitions', data);
    return response.data.data;
  },

  async updateMetricDefinition(id: string, data: UpdateMetricDefinitionRequest): Promise<FinancialMetricDefinition> {
    const response = await api.put<ApiResponse<FinancialMetricDefinition>>(`/financial/metric-definitions/${id}`, data);
    return response.data.data;
  },

  async deleteMetricDefinition(id: string): Promise<void> {
    await api.delete(`/financial/metric-definitions/${id}`);
  },

  async getPeriodValues(periodId: string): Promise<PeriodMetricValues> {
    const response = await api.get<ApiResponse<PeriodMetricValues>>(`/financial/${periodId}/values`);
    return response.data.data;
  },

  async savePeriodValues(periodId: string, values: MetricValues): Promise<PeriodMetricValues> {
    const response = await api.put<ApiResponse<PeriodMetricValues>>(`/financial/${periodId}/values`, { values });
    return response.data.data;
  },

  async getMetricSeries(companyId: string, year: number): Promise<FinancialMetricSeries> {
    const response = await api.get<ApiResponse<FinancialMetricSeries>>('/financial/metric-series', {
      params: { companyId, year },
    });
    return response.data.data;
  },

  async getDashboardConfig(companyId: string): Promise<FinancialDashboardConfig> {
    const response = await api.get<ApiResponse<FinancialDashboardConfig>>('/financial/dashboard-config', {
      params: { companyId },
    });
    return response.data.data;
  },

  async saveDashboardConfig(companyId: string, widgets: DashboardWidget[]): Promise<FinancialDashboardConfig> {
    const response = await api.put<ApiResponse<FinancialDashboardConfig>>('/financial/dashboard-config', {
      companyId,
      widgets,
    });
    return response.data.data;
  },
};
