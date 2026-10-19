import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  LineChart, Line, BarChart, Bar, AreaChart, Area, XAxis, YAxis,
  CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts';
import {
  ArrowLeft, TrendingUp, TrendingDown, Settings2, Plus, Trash2, ChevronUp, ChevronDown, Check, X,
} from 'lucide-react';
import { Button, Card, Spinner } from '@/components/ui';
import {
  useFinancialMetricSeries,
  useFinancialDashboardConfig,
  useSaveFinancialDashboardConfig,
} from '@/hooks';
import { useClientStore } from '@/stores/clientStore';
import {
  monthNames,
  widgetTypeLabels,
  widgetScopeLabels,
  widgetAggregationLabels,
  type WidgetScope,
  type WidgetAggregation,
  type DashboardWidget,
  type DashboardWidgetType,
  type FinancialMetricDefinition,
  type MetricSeriesPoint,
} from '@/types';
import { formatMetricValue, groupByCategory } from '@/utils/financialMetrics';

const COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EF4444', '#06B6D4', '#EC4899'];

const compact = new Intl.NumberFormat('pt-BR', { notation: 'compact', maximumFractionDigits: 1 });

type DefMap = Map<string, FinancialMetricDefinition>;

// ─── KPI widgets ──────────────────────────────────────────────────────────────

const closedPoints = (points: MetricSeriesPoint[]) =>
  points.filter((p) => p.status === 'approved' || p.status === 'locked').sort((a, b) => a.month - b.month);

function pctChange(current: number | null, previous: number | null): number | null {
  return current != null && previous != null && previous !== 0
    ? ((current - previous) / Math.abs(previous)) * 100
    : null;
}

function TrendBadge({ trend, suffix }: { trend: number | null; suffix: string }) {
  if (trend == null) return null;
  return (
    <div
      className={`flex items-center gap-1 mt-2 text-xs font-medium ${
        trend > 0 ? 'text-green-600' : trend < 0 ? 'text-red-500' : 'text-gray-400'
      }`}
    >
      {trend >= 0 ? <TrendingUp className="w-3.5 h-3.5" /> : <TrendingDown className="w-3.5 h-3.5" />}
      {Math.abs(trend).toFixed(1)}% {suffix}
    </div>
  );
}

function aggregate(values: number[], aggregation: WidgetAggregation): number | null {
  if (values.length === 0) return null;
  if (aggregation === 'sum') return values.reduce((a, b) => a + b, 0);
  if (aggregation === 'avg') return values.reduce((a, b) => a + b, 0) / values.length;
  return values[values.length - 1];
}

/** Annual KPI: consolidates the approved/locked periods of the year. */
function AnnualKpiWidget({ widget, points, defs }: { widget: DashboardWidget; points: MetricSeriesPoint[]; defs: DefMap }) {
  const def = defs.get(widget.metricKeys[0]);
  if (!def) return <MissingMetric widget={widget} />;

  const values = closedPoints(points)
    .map((p) => p.values[def.key])
    .filter((v): v is number => v != null);
  const value = aggregate(values, widget.aggregation);

  return (
    <Card className="p-5">
      <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">{widget.title}</p>
      <p className="text-2xl font-bold text-gray-900 mt-1">{formatMetricValue(value, def)}</p>
      <p className="text-sm text-gray-400 mt-0.5">
        {def.label} · {widgetAggregationLabels[widget.aggregation]}
      </p>
      <p className="text-xs text-gray-400 mt-1">{values.length} competência(s) fechada(s)</p>
    </Card>
  );
}

/** Monthly KPI: the value of one competência compared with the previous month. */
function MonthlyKpiWidget({
  widget, point, previous, defs,
}: { widget: DashboardWidget; point: MetricSeriesPoint | undefined; previous: MetricSeriesPoint | undefined; defs: DefMap }) {
  const def = defs.get(widget.metricKeys[0]);
  if (!def) return <MissingMetric widget={widget} />;

  const value = point?.values[def.key] ?? null;
  const prev = previous?.values[def.key] ?? null;

  return (
    <Card className="p-5">
      <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">{widget.title}</p>
      <p className="text-2xl font-bold text-gray-900 mt-1">{formatMetricValue(value, def)}</p>
      <p className="text-sm text-gray-400 mt-0.5">{def.label}</p>
      <TrendBadge trend={pctChange(value, prev)} suffix="MoM" />
    </Card>
  );
}

/** All active metrics of one competência with variation against the previous month. */
function CompetenceTable({
  defs, point, previous,
}: { defs: FinancialMetricDefinition[]; point: MetricSeriesPoint | undefined; previous: MetricSeriesPoint | undefined }) {
  const groups = groupByCategory(defs);
  return (
    <Card className="p-5">
      <h3 className="text-sm font-semibold text-gray-700 mb-4">Todas as métricas da competência</h3>
      <table className="w-full text-sm">
        <thead>
          <tr className="text-xs text-gray-500 text-left">
            <th className="pb-2 font-medium">Métrica</th>
            <th className="pb-2 font-medium text-right">Competência</th>
            <th className="pb-2 font-medium text-right">Mês anterior</th>
            <th className="pb-2 font-medium text-right">Var.</th>
          </tr>
        </thead>
        <tbody>
          {groups.map(([category, items]) => (
            <>
              <tr key={`c-${category}`}>
                <td colSpan={4} className="pt-3 pb-1 text-xs font-semibold text-gray-400 uppercase">{category}</td>
              </tr>
              {items.map((d) => {
                const cur = point?.values[d.key] ?? null;
                const prev = previous?.values[d.key] ?? null;
                const change = pctChange(cur, prev);
                return (
                  <tr key={d.key} className="border-t border-gray-50">
                    <td className="py-1.5 text-gray-700">{d.label}</td>
                    <td className="py-1.5 text-right font-medium text-gray-900">{formatMetricValue(cur, d)}</td>
                    <td className="py-1.5 text-right text-gray-500">{formatMetricValue(prev, d)}</td>
                    <td className={`py-1.5 text-right ${change == null ? 'text-gray-300' : change >= 0 ? 'text-green-600' : 'text-red-500'}`}>
                      {change == null ? '—' : `${change >= 0 ? '+' : ''}${change.toFixed(1)}%`}
                    </td>
                  </tr>
                );
              })}
            </>
          ))}
        </tbody>
      </table>
    </Card>
  );
}

function MissingMetric({ widget }: { widget: DashboardWidget }) {
  return (
    <Card className="p-5">
      <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">{widget.title}</p>
      <p className="text-sm text-gray-400 mt-2">Métrica não disponível.</p>
    </Card>
  );
}

// ─── Chart widget ─────────────────────────────────────────────────────────────

function ChartWidget({
  widget, points, defs, year,
}: { widget: DashboardWidget; points: MetricSeriesPoint[]; defs: DefMap; year: number }) {
  const series = widget.metricKeys.map((k) => defs.get(k)).filter((d): d is FinancialMetricDefinition => !!d);

  const data = points.map((p) => {
    const row: Record<string, string | number | null> = { month: monthNames[p.month - 1] ?? `M${p.month}` };
    for (const d of series) row[d.key] = p.values[d.key] ?? null;
    return row;
  });
  const hasData = data.some((row) => series.some((d) => row[d.key] != null));

  const common = { data, margin: { top: 5, right: 15, left: 0, bottom: 5 } };
  const axes = (
    <>
      <CartesianGrid strokeDasharray="3 3" stroke="#F3F4F6" />
      <XAxis dataKey="month" tick={{ fontSize: 11 }} />
      <YAxis tick={{ fontSize: 11 }} tickFormatter={(v: number) => compact.format(v)} />
      <Tooltip
        formatter={(v: number, _n: string, item) => {
          const def = defs.get(String(item.dataKey));
          return def ? formatMetricValue(v, def) : v;
        }}
      />
      {series.length > 1 && <Legend />}
    </>
  );

  return (
    <Card className="p-5">
      <h3 className="text-sm font-semibold text-gray-700 mb-4">{widget.title} — {year}</h3>
      {!hasData ? (
        <p className="text-sm text-gray-400 text-center py-12">Sem dados para o período.</p>
      ) : (
        <ResponsiveContainer width="100%" height={220}>
          {widget.widgetType === 'bar' ? (
            <BarChart {...common}>
              {axes}
              {series.map((d, i) => (
                <Bar key={d.key} dataKey={d.key} name={d.label} fill={COLORS[i % COLORS.length]} radius={[4, 4, 0, 0]} />
              ))}
            </BarChart>
          ) : widget.widgetType === 'area' ? (
            <AreaChart {...common}>
              {axes}
              {series.map((d, i) => (
                <Area
                  key={d.key} type="monotone" dataKey={d.key} name={d.label} connectNulls
                  stroke={COLORS[i % COLORS.length]} fill={COLORS[i % COLORS.length]} fillOpacity={0.15} strokeWidth={2}
                />
              ))}
            </AreaChart>
          ) : (
            <LineChart {...common}>
              {axes}
              {series.map((d, i) => (
                <Line
                  key={d.key} type="monotone" dataKey={d.key} name={d.label} connectNulls
                  stroke={COLORS[i % COLORS.length]} strokeWidth={2} dot={{ r: 3 }}
                />
              ))}
            </LineChart>
          )}
        </ResponsiveContainer>
      )}
    </Card>
  );
}

// ─── Widget editor ────────────────────────────────────────────────────────────

function WidgetEditor({
  widgets, defs, onChange,
}: { widgets: DashboardWidget[]; defs: FinancialMetricDefinition[]; onChange: (w: DashboardWidget[]) => void }) {
  const update = (i: number, patch: Partial<DashboardWidget>) =>
    onChange(widgets.map((w, idx) => (idx === i ? { ...w, ...patch } : w)));

  const move = (i: number, dir: -1 | 1) => {
    const j = i + dir;
    if (j < 0 || j >= widgets.length) return;
    const next = [...widgets];
    [next[i], next[j]] = [next[j], next[i]];
    onChange(next);
  };

  const toggleMetric = (i: number, key: string) => {
    const w = widgets[i];
    if (w.widgetType === 'kpi') return update(i, { metricKeys: [key] });
    const keys = w.metricKeys.includes(key) ? w.metricKeys.filter((k) => k !== key) : [...w.metricKeys, key];
    update(i, { metricKeys: keys });
  };

  const changeType = (i: number, widgetType: DashboardWidgetType) => {
    const w = widgets[i];
    update(i, {
      widgetType,
      metricKeys: widgetType === 'kpi' ? w.metricKeys.slice(0, 1) : w.metricKeys,
      scope: widgetType === 'kpi' ? w.scope : 'annual',
      size: 'small',
    });
  };

  const add = () =>
    onChange([
      ...widgets,
      { title: 'Novo widget', widgetType: 'line', metricKeys: defs[0] ? [defs[0].key] : [], size: 'small', scope: 'annual', aggregation: 'last', displayOrder: widgets.length * 10 },
    ]);

  return (
    <div className="space-y-3">
      {widgets.map((w, i) => (
        <Card key={w.id ?? `new-${i}`} className="p-4 space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <input
              aria-label="Título do widget"
              value={w.title}
              onChange={(e) => update(i, { title: e.target.value })}
              className="flex-1 min-w-[160px] text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <select
              aria-label="Tipo do widget"
              value={w.widgetType}
              onChange={(e) => changeType(i, e.target.value as DashboardWidgetType)}
              className="text-sm border border-gray-200 rounded-lg px-3 py-2"
            >
              {(Object.keys(widgetTypeLabels) as DashboardWidgetType[]).map((t) => (
                <option key={t} value={t}>{widgetTypeLabels[t]}</option>
              ))}
            </select>
            {w.widgetType === 'kpi' && (
              <>
                <select
                  aria-label="Escopo do widget"
                  value={w.scope}
                  onChange={(e) => update(i, { scope: e.target.value as WidgetScope })}
                  className="text-sm border border-gray-200 rounded-lg px-3 py-2"
                >
                  {(Object.keys(widgetScopeLabels) as WidgetScope[]).map((sc) => (
                    <option key={sc} value={sc}>{widgetScopeLabels[sc]}</option>
                  ))}
                </select>
                {w.scope === 'annual' && (
                  <select
                    aria-label="Agregação do widget"
                    value={w.aggregation}
                    onChange={(e) => update(i, { aggregation: e.target.value as WidgetAggregation })}
                    className="text-sm border border-gray-200 rounded-lg px-3 py-2"
                  >
                    {(Object.keys(widgetAggregationLabels) as WidgetAggregation[]).map((ag) => (
                      <option key={ag} value={ag}>{widgetAggregationLabels[ag]}</option>
                    ))}
                  </select>
                )}
              </>
            )}
            {w.widgetType !== 'kpi' && (
              <select
                aria-label="Largura do widget"
                value={w.size}
                onChange={(e) => update(i, { size: e.target.value as 'small' | 'large' })}
                className="text-sm border border-gray-200 rounded-lg px-3 py-2"
              >
                <option value="small">Meia largura</option>
                <option value="large">Largura total</option>
              </select>
            )}
            <button onClick={() => move(i, -1)} className="p-2 text-gray-400 hover:text-gray-700" aria-label="Mover para cima"><ChevronUp size={16} /></button>
            <button onClick={() => move(i, 1)} className="p-2 text-gray-400 hover:text-gray-700" aria-label="Mover para baixo"><ChevronDown size={16} /></button>
            <button
              onClick={() => onChange(widgets.filter((_, idx) => idx !== i))}
              className="p-2 text-red-400 hover:text-red-600"
              aria-label="Remover widget"
            >
              <Trash2 size={16} />
            </button>
          </div>
          <div className="flex flex-wrap gap-2">
            {defs.map((d) => {
              const on = w.metricKeys.includes(d.key);
              return (
                <button
                  key={d.key}
                  type="button"
                  onClick={() => toggleMetric(i, d.key)}
                  className={`px-2.5 py-1 text-xs rounded-full border transition-colors ${
                    on ? 'bg-blue-50 border-blue-300 text-blue-700' : 'border-gray-200 text-gray-500 hover:bg-gray-50'
                  }`}
                >
                  {d.label}
                </button>
              );
            })}
          </div>
        </Card>
      ))}
      <Button size="sm" variant="secondary" icon={<Plus size={14} />} onClick={add}>
        Adicionar widget
      </Button>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function FinancialDashboardPage() {
  const navigate = useNavigate();
  const { selectedCompanyId } = useClientStore();
  const [year, setYear] = useState(new Date().getFullYear());
  const [view, setView] = useState<'annual' | 'monthly'>('annual');
  const [month, setMonth] = useState<number | null>(null);
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState<DashboardWidget[]>([]);

  const companyId = selectedCompanyId || '';
  const { data: series, isLoading: seriesLoading } = useFinancialMetricSeries(companyId, year);
  const { data: config, isLoading: configLoading } = useFinancialDashboardConfig(companyId);
  const saveConfig = useSaveFinancialDashboardConfig();

  const activeDefs = useMemo(() => (config?.definitions ?? []).filter((d) => d.isActive), [config]);
  const defMap: DefMap = useMemo(
    () => new Map((config?.definitions ?? []).map((d) => [d.key, d])),
    [config],
  );
  const points = series?.points ?? [];

  // Competência selection defaults to the latest period that has data.
  const selectedMonth = month ?? points[points.length - 1]?.month ?? null;
  const monthPoint = points.find((p) => p.month === selectedMonth);
  const previousPoint = selectedMonth != null
    ? [...points].filter((p) => p.month < selectedMonth).sort((a, b) => b.month - a.month)[0]
    : undefined;

  const startEditing = () => {
    setDraft((config?.widgets ?? []).map((w) => ({ ...w, metricKeys: [...w.metricKeys] })));
    setEditing(true);
  };

  const handleSave = () => {
    saveConfig.mutate(
      { companyId, widgets: draft.map((w, i) => ({ ...w, displayOrder: i * 10 })) },
      { onSuccess: () => setEditing(false) },
    );
  };

  if (!selectedCompanyId) {
    return (
      <div className="text-center py-20 text-gray-400">
        <p>Selecione uma empresa para ver o dashboard.</p>
      </div>
    );
  }

  const widgets = config?.widgets ?? [];
  const annualKpis = widgets.filter((w) => w.widgetType === 'kpi' && w.scope === 'annual');
  const monthlyKpis = widgets.filter((w) => w.widgetType === 'kpi' && w.scope === 'monthly');
  const charts = widgets.filter((w) => w.widgetType !== 'kpi');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button onClick={() => navigate('/financial')} className="p-2 rounded-xl hover:bg-gray-100 text-gray-500">
          <ArrowLeft className="w-5 h-5" />
        </button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold text-gray-900">Dashboard Financeiro</h1>
          <p className="text-sm text-gray-400 mt-0.5">Métricas e tendências ao longo do ano</p>
        </div>
        <div className="flex items-center gap-2">
          <button onClick={() => setYear((y) => y - 1)} className="p-2 rounded-lg hover:bg-gray-100 text-gray-500">‹</button>
          <span className="text-sm font-semibold text-gray-700 w-12 text-center">{year}</span>
          <button onClick={() => setYear((y) => y + 1)} className="p-2 rounded-lg hover:bg-gray-100 text-gray-500">›</button>
        </div>
        {editing ? (
          <div className="flex gap-2">
            <Button size="sm" variant="secondary" icon={<X size={14} />} onClick={() => setEditing(false)}>
              Cancelar
            </Button>
            <Button size="sm" icon={<Check size={14} />} onClick={handleSave} loading={saveConfig.isPending}>
              Salvar
            </Button>
          </div>
        ) : (
          <Button size="sm" variant="secondary" icon={<Settings2 size={14} />} onClick={startEditing} disabled={!config}>
            Configurar
          </Button>
        )}
      </div>

      {seriesLoading || configLoading ? (
        <div className="flex justify-center py-20">
          <Spinner className="w-8 h-8" />
        </div>
      ) : editing ? (
        <WidgetEditor widgets={draft} defs={activeDefs} onChange={setDraft} />
      ) : (
        <>
          <div className="flex gap-1 border-b border-gray-200" role="tablist">
            {(['annual', 'monthly'] as const).map((v) => (
              <button
                key={v}
                role="tab"
                aria-selected={view === v}
                onClick={() => setView(v)}
                className={`px-4 py-2 text-sm font-medium -mb-px border-b-2 transition-colors ${
                  view === v ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                {v === 'annual' ? 'Visão anual' : 'Por competência'}
              </button>
            ))}
          </div>

          {view === 'annual' ? (
            annualKpis.length + charts.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-12">
                Nenhum widget anual configurado. Use “Configurar” para adicionar.
              </p>
            ) : (
              <>
                {annualKpis.length > 0 && (
                  <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
                    {annualKpis.map((w, i) => (
                      <AnnualKpiWidget key={w.id ?? i} widget={w} points={points} defs={defMap} />
                    ))}
                  </div>
                )}
                {charts.length > 0 && (
                  <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
                    {charts.map((w, i) => (
                      <div key={w.id ?? i} className={w.size === 'large' ? 'xl:col-span-2' : ''}>
                        <ChartWidget widget={w} points={points} defs={defMap} year={year} />
                      </div>
                    ))}
                  </div>
                )}
              </>
            )
          ) : points.length === 0 ? (
            <p className="text-sm text-gray-400 text-center py-12">Nenhuma competência cadastrada em {year}.</p>
          ) : (
            <>
              <div className="flex items-center gap-3">
                <label htmlFor="competence" className="text-sm font-medium text-gray-600">Competência</label>
                <select
                  id="competence"
                  value={selectedMonth ?? ''}
                  onChange={(e) => setMonth(Number(e.target.value))}
                  className="text-sm border border-gray-200 rounded-lg px-3 py-2"
                >
                  {points.map((p) => (
                    <option key={p.periodId} value={p.month}>
                      {monthNames[p.month - 1]}/{p.year}
                    </option>
                  ))}
                </select>
                {monthPoint && monthPoint.status !== 'approved' && monthPoint.status !== 'locked' && (
                  <span className="text-xs text-yellow-600">Competência ainda não aprovada</span>
                )}
              </div>
              {monthlyKpis.length > 0 && (
                <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
                  {monthlyKpis.map((w, i) => (
                    <MonthlyKpiWidget key={w.id ?? i} widget={w} point={monthPoint} previous={previousPoint} defs={defMap} />
                  ))}
                </div>
              )}
              <CompetenceTable defs={activeDefs} point={monthPoint} previous={previousPoint} />
            </>
          )}
        </>
      )}
    </div>
  );
}
