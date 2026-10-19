import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Send, CheckCircle, Lock, Settings2 } from 'lucide-react';
import { Button, Card, Spinner, Badge } from '@/components/ui';
import {
  useFinancialPeriods,
  useSubmitFinancialPeriod,
  useApproveFinancialPeriod,
  useLockFinancialPeriod,
  useMetricDefinitions,
  usePeriodMetricValues,
  useSavePeriodMetricValues,
} from '@/hooks';
import { useClientStore } from '@/stores/clientStore';
import {
  financialPeriodStatusLabels,
  financialPeriodStatusColors,
  monthNames,
  type FinancialMetricDefinition,
  type MetricValues,
} from '@/types';
import { formatMetricValue, groupByCategory } from '@/utils/financialMetrics';

function inputPrefix(def: FinancialMetricDefinition): string {
  if (def.dataType === 'currency') return 'R$';
  if (def.dataType === 'percentage') return '%';
  return def.unit ?? '';
}

function MetricInput({
  def,
  value,
  calculatedValue,
  onChange,
  disabled,
}: {
  def: FinancialMetricDefinition;
  value: string;
  calculatedValue: number | null | undefined;
  onChange: (v: string) => void;
  disabled: boolean;
}) {
  const prefix = inputPrefix(def);

  return (
    <div>
      <label className="block text-xs font-medium text-gray-600 mb-1" title={def.description ?? undefined}>
        {def.label}
        {def.isCalculated && <span className="ml-1 text-gray-400">(calculado)</span>}
      </label>
      {def.isCalculated ? (
        <div className="px-3 py-2 text-sm text-gray-500 bg-gray-50 border border-gray-200 rounded-lg">
          {formatMetricValue(calculatedValue, def)}
        </div>
      ) : (
        <div className="flex items-center border border-gray-200 rounded-lg overflow-hidden focus-within:ring-2 focus-within:ring-blue-500">
          {prefix && (
            <span className="px-2 py-2 text-xs text-gray-400 bg-gray-50 border-r border-gray-200">{prefix}</span>
          )}
          <input
            type="number"
            step={def.dataType === 'integer' ? 1 : 'any'}
            aria-label={def.label}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            disabled={disabled}
            className="flex-1 px-3 py-2 text-sm text-gray-900 bg-transparent focus:outline-none disabled:cursor-not-allowed disabled:text-gray-400"
          />
        </div>
      )}
    </div>
  );
}

function toDraft(values: MetricValues | undefined): Record<string, string> {
  const draft: Record<string, string> = {};
  for (const [key, v] of Object.entries(values ?? {})) draft[key] = v != null ? String(v) : '';
  return draft;
}

export default function FinancialPeriodPage() {
  const { year, month } = useParams<{ year: string; month: string }>();
  const navigate = useNavigate();
  const { selectedCompanyId } = useClientStore();

  const filters = {
    companyId: selectedCompanyId || undefined,
    year: Number(year),
    pageSize: 12,
  };
  const { data: list } = useFinancialPeriods(filters);
  const period = list?.items.find((p) => p.year === Number(year) && p.month === Number(month));

  const submitPeriod = useSubmitFinancialPeriod();
  const approvePeriod = useApproveFinancialPeriod();
  const lockPeriod = useLockFinancialPeriod();
  const saveValues = useSavePeriodMetricValues();

  const { data: definitions } = useMetricDefinitions(period?.companyId ?? '');
  const { data: periodValues } = usePeriodMetricValues(period?.id ?? '');

  const locked = period?.status === 'locked' || period?.status === 'approved';

  const [draft, setDraft] = useState<Record<string, string>>({});

  useEffect(() => {
    if (periodValues) setDraft(toDraft(periodValues.values));
  }, [periodValues]);

  if (!period && !list) {
    return (
      <div className="flex justify-center py-20">
        <Spinner className="w-8 h-8" />
      </div>
    );
  }

  if (!period) {
    return (
      <div className="text-center py-20 text-gray-400">
        <p>
          Período {monthNames[Number(month) - 1]}/{year} não encontrado.
        </p>
        <Button variant="secondary" size="sm" className="mt-4" onClick={() => navigate('/financial')}>
          Voltar
        </Button>
      </div>
    );
  }

  const label = `${monthNames[period.month - 1]}/${period.year}`;
  const groups = groupByCategory(definitions ?? []);

  const handleSave = () => {
    const values: MetricValues = {};
    for (const def of definitions ?? []) {
      if (def.isCalculated) continue;
      const raw = draft[def.key];
      if (raw === undefined) continue;
      const n = parseFloat(raw);
      values[def.key] = raw === '' || isNaN(n) ? null : n;
    }
    saveValues.mutate({ id: period.id, values });
  };

  return (
    <div className="max-w-3xl space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate('/financial')}
          className="p-2 rounded-lg hover:bg-gray-100 text-gray-400 transition-colors"
        >
          <ArrowLeft size={18} />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-bold text-gray-900">Período {label}</h1>
            <Badge className={financialPeriodStatusColors[period.status] ?? ''}>
              {financialPeriodStatusLabels[period.status] ?? period.status}
            </Badge>
          </div>
        </div>
        <div className="flex gap-2">
          <Button
            icon={<Settings2 size={14} />}
            size="sm"
            variant="secondary"
            onClick={() => navigate('/financial/metrics')}
          >
            Métricas
          </Button>
          {period.status === 'draft' && (
            <Button
              icon={<Send size={14} />}
              size="sm"
              onClick={() => submitPeriod.mutate(period.id)}
              loading={submitPeriod.isPending}
            >
              Submeter
            </Button>
          )}
          {period.status === 'submitted' && (
            <Button
              icon={<CheckCircle size={14} />}
              size="sm"
              variant="success"
              onClick={() => approvePeriod.mutate(period.id)}
              loading={approvePeriod.isPending}
            >
              Aprovar
            </Button>
          )}
          {period.status === 'approved' && (
            <Button
              icon={<Lock size={14} />}
              size="sm"
              variant="secondary"
              onClick={() => lockPeriod.mutate(period.id)}
              loading={lockPeriod.isPending}
            >
              Bloquear
            </Button>
          )}
        </div>
      </div>

      {!definitions || !periodValues ? (
        <div className="flex justify-center py-10">
          <Spinner className="w-6 h-6" />
        </div>
      ) : (
        <>
          {groups.map(([category, defs]) => (
            <Card key={category}>
              <div className="p-5 space-y-4">
                <h2 className="text-sm font-semibold text-gray-800">{category}</h2>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  {defs.map((def) => (
                    <MetricInput
                      key={def.id}
                      def={def}
                      value={draft[def.key] ?? ''}
                      calculatedValue={periodValues.values[def.key]}
                      onChange={(v) => setDraft((d) => ({ ...d, [def.key]: v }))}
                      disabled={locked}
                    />
                  ))}
                </div>
              </div>
            </Card>
          ))}

          {groups.length === 0 && (
            <p className="text-sm text-gray-400 text-center py-10">
              Nenhuma métrica configurada para esta empresa.
            </p>
          )}

          {!locked && groups.length > 0 && (
            <div className="flex justify-end">
              <Button onClick={handleSave} loading={saveValues.isPending}>
                Salvar métricas
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
