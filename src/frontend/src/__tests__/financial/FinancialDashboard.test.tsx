import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import FinancialDashboardPage from '../../pages/financial/FinancialDashboardPage';

vi.mock('../../hooks', () => ({
  useFinancialMetricSeries: vi.fn(),
  useFinancialDashboardConfig: vi.fn(),
  useSaveFinancialDashboardConfig: vi.fn(),
}));

vi.mock('../../stores/clientStore', () => ({
  useClientStore: vi.fn(),
}));

import {
  useFinancialMetricSeries,
  useFinancialDashboardConfig,
  useSaveFinancialDashboardConfig,
} from '../../hooks';
import { useClientStore } from '../../stores/clientStore';

const def = (key: string, label: string, dataType = 'currency') => ({
  id: key,
  companyId: 'company-1',
  key,
  label,
  category: 'Geral',
  dataType,
  unit: null,
  description: null,
  displayOrder: 0,
  isSystem: false,
  isCalculated: false,
  isActive: true,
});

const config = {
  companyId: 'company-1',
  definitions: [def('mrr', 'MRR'), def('tickets_per_day', 'Tickets por dia', 'integer')],
  widgets: [
    { id: 'w1', title: 'Receita Recorrente', widgetType: 'kpi', metricKeys: ['mrr'], size: 'small', scope: 'annual', aggregation: 'last', displayOrder: 0 },
    { id: 'w2', title: 'Volume de Tickets', widgetType: 'kpi', metricKeys: ['tickets_per_day'], size: 'small', scope: 'monthly', aggregation: 'last', displayOrder: 10 },
    { id: 'w3', title: 'Tickets acumulados', widgetType: 'kpi', metricKeys: ['tickets_per_day'], size: 'small', scope: 'annual', aggregation: 'sum', displayOrder: 20 },
  ],
};

const series = {
  companyId: 'company-1',
  year: 2026,
  points: [
    { periodId: 'p1', year: 2026, month: 1, status: 'approved', values: { mrr: 100000, tickets_per_day: 40 } },
    { periodId: 'p2', year: 2026, month: 2, status: 'approved', values: { mrr: 110000, tickets_per_day: 44 } },
  ],
};

function mockHooks(overrides: { config?: unknown; series?: unknown; loading?: boolean } = {}) {
  vi.mocked(useFinancialMetricSeries).mockReturnValue({
    data: 'series' in overrides ? overrides.series : series,
    isLoading: overrides.loading ?? false,
  } as never);
  vi.mocked(useFinancialDashboardConfig).mockReturnValue({
    data: 'config' in overrides ? overrides.config : config,
    isLoading: false,
  } as never);
  vi.mocked(useSaveFinancialDashboardConfig).mockReturnValue({ mutate: vi.fn(), isPending: false } as never);
}

describe('FinancialDashboardPage (configurable)', () => {
  beforeEach(() => {
    vi.mocked(useClientStore).mockReturnValue({ selectedCompanyId: 'company-1' } as never);
  });

  it('asks to select a company when none is selected', () => {
    vi.mocked(useClientStore).mockReturnValue({ selectedCompanyId: null } as never);
    mockHooks();
    render(<MemoryRouter><FinancialDashboardPage /></MemoryRouter>);
    expect(screen.getByText(/Selecione uma empresa/i)).toBeInTheDocument();
  });

  it('shows spinner while loading', () => {
    mockHooks({ loading: true });
    render(<MemoryRouter><FinancialDashboardPage /></MemoryRouter>);
    expect(document.querySelector('.spinner')).toBeTruthy();
  });

  it('annual view consolidates closed periods using the widget aggregation', () => {
    mockHooks();
    render(<MemoryRouter><FinancialDashboardPage /></MemoryRouter>);
    expect(screen.getByText('Dashboard Financeiro')).toBeInTheDocument();
    expect(screen.getByText('Receita Recorrente')).toBeInTheDocument();
    expect(screen.getByText('Tickets acumulados')).toBeInTheDocument();
    expect(screen.getByText('84')).toBeInTheDocument(); // 40 + 44
    expect(screen.queryByText('Volume de Tickets')).not.toBeInTheDocument();
  });

  it('competence view shows the selected month with MoM and the metrics table', () => {
    mockHooks();
    render(<MemoryRouter><FinancialDashboardPage /></MemoryRouter>);
    fireEvent.click(screen.getByRole('tab', { name: 'Por competência' }));
    expect(screen.getByText('Volume de Tickets')).toBeInTheDocument();
    expect(screen.queryByText('Tickets acumulados')).not.toBeInTheDocument();
    expect(screen.getByText('10.0% MoM')).toBeInTheDocument();
    expect(screen.getByText('Todas as métricas da competência')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Competência'), { target: { value: '1' } });
    expect(screen.queryByText('10.0% MoM')).not.toBeInTheDocument();
  });

  it('shows empty state when no widgets are configured', () => {
    mockHooks({ config: { ...config, widgets: [] } });
    render(<MemoryRouter><FinancialDashboardPage /></MemoryRouter>);
    expect(screen.getByText(/Nenhum widget anual configurado/i)).toBeInTheDocument();
  });

  it('enters configuration mode and allows adding a widget', () => {
    mockHooks();
    render(<MemoryRouter><FinancialDashboardPage /></MemoryRouter>);
    fireEvent.click(screen.getByText('Configurar'));
    expect(screen.getAllByLabelText('Título do widget')).toHaveLength(3);
    fireEvent.click(screen.getByText('Adicionar widget'));
    expect(screen.getAllByLabelText('Título do widget')).toHaveLength(4);
    expect(screen.getByText('Salvar')).toBeInTheDocument();
  });

  it('changes year when navigation buttons are clicked', () => {
    const currentYear = new Date().getFullYear();
    mockHooks();
    render(<MemoryRouter><FinancialDashboardPage /></MemoryRouter>);
    fireEvent.click(screen.getByText('‹'));
    expect(screen.getByText(String(currentYear - 1))).toBeInTheDocument();
  });
});
