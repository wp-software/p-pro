import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, ChevronLeft, ChevronRight, Lock, Settings2 } from 'lucide-react';
import { Button, Card, Spinner, Badge } from '@/components/ui';
import { useFinancialPeriods, useCreateFinancialPeriod } from '@/hooks';
import { useClientStore } from '@/stores/clientStore';
import {
  financialPeriodStatusLabels,
  financialPeriodStatusColors,
  monthNames,
  type FinancialPeriod,
  type CreateFinancialPeriodRequest,
} from '@/types';

export default function FinancialPage() {
  const navigate = useNavigate();
  const { selectedCompanyId } = useClientStore();
  const [year, setYear] = useState(new Date().getFullYear());
  const [showNewForm, setShowNewForm] = useState(false);
  const [newMonth, setNewMonth] = useState(new Date().getMonth() + 1);

  const filters = useMemo(
    () => ({ companyId: selectedCompanyId || undefined, year, pageSize: 12 }),
    [selectedCompanyId, year]
  );

  const { data, isLoading } = useFinancialPeriods(filters);
  const createPeriod = useCreateFinancialPeriod();

  // Build 12-month grid
  const periodByMonth = useMemo(() => {
    const map: Record<number, FinancialPeriod> = {};
    data?.items.forEach((p) => { map[p.month] = p; });
    return map;
  }, [data]);

  function handleCreate() {
    if (!selectedCompanyId) return;
    const req: CreateFinancialPeriodRequest = {
      companyId: selectedCompanyId,
      year,
      month: newMonth,
    };
    createPeriod.mutate(req, {
      onSuccess: (p) => {
        setShowNewForm(false);
        navigate(`/financial/${p.year}/${p.month}`);
      },
    });
  }

  if (!selectedCompanyId) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400">
        <p>Selecione uma empresa para ver os períodos financeiros.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Financeiro</h1>
          <p className="text-sm text-gray-500 mt-1">Períodos mensais de dados financeiros</p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" icon={<Settings2 size={16} />} onClick={() => navigate('/financial/metrics')}>
            Métricas
          </Button>
          <Button icon={<Plus size={16} />} onClick={() => setShowNewForm(true)}>
            Novo Período
          </Button>
        </div>
      </div>

      {/* Year selector */}
      <div className="flex items-center gap-3">
        <button
          onClick={() => setYear((y) => y - 1)}
          className="p-2 rounded-lg hover:bg-gray-100 text-gray-500 transition-colors"
        >
          <ChevronLeft size={16} />
        </button>
        <span className="text-lg font-semibold text-gray-800 min-w-[60px] text-center">{year}</span>
        <button
          onClick={() => setYear((y) => y + 1)}
          className="p-2 rounded-lg hover:bg-gray-100 text-gray-500 transition-colors"
        >
          <ChevronRight size={16} />
        </button>
      </div>

      {/* New period form */}
      {showNewForm && (
        <Card>
          <div className="p-4 flex items-end gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Mês</label>
              <select
                value={newMonth}
                onChange={(e) => setNewMonth(Number(e.target.value))}
                className="text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {monthNames.map((m, i) => (
                  <option key={i + 1} value={i + 1}>
                    {m}/{year}
                  </option>
                ))}
              </select>
            </div>
            <Button
              size="sm"
              onClick={handleCreate}
              loading={createPeriod.isPending}
            >
              Criar
            </Button>
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setShowNewForm(false)}
            >
              Cancelar
            </Button>
          </div>
        </Card>
      )}

      {/* 12-month calendar grid */}
      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner className="w-8 h-8" />
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
          {Array.from({ length: 12 }, (_, i) => i + 1).map((month) => {
            const period = periodByMonth[month];
            const label = `${monthNames[month - 1]}/${year}`;
            return (
              <Card
                key={month}
                className={`cursor-pointer transition-all hover:shadow-md ${
                  period ? 'border-gray-200' : 'border-dashed border-gray-200 opacity-60'
                }`}
                onClick={() =>
                  period
                    ? navigate(`/financial/${year}/${month}`)
                    : setShowNewForm(true)
                }
              >
                <div className="p-4 space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-semibold text-gray-700">{label}</span>
                    {period?.status === 'locked' && (
                      <Lock size={12} className="text-blue-500" />
                    )}
                  </div>
                  {period ? (
                    <>
                      <Badge className={financialPeriodStatusColors[period.status] ?? ''}>
                        {financialPeriodStatusLabels[period.status] ?? period.status}
                      </Badge>
                      {period.metrics?.mrr && (
                        <p className="text-xs text-gray-500">
                          MRR: R$ {(period.metrics.mrr / 1000).toFixed(0)}k
                        </p>
                      )}
                    </>
                  ) : (
                    <p className="text-xs text-gray-400">Não iniciado</p>
                  )}
                </div>
              </Card>
            );
          })}
        </div>
      )}

      {/* Summary */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {(['draft', 'submitted', 'approved', 'locked'] as const).map((s) => (
          <Card key={s}>
            <div className="p-4">
              <p className="text-xs text-gray-500 mb-1">
                {financialPeriodStatusLabels[s]}
              </p>
              <p className="text-2xl font-bold text-gray-900">
                {data?.items.filter((p) => p.status === s).length ?? 0}
              </p>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
