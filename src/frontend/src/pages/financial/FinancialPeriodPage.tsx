import { useState, useEffect, useMemo } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Send, CheckCircle, Lock, Settings2, Paperclip, FolderOpen } from 'lucide-react';
import { Button, Card, Spinner, Badge, ProgressBar } from '@/components/ui';
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
import { useFinancialDocuments } from '@/hooks/useDataRoom';
import MetricDocumentsDrawer from '@/components/financial/MetricDocumentsDrawer';
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

function DocsButton({ count, onClick, label }: { count: number; onClick: () => void; label: string }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={`Documentos de ${label}${count ? ` (${count})` : ''}`}
      title={count ? `${count} documento(s) anexado(s)` : 'Anexar documento'}
      className={`relative p-2 rounded-lg border transition-colors ${
        count
          ? 'border-blue-200 bg-blue-50 text-blue-600 hover:bg-blue-100'
          : 'border-gray-200 text-gray-400 hover:text-blue-600 hover:border-blue-300'
      }`}
    >
      <Paperclip size={15} />
      {count > 0 && (
        <span className="absolute -top-1.5 -right-1.5 min-w-[18px] h-[18px] px-1 rounded-full bg-blue-600 text-white text-[10px] leading-[18px] text-center">
          {count}
        </span>
      )}
    </button>
  );
}

function MetricRow({
  def,
  value,
  calculatedValue,
  onChange,
  disabled,
  docCount,
  onOpenDocs,
}: {
  def: FinancialMetricDefinition;
  value: string;
  calculatedValue: number | null | undefined;
  onChange: (v: string) => void;
  disabled: boolean;
  docCount: number;
  onOpenDocs: () => void;
}) {
  const prefix = inputPrefix(def);
  const inputId = `metric-${def.key}`;

  return (
    <div className="grid grid-cols-1 sm:grid-cols-[1fr_220px_40px] gap-2 sm:gap-4 items-center py-3 first:pt-0 last:pb-0">
      <div className="min-w-0">
        <label htmlFor={inputId} className="text-sm font-medium text-gray-800">
          {def.label}
        </label>
        {def.isCalculated && (
          <span className="ml-2 text-[10px] uppercase tracking-wide text-gray-400 bg-gray-100 rounded px-1.5 py-0.5">
            calculado
          </span>
        )}
        {def.description && <p className="text-xs text-gray-400 truncate">{def.description}</p>}
      </div>

      {def.isCalculated ? (
        <div id={inputId} className="px-3 py-2 text-sm text-gray-600 bg-gray-50 border border-gray-200 rounded-lg text-right">
          {formatMetricValue(calculatedValue, def)}
        </div>
      ) : (
        <div className="flex items-center border border-gray-200 rounded-lg overflow-hidden focus-within:ring-2 focus-within:ring-blue-500">
          {prefix && <span className="px-2 py-2 text-xs text-gray-400 bg-gray-50 border-r border-gray-200">{prefix}</span>}
          <input
            id={inputId}
            type="number"
            inputMode="decimal"
            step={def.dataType === 'integer' ? 1 : 'any'}
            value={value}
            placeholder="—"
            onChange={(e) => onChange(e.target.value)}
            disabled={disabled}
            className="flex-1 min-w-0 px-3 py-2 text-sm text-right text-gray-900 bg-transparent focus:outline-none disabled:cursor-not-allowed disabled:text-gray-400"
          />
        </div>
      )}

      {def.isCalculated ? <span /> : <DocsButton count={docCount} onClick={onOpenDocs} label={def.label} />}
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
  const { data: financialDocs = [] } = useFinancialDocuments(period?.id ?? '');

  const locked = period?.status === 'locked' || period?.status === 'approved';

  const [draft, setDraft] = useState<Record<string, string>>({});
  const [docsTarget, setDocsTarget] = useState<{ key: string | null; title: string } | null>(null);

  useEffect(() => {
    if (periodValues) setDraft(toDraft(periodValues.values));
  }, [periodValues]);

  const saved = useMemo(() => toDraft(periodValues?.values), [periodValues]);
  const editable = useMemo(() => (definitions ?? []).filter((d) => !d.isCalculated), [definitions]);
  const dirty = editable.some((d) => (draft[d.key] ?? '') !== (saved[d.key] ?? ''));
  const filled = editable.filter((d) => (draft[d.key] ?? '') !== '').length;

  const docCountByKey = useMemo(() => {
    const map: Record<string, number> = {};
    for (const d of financialDocs) if (d.metricKey) map[d.metricKey] = (map[d.metricKey] ?? 0) + 1;
    return map;
  }, [financialDocs]);
  const generalDocs = financialDocs.filter((d) => !d.metricKey).length;

  useEffect(() => {
    if (!dirty) return;
    const handler = (e: BeforeUnloadEvent) => e.preventDefault();
    window.addEventListener('beforeunload', handler);
    return () => window.removeEventListener('beforeunload', handler);
  }, [dirty]);

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
    for (const def of editable) {
      const raw = draft[def.key];
      if (raw === undefined) continue;
      const n = parseFloat(raw);
      values[def.key] = raw === '' || isNaN(n) ? null : n;
    }
    saveValues.mutate({ id: period.id, values });
  };

  return (
    <div className="max-w-4xl space-y-6 animate-fade-in pb-6">
      {/* Header */}
      <div className="flex flex-wrap items-center gap-4">
        <button
          onClick={() => navigate('/financial')}
          className="p-2 rounded-lg hover:bg-gray-100 text-gray-400 transition-colors"
          aria-label="Voltar"
        >
          <ArrowLeft size={18} />
        </button>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-bold text-gray-900">Indicadores — {label}</h1>
            <Badge className={financialPeriodStatusColors[period.status] ?? ''}>
              {financialPeriodStatusLabels[period.status] ?? period.status}
            </Badge>
          </div>
        </div>
        <div className="flex gap-2">
          <Button icon={<Settings2 size={14} />} size="sm" variant="secondary" onClick={() => navigate('/financial/metrics')}>
            Configurar
          </Button>
          {period.status === 'draft' && (
            <Button
              icon={<Send size={14} />}
              size="sm"
              onClick={() => submitPeriod.mutate(period.id)}
              loading={submitPeriod.isPending}
              disabled={dirty}
              title={dirty ? 'Salve as alterações antes de submeter' : undefined}
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
          {/* Summary */}
          <Card>
            <div className="p-4 flex flex-wrap items-center gap-x-8 gap-y-3">
              <div className="flex-1 min-w-[200px]">
                <div className="flex justify-between text-xs text-gray-500 mb-1.5">
                  <span>Preenchimento</span>
                  <span>{filled} de {editable.length} indicadores</span>
                </div>
                <ProgressBar value={filled} max={Math.max(editable.length, 1)} />
              </div>
              <div className="text-xs text-gray-500 flex items-center gap-1.5">
                <Paperclip size={13} /> {financialDocs.length} documento(s) anexado(s)
              </div>
            </div>
            {locked && (
              <p className="px-4 pb-4 text-xs text-amber-700">
                Período {period.status === 'locked' ? 'bloqueado' : 'aprovado'}: valores e anexos não podem ser alterados.
              </p>
            )}
          </Card>

          {groups.map(([category, defs]) => (
            <Card key={category}>
              <div className="p-5">
                <h2 className="text-sm font-semibold text-gray-800 mb-4">{category}</h2>
                <div className="divide-y divide-gray-100">
                  {defs.map((def) => (
                    <MetricRow
                      key={def.id}
                      def={def}
                      value={draft[def.key] ?? ''}
                      calculatedValue={periodValues.values[def.key]}
                      onChange={(v) => setDraft((d) => ({ ...d, [def.key]: v }))}
                      disabled={locked}
                      docCount={docCountByKey[def.key] ?? 0}
                      onOpenDocs={() => setDocsTarget({ key: def.key, title: def.label })}
                    />
                  ))}
                </div>
              </div>
            </Card>
          ))}

          {groups.length === 0 && (
            <div className="text-center py-10">
              <p className="text-sm text-gray-400">Nenhuma métrica configurada para esta empresa.</p>
              <Button size="sm" variant="secondary" className="mt-3" onClick={() => navigate('/financial/metrics')}>
                Configurar indicadores
              </Button>
            </div>
          )}

          <Card>
            <div className="p-5 flex items-center justify-between gap-4">
              <div>
                <h2 className="text-sm font-semibold text-gray-800">Documentos gerais do período</h2>
                <p className="text-xs text-gray-500">
                  Balancetes, DRE e demais arquivos do mês. Ficam no Data Room em Financeiro /{' '}
                  {String(period.month).padStart(2, '0')}-{period.year}.
                </p>
              </div>
              <Button
                size="sm"
                variant="secondary"
                icon={<FolderOpen size={14} />}
                onClick={() => setDocsTarget({ key: null, title: 'Documentos gerais' })}
              >
                Documentos{generalDocs ? ` (${generalDocs})` : ''}
              </Button>
            </div>
          </Card>
        </>
      )}

      {/* Sticky save bar */}
      {!locked && dirty && (
        <div className="sticky bottom-0 z-40 bg-white/95 backdrop-blur border border-gray-200 rounded-xl shadow-lg">
          <div className="px-5 py-3 flex items-center justify-between gap-4">
            <span className="text-sm text-amber-700">Você tem alterações não salvas</span>
            <div className="flex gap-2">
              <Button size="sm" variant="secondary" onClick={() => setDraft(saved)} disabled={saveValues.isPending}>
                Descartar
              </Button>
              <Button size="sm" onClick={handleSave} loading={saveValues.isPending}>
                Salvar métricas
              </Button>
            </div>
          </div>
        </div>
      )}

      {docsTarget && (
        <MetricDocumentsDrawer
          periodId={period.id}
          year={period.year}
          month={period.month}
          metricKey={docsTarget.key}
          title={docsTarget.title}
          canUpload={!locked}
          onClose={() => setDocsTarget(null)}
        />
      )}
    </div>
  );
}
