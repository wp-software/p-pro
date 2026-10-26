import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Plus, Pencil, Trash2, Eye, EyeOff, Search } from 'lucide-react';
import { Button, Card, Spinner, Badge, ConfirmDialog } from '@/components/ui';
import {
  MetricDefinitionModal,
  emptyMetricForm,
  type MetricFormState,
} from '@/components/modals/MetricDefinitionModal';
import {
  useMetricDefinitions,
  useCreateMetricDefinition,
  useUpdateMetricDefinition,
  useDeleteMetricDefinition,
} from '@/hooks';
import { useClientStore } from '@/stores/clientStore';
import { metricDataTypeLabels, type FinancialMetricDefinition } from '@/types';
import { groupByCategory } from '@/utils/financialMetrics';

export default function FinancialMetricsPage() {
  const navigate = useNavigate();
  const { selectedCompanyId } = useClientStore();
  const companyId = selectedCompanyId || '';

  const { data: definitions, isLoading } = useMetricDefinitions(companyId, true);
  const createDef = useCreateMetricDefinition();
  const updateDef = useUpdateMetricDefinition();
  const deleteDef = useDeleteMetricDefinition();

  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<FinancialMetricDefinition | null>(null);
  const [toDelete, setToDelete] = useState<FinancialMetricDefinition | null>(null);
  const [search, setSearch] = useState('');

  const all = useMemo(() => definitions ?? [], [definitions]);
  const categories = useMemo(() => Array.from(new Set(all.map((d) => d.category))).sort(), [all]);
  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return q
      ? all.filter((d) => [d.label, d.key, d.category].some((s) => s.toLowerCase().includes(q)))
      : all;
  }, [all, search]);
  const groups = groupByCategory(filtered);

  const initialForm = useMemo<MetricFormState>(
    () =>
      editing
        ? {
            label: editing.label,
            category: editing.category,
            dataType: editing.dataType,
            unit: editing.unit ?? '',
            description: editing.description ?? '',
          }
        : emptyMetricForm,
    [editing],
  );

  if (!selectedCompanyId) {
    return (
      <div className="text-center py-20 text-gray-400">
        <p>Selecione uma empresa para configurar as métricas.</p>
      </div>
    );
  }

  const closeModal = () => {
    setModalOpen(false);
    setEditing(null);
  };

  const openCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const openEdit = (def: FinancialMetricDefinition) => {
    setEditing(def);
    setModalOpen(true);
  };

  const handleSubmit = (f: MetricFormState) => {
    if (editing) {
      updateDef.mutate(
        {
          id: editing.id,
          data: {
            label: f.label,
            category: f.category || undefined,
            dataType: editing.isSystem ? undefined : f.dataType,
            unit: f.unit,
            description: f.description,
            isActive: editing.isActive,
          },
        },
        { onSuccess: closeModal },
      );
      return;
    }
    createDef.mutate(
      {
        companyId,
        label: f.label,
        category: f.category || undefined,
        dataType: f.dataType,
        unit: f.unit || undefined,
        description: f.description || undefined,
      },
      { onSuccess: closeModal },
    );
  };

  const toggleActive = (def: FinancialMetricDefinition) =>
    updateDef.mutate({
      id: def.id,
      data: {
        label: def.label,
        category: def.category,
        unit: def.unit ?? '',
        description: def.description ?? '',
        isActive: !def.isActive,
      },
    });

  const confirmDelete = () => {
    if (!toDelete) return;
    deleteDef.mutate(toDelete.id, { onSettled: () => setToDelete(null) });
  };

  const activeCount = all.filter((d) => d.isActive).length;
  const customCount = all.filter((d) => !d.isSystem).length;

  return (
    <div className="max-w-4xl space-y-6 animate-fade-in">
      <div className="flex items-center gap-4">
        <button
          aria-label="Voltar"
          onClick={() => navigate('/financial')}
          className="p-2 rounded-lg hover:bg-gray-100 text-gray-400"
        >
          <ArrowLeft size={18} />
        </button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold text-gray-900">Métricas Financeiras</h1>
          <p className="text-sm text-gray-500 mt-1">
            Defina as métricas acompanhadas por esta empresa. Elas aparecem nos períodos e podem ser usadas no dashboard.
          </p>
        </div>
        <Button icon={<Plus size={16} />} onClick={openCreate}>
          Nova métrica
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-3">
        {[
          { label: 'Total', value: all.length },
          { label: 'Ativas', value: activeCount },
          { label: 'Personalizadas', value: customCount },
        ].map((s) => (
          <Card key={s.label} className="p-4">
            <p className="text-xs text-gray-500">{s.label}</p>
            <p className="text-2xl font-semibold text-gray-900">{s.value}</p>
          </Card>
        ))}
      </div>

      <div className="relative">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
        <input
          aria-label="Buscar métricas"
          placeholder="Buscar por nome, chave ou categoria"
          className="w-full text-sm border border-gray-200 rounded-lg pl-9 pr-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20">
          <Spinner className="w-8 h-8" />
        </div>
      ) : groups.length === 0 ? (
        <p className="text-center text-sm text-gray-400 py-12">Nenhuma métrica encontrada.</p>
      ) : (
        groups.map(([category, defs]) => (
          <Card key={category}>
            <div className="p-5 space-y-3">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-semibold text-gray-800">{category}</h2>
                <span className="text-xs text-gray-400">{defs.length}</span>
              </div>
              <ul className="divide-y divide-gray-100">
                {defs.map((def) => {
                  const canDelete = !def.isSystem && !def.inUse;
                  return (
                    <li key={def.id} className={`py-3 flex items-center gap-3 ${def.isActive ? '' : 'opacity-50'}`}>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-900 truncate">{def.label}</p>
                        <p className="text-xs text-gray-400 truncate">
                          {def.key} · {metricDataTypeLabels[def.dataType]}
                          {def.unit ? ` · ${def.unit}` : ''}
                        </p>
                        {def.description && <p className="text-xs text-gray-500 mt-0.5 truncate">{def.description}</p>}
                      </div>
                      {def.isSystem && <Badge>Padrão</Badge>}
                      {def.isCalculated && <Badge>Calculada</Badge>}
                      {!def.isSystem && def.inUse && <Badge>Em uso</Badge>}
                      <button
                        aria-label={def.isActive ? 'Ocultar' : 'Reativar'}
                        title={def.isActive ? 'Ocultar' : 'Reativar'}
                        className="p-2 text-gray-400 hover:text-gray-700"
                        onClick={() => toggleActive(def)}
                      >
                        {def.isActive ? <Eye size={16} /> : <EyeOff size={16} />}
                      </button>
                      <button
                        aria-label="Editar"
                        title="Editar"
                        className="p-2 text-gray-400 hover:text-gray-700"
                        onClick={() => openEdit(def)}
                      >
                        <Pencil size={16} />
                      </button>
                      {canDelete && (
                        <button
                          aria-label="Excluir"
                          title="Excluir (nunca utilizada)"
                          className="p-2 text-red-400 hover:text-red-600"
                          onClick={() => setToDelete(def)}
                        >
                          <Trash2 size={16} />
                        </button>
                      )}
                    </li>
                  );
                })}
              </ul>
            </div>
          </Card>
        ))
      )}

      <MetricDefinitionModal
        isOpen={modalOpen}
        title={editing ? 'Editar métrica' : 'Nova métrica'}
        initial={initialForm}
        lockType={editing?.isSystem}
        categories={categories}
        submitting={createDef.isPending || updateDef.isPending}
        onSubmit={handleSubmit}
        onClose={closeModal}
      />

      <ConfirmDialog
        isOpen={!!toDelete}
        title="Excluir métrica"
        message={`Excluir a métrica "${toDelete?.label ?? ''}"? Ela nunca foi utilizada.`}
        confirmText="Excluir"
        confirmVariant="danger"
        isLoading={deleteDef.isPending}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </div>
  );
}
