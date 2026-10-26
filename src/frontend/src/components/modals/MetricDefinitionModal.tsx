import { useEffect, useState } from 'react';
import { X } from 'lucide-react';
import { Button } from '@/components/ui';
import { metricDataTypeLabels, type MetricDataType } from '@/types';

export interface MetricFormState {
  label: string;
  category: string;
  dataType: MetricDataType;
  unit: string;
  description: string;
}

export const emptyMetricForm: MetricFormState = {
  label: '',
  category: '',
  dataType: 'number',
  unit: '',
  description: '',
};

interface MetricDefinitionModalProps {
  isOpen: boolean;
  title: string;
  initial: MetricFormState;
  lockType?: boolean;
  categories: string[];
  submitting: boolean;
  onSubmit: (form: MetricFormState) => void;
  onClose: () => void;
}

const fieldClass =
  'w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500';

export function MetricDefinitionModal({
  isOpen,
  title,
  initial,
  lockType = false,
  categories,
  submitting,
  onSubmit,
  onClose,
}: MetricDefinitionModalProps) {
  const [form, setForm] = useState(initial);

  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  if (!isOpen) return null;

  const set = (patch: Partial<MetricFormState>) => setForm((f) => ({ ...f, ...patch }));
  const valid = form.label.trim().length > 0;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <form
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="bg-white rounded-lg max-w-lg w-full shadow-xl animate-slide-in-top"
        onSubmit={(e) => {
          e.preventDefault();
          if (valid) onSubmit(form);
        }}
      >
        <div className="flex items-center justify-between p-5 border-b border-gray-100">
          <h2 className="text-lg font-semibold text-gray-900">{title}</h2>
          <button type="button" aria-label="Fechar" onClick={onClose} className="p-1 text-gray-400 hover:text-gray-700">
            <X size={18} />
          </button>
        </div>

        <div className="p-5 space-y-4">
          <label className="block">
            <span className="text-xs font-medium text-gray-600">Nome *</span>
            <input
              autoFocus
              maxLength={120}
              aria-label="Nome"
              placeholder="Ex.: Receita recorrente"
              className={`${fieldClass} mt-1`}
              value={form.label}
              onChange={(e) => set({ label: e.target.value })}
            />
          </label>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <label className="block">
              <span className="text-xs font-medium text-gray-600">Categoria</span>
              <input
                aria-label="Categoria"
                list="metric-categories"
                placeholder="Geral"
                className={`${fieldClass} mt-1`}
                value={form.category}
                onChange={(e) => set({ category: e.target.value })}
              />
              <datalist id="metric-categories">
                {categories.map((c) => (
                  <option key={c} value={c} />
                ))}
              </datalist>
            </label>

            <label className="block">
              <span className="text-xs font-medium text-gray-600">Tipo de dado</span>
              <select
                aria-label="Tipo"
                className={`${fieldClass} mt-1 disabled:bg-gray-50`}
                value={form.dataType}
                disabled={lockType}
                onChange={(e) => set({ dataType: e.target.value as MetricDataType })}
              >
                {(Object.keys(metricDataTypeLabels) as MetricDataType[]).map((t) => (
                  <option key={t} value={t}>
                    {metricDataTypeLabels[t]}
                  </option>
                ))}
              </select>
              {lockType && <span className="text-xs text-gray-400">Métricas padrão não permitem alterar o tipo.</span>}
            </label>
          </div>

          <label className="block">
            <span className="text-xs font-medium text-gray-600">Unidade</span>
            <input
              aria-label="Unidade"
              placeholder="Opcional (ex.: meses, x)"
              className={`${fieldClass} mt-1`}
              value={form.unit}
              onChange={(e) => set({ unit: e.target.value })}
            />
          </label>

          <label className="block">
            <span className="text-xs font-medium text-gray-600">Descrição</span>
            <textarea
              aria-label="Descrição"
              rows={3}
              placeholder="Opcional"
              className={`${fieldClass} mt-1 resize-none`}
              value={form.description}
              onChange={(e) => set({ description: e.target.value })}
            />
          </label>
        </div>

        <div className="flex justify-end gap-2 p-5 border-t border-gray-100">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" loading={submitting} disabled={!valid}>
            Salvar
          </Button>
        </div>
      </form>
    </div>
  );
}
