// Financial Types — mirrors backend DTOs and Entities

// ─── Enums ────────────────────────────────────────────────────────────────────

export enum FinancialPeriodStatus {
  Draft = 'draft',
  Submitted = 'submitted',
  Approved = 'approved',
  Locked = 'locked',
}

// ─── Labels ───────────────────────────────────────────────────────────────────

export const financialPeriodStatusLabels: Record<string, string> = {
  draft: 'Rascunho',
  submitted: 'Submetido',
  approved: 'Aprovado',
  locked: 'Bloqueado',
};

export const financialPeriodStatusColors: Record<string, string> = {
  draft: 'bg-gray-100 text-gray-700',
  submitted: 'bg-yellow-100 text-yellow-700',
  approved: 'bg-green-100 text-green-700',
  locked: 'bg-blue-100 text-blue-700',
};

export const monthNames: string[] = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
];

export const runwayStatusColors: Record<string, string> = {
  green: 'text-green-600',
  yellow: 'text-yellow-600',
  red: 'text-red-600',
};

// ─── Interfaces ───────────────────────────────────────────────────────────────

export interface FinancialMetric {
  id: string;
  periodId: string;
  // Revenue
  grossRevenue: number | null;
  netRevenue: number | null;
  mrr: number | null;
  arr: number | null;
  // Cash & Burn
  cashBalance: number | null;
  burnRate: number | null;
  runwayMonths: number | null;
  runwayStatus: string | null;
  // Unit Economics
  customerCount: number | null;
  churnRate: number | null;
  cac: number | null;
  ltv: number | null;
  ltvToCacRatio: number | null;
  nps: number | null;
  // Profitability
  ebitda: number | null;
  ebitdaMargin: number | null;
  netIncome: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface FinancialPeriod {
  id: string;
  clientId: string;
  companyId: string;
  year: number;
  month: number;
  periodLabel: string;
  status: string;
  notes: string | null;
  submittedAt: string | null;
  approvedAt: string | null;
  lockedAt: string | null;
  createdAt: string;
  updatedAt: string;
  metrics: FinancialMetric | null;
}

export interface FinancialPeriodListResponse {
  items: FinancialPeriod[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface FinancialTrend {
  mrrGrowthPercent: number | null;
  arrCurrentMonth: number | null;
  avgBurnRate3Months: number | null;
  runwayMonths: number | null;
  runwayStatus: string | null;
  avgChurnRate3Months: number | null;
}

export interface FinancialDashboard {
  companyId: string;
  year: number;
  periods: FinancialPeriod[];
  trend: FinancialTrend | null;
}

// ─── Filters ──────────────────────────────────────────────────────────────────

export interface FinancialFilters {
  companyId?: string;
  page?: number;
  pageSize?: number;
  year?: number;
  status?: string;
}

// ─── Requests ─────────────────────────────────────────────────────────────────

export interface CreateFinancialPeriodRequest {
  companyId: string;
  year: number;
  month: number;
  notes?: string;
}

export interface UpdateFinancialPeriodRequest {
  notes?: string;
}

export interface UpsertRevenueRequest {
  grossRevenue?: number;
  netRevenue?: number;
  mrr?: number;
}

export interface UpsertCashBurnRequest {
  cashBalance?: number;
  burnRate?: number;
}

export interface UpsertUnitEconomicsRequest {
  customerCount?: number;
  churnRate?: number;
  cac?: number;
  ltv?: number;
  nps?: number;
}

export interface UpsertProfitabilityRequest {
  ebitda?: number;
  netIncome?: number;
}

// ─── Configurable metrics & dashboard ─────────────────────────────────────────

export type MetricDataType = 'currency' | 'number' | 'integer' | 'percentage';
export type DashboardWidgetType = 'kpi' | 'line' | 'bar' | 'area';
export type WidgetScope = 'annual' | 'monthly';
export type WidgetAggregation = 'last' | 'sum' | 'avg';

export const widgetScopeLabels: Record<WidgetScope, string> = {
  annual: 'Anual',
  monthly: 'Por competência',
};

export const widgetAggregationLabels: Record<WidgetAggregation, string> = {
  last: 'Último valor do ano',
  sum: 'Soma do ano',
  avg: 'Média do ano',
};

export const metricDataTypeLabels: Record<MetricDataType, string> = {
  currency: 'Moeda (R$)',
  number: 'Número',
  integer: 'Número inteiro',
  percentage: 'Percentual (%)',
};

export const widgetTypeLabels: Record<DashboardWidgetType, string> = {
  kpi: 'Indicador (KPI)',
  line: 'Linhas',
  bar: 'Barras',
  area: 'Área',
};

export interface FinancialMetricDefinition {
  id: string;
  companyId: string;
  key: string;
  label: string;
  category: string;
  dataType: MetricDataType;
  unit: string | null;
  description: string | null;
  displayOrder: number;
  isSystem: boolean;
  isCalculated: boolean;
  isActive: boolean;
}

export interface CreateMetricDefinitionRequest {
  companyId: string;
  key?: string;
  label: string;
  category?: string;
  dataType: MetricDataType;
  unit?: string;
  description?: string;
}

export interface UpdateMetricDefinitionRequest {
  label: string;
  category?: string;
  dataType?: MetricDataType;
  unit?: string;
  description?: string;
  displayOrder?: number;
  isActive?: boolean;
}

export type MetricValues = Record<string, number | null>;

export interface PeriodMetricValues {
  periodId: string;
  values: MetricValues;
}

export interface MetricSeriesPoint {
  periodId: string;
  year: number;
  month: number;
  status: string;
  values: MetricValues;
}

export interface FinancialMetricSeries {
  companyId: string;
  year: number;
  points: MetricSeriesPoint[];
}

export interface DashboardWidget {
  id?: string;
  title: string;
  widgetType: DashboardWidgetType;
  metricKeys: string[];
  size: 'small' | 'large';
  scope: WidgetScope;
  aggregation: WidgetAggregation;
  displayOrder: number;
}

export interface FinancialDashboardConfig {
  companyId: string;
  definitions: FinancialMetricDefinition[];
  widgets: DashboardWidget[];
}
