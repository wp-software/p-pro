import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Plus, Pencil, Trash2, Eye, EyeOff, Check, X } from 'lucide-react';
import { Button, Card, Spinner, Badge } from '@/components/ui';
import {
  useMetricDefinitions,
  useCreateMetricDefinition,
  useUpdateMetricDefinition,
  useDeleteMetricDefinition,
} from '@/hooks';
import { useClientStore } from '@/stores/clientStore';
import {
  metricDataTypeLabels,
  type FinancialMetricDefinition,
  type MetricDataType,
} from '@/types';
import { groupByCategory } from '@/utils/financialMetrics';

interface FormState {
  label: string;
  category: string;
  dataType: MetricDataType;
  unit: string;
  description: string;
}

const emptyForm: FormState = { label: '', category: '', dataType: 'number', unit: '', description: '' };

function MetricForm({
  initial,
  lockType,
  submitting,
  onSubmit,
  onCancel,
}: {
  initial: FormState;
  lockType: boolean;
  submitting: boolean;
  onSubmit: (f: FormState) => void;
  onCancel: () => void;
}) {
  const [form, setForm] = useState(initial);
  const set = (patch: Partial<FormState>) => setForm((f) => ({ ...f, ...patch }));
  const field = 'text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500';

  return (
    <div className="grid grid-cols-1 sm:grid-cols-6 gap-3 items-end">
      <input aria-label="Nome" placeholder="Nome da métrica" className={`${field} sm:col-span-2`}
        value={form.label} onChange={(e) => set({ label: e.target.value })} />
      <input aria-label="Categoria" placeholder="Categoria" className={field}
        value={form.category} onChange={(e) => set({ category: e.target.value })} />
      <select aria-label="Tipo" className={field} value={form.dataType} disabled={lockType}
        onChange={(e) => set({ dataType: e.target.value as MetricDataType })}>
        {(Object.keys(metricDataTypeLabels) as MetricDataType[]).map((t) => (
          <option key={t} value={t}>{metricDataTypeLabels[t]}</option>
        ))}
      </select>
      <input aria-label="Unidade" placeholder="Unidade (opcional)" className={field}
        value={form.unit} onChange={(e) => set({ unit: e.target.value })} />
      <div className="flex gap-2 justify-end">
        <Button size="sm" icon={<Check size={14} />} loading={submitting}
          disabled={!form.label.trim()} onClick={() => onSubmit(form)}>
          Salvar
        </Button>
        <Button size="sm" variant="secondary" icon={<X size={14} />} onClick={onCancel}>
          Cancelar
        </Button>
      </div>
      <input aria-label="Descrição" placeholder="Descrição (opcional)" className={`${field} sm:col-span-6`}
        value={form.description} onChange={(e) => set({ description: e.target.value })} />
    </div>
  );
}

export default function FinancialMetricsPage() {
  const navigate = useNavigate();
  const { selectedCompanyId } = useClientStore();
  const companyId = selectedCompanyId || '';

  const { data: definitions, isLoading } = useMetricDefinitions(companyId, true);
  const createDef = useCreateMetricDefinition();
  const updateDef = useUpdateMetricDefinition();
  const deleteDef = useDeleteMetricDefinition();

  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  if (!selectedCompanyId) {
    return (
      <div className="text-center py-20 text-gray-400">
        <p>Selecione uma empresa para configurar as métricas.</p>
      </div>
    );
  }

  const handleCreate = (f: FormState) =>
    createDef.mutate(
      {
        companyId,
        label: f.label,
        category: f.category || undefined,
        dataType: f.dataType,
        unit: f.unit || undefined,
        description: f.description || undefined,
      },
      { onSuccess: () => setCreating(false) },
    );

  const handleUpdate = (def: FinancialMetricDefinition, f: FormState) =>
    updateDef.mutate(
      {
        id: def.id,
        data: {
          label: f.label,
          category: f.category || undefined,
          dataType: def.isSystem ? undefined : f.dataType,
          unit: f.unit,
          description: f.description,
          isActive: def.isActive,
        },
      },
      { onSuccess: () => setEditingId(null) },
    );

  const handleDelete = (def: FinancialMetricDefinition) => {
    if (window.confirm(`Excluir a métrica "${def.label}" e todos os valores já lançados?`)) {
      deleteDef.mutate(def.id);
    }
  };

  const groups = groupByCategory(definitions ?? []);

  return (
    <div className="max-w-4xl space-y-6 animate-fade-in">
      <div className="flex items-center gap-4">
        <button onClick={() => navigate('/financial')} className="p-2 rounded-lg hover:bg-gray-100 text-gray-400">
          <ArrowLeft size={18} />
        </button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold text-gray-900">Métricas Financeiras</h1>
          <p className="text-sm text-gray-500 mt-1">
            Defina as métricas acompanhadas por esta empresa. Elas aparecem nos períodos e podem ser usadas no dashboard.
          </p>
        </div>
        <Button icon={<Plus size={16} />} onClick={() => setCreating(true)}>
          Nova métrica
        </Button>
      </div>

      {creating && (
        <Card className="p-4">
          <MetricForm initial={emptyForm} lockType={false} submitting={createDef.isPending}
            onSubmit={handleCreate} onCancel={() => setCreating(false)} />
        </Card>
      )}

      {isLoading ? (
        <div className="flex justify-center py-20"><Spinner className="w-8 h-8" /></div>
      ) : (
        groups.map(([category, defs]) => (
          <Card key={category}>
            <div className="p-5 space-y-3">
              <h2 className="text-sm font-semibold text-gray-800">{category}</h2>
              <ul className="divide-y divide-gray-100">
                {defs.map((def) => (
                  <li key={def.id} className="py-3">
                    {editingId === def.id ? (
                      <MetricForm
                        initial={{
                          label: def.label,
                          category: def.category,
                          dataType: def.dataType,
                          unit: def.unit ?? '',
                          description: def.description ?? '',
                        }}
                        lockType={def.isSystem}
                        submitting={updateDef.isPending}
                        onSubmit={(f) => handleUpdate(def, f)}
                        onCancel={() => setEditingId(null)}
                      />
                    ) : (
                      <div className={`flex items-center gap-3 ${def.isActive ? '' : 'opacity-50'}`}>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-900">{def.label}</p>
                          <p className="text-xs text-gray-400">
                            {def.key} · {metricDataTypeLabels[def.dataType]}
                            {def.unit ? ` · ${def.unit}` : ''}
                          </p>
                        </div>
                        {def.isSystem && <Badge>Padrão</Badge>}
                        {def.isCalculated && <Badge>Calculada</Badge>}
                        <button
                          aria-label={def.isActive ? 'Ocultar' : 'Reativar'}
                          className="p-2 text-gray-400 hover:text-gray-700"
                          onClick={() =>
                            updateDef.mutate({
                              id: def.id,
                              data: {
                                label: def.label,
                                category: def.category,
                                unit: def.unit ?? '',
                                description: def.description ?? '',
                                isActive: !def.isActive,
                              },
                            })
                          }
                        >
                          {def.isActive ? <Eye size={16} /> : <EyeOff size={16} />}
                        </button>
                        <button aria-label="Editar" className="p-2 text-gray-400 hover:text-gray-700"
                          onClick={() => setEditingId(def.id)}>
                          <Pencil size={16} />
                        </button>
                        {!def.isSystem && (
                          <button aria-label="Excluir" className="p-2 text-red-400 hover:text-red-600"
                            onClick={() => handleDelete(def)}>
                            <Trash2 size={16} />
                          </button>
                        )}
                      </div>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          </Card>
        ))
      )}
    </div>
  );
}
