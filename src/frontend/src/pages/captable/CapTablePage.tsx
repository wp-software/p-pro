import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Download, RefreshCw, Calendar, PieChart, BarChart3, Calculator, ArrowRightLeft } from 'lucide-react';
import { Button, Card } from '@/components/ui';
import { CapTableStats, CapTableChart, CapTableTable, RoundSimulatorModal } from '@/components/captable';
import { useCapTable } from '@/hooks';
import { useClientStore } from '@/stores/clientStore';

type ChartView = 'type' | 'class';

export default function CapTablePage() {
  const navigate = useNavigate();
  const { selectedCompanyId: rawCompanyId } = useClientStore();
  const selectedCompanyId = rawCompanyId === '__all__' ? null : rawCompanyId;
  const [chartView, setChartView] = useState<ChartView>('type');
  const [asOfDate, setAsOfDate] = useState<string>(new Date().toISOString().split('T')[0]);
  const [showSimulator, setShowSimulator] = useState(false);

  const { data, isLoading, isError, refetch, isFetching } = useCapTable(selectedCompanyId ?? undefined);

  const handleExport = () => {
    // TODO: Implementar exportação CSV/PDF
    console.log('Exportar Cap Table', { asOfDate });
  };

  const handleRefresh = () => {
    refetch();
  };

  if (!selectedCompanyId) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Card className="p-8 text-center">
          <PieChart className="w-12 h-12 text-gray-400 mx-auto mb-4" />
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Nenhuma empresa selecionada</h2>
          <p className="text-gray-500">Selecione uma empresa específica (não "Todas as empresas") para visualizar o Cap Table.</p>
        </Card>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Card className="p-8 text-center">
          <div className="text-red-500 mb-4">
            <PieChart className="w-12 h-12 mx-auto" />
          </div>
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Erro ao carregar dados</h2>
          <p className="text-gray-500 mb-4">Não foi possível carregar o Cap Table.</p>
          <Button onClick={handleRefresh}>Tentar novamente</Button>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="page-header">
        <div>
          <h1 className="page-title">Cap Table</h1>
          <p className="page-subtitle">
            {data?.companyName ?? 'Carregando...'} - Distribuição societária
          </p>
        </div>
        <div className="flex items-center gap-3">
          {/* Data de referência */}
          <div className="flex items-center gap-2 bg-white border border-gray-200 rounded-lg px-3 py-2">
            <Calendar className="w-4 h-4 text-gray-500" />
            <input
              type="date"
              value={asOfDate}
              onChange={(e) => setAsOfDate(e.target.value)}
              className="text-sm border-none focus:ring-0 p-0"
            />
          </div>

          <Button
            variant="primary"
            onClick={() => navigate('/cap-table/transactions')}
            icon={<ArrowRightLeft className="w-4 h-4" />}
          >
            Movimentações
          </Button>

          <Button
            variant="primary"
            onClick={() => setShowSimulator(true)}
            icon={<Calculator className="w-4 h-4" />}
          >
            Simular Rodada
          </Button>

          <Button
            variant="secondary"
            onClick={handleRefresh}
            loading={isFetching}
            icon={<RefreshCw className="w-4 h-4" />}
          >
            Atualizar
          </Button>

          <Button
            variant="secondary"
            onClick={handleExport}
            icon={<Download className="w-4 h-4" />}
          >
            Exportar
          </Button>
        </div>
      </div>

      {/* Stats */}
      <CapTableStats data={data} isLoading={isLoading} />

      {/* Charts Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Toggle de visualização do gráfico */}
        <div>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Distribuição</h2>
            <div className="flex bg-gray-100 rounded-lg p-1">
              <button
                onClick={() => setChartView('type')}
                className={`flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-md transition-colors ${
                  chartView === 'type'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                <PieChart className="w-4 h-4" />
                Por Tipo
              </button>
              <button
                onClick={() => setChartView('class')}
                className={`flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-md transition-colors ${
                  chartView === 'class'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                <BarChart3 className="w-4 h-4" />
                Por Classe
              </button>
            </div>
          </div>
          <CapTableChart data={data} isLoading={isLoading} view={chartView} />
        </div>

        {/* Resumo por classe */}
        <Card className="p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Resumo por Classe</h3>
          {isLoading ? (
            <div className="space-y-3">
              {[...Array(4)].map((_, i) => (
                <div key={i} className="flex items-center justify-between animate-pulse">
                  <div className="h-4 bg-gray-200 rounded w-24"></div>
                  <div className="h-4 bg-gray-200 rounded w-16"></div>
                </div>
              ))}
            </div>
          ) : data?.summaryByClass && data.summaryByClass.length > 0 ? (
            <div className="space-y-3">
              {data.summaryByClass.map((item) => (
                <div
                  key={item.shareClassId}
                  className="flex items-center justify-between py-2 border-b border-gray-100 last:border-0"
                >
                  <div className="flex items-center gap-3">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-800">
                      {item.shareClassCode}
                    </span>
                    <span className="text-sm text-gray-700">{item.shareClassName}</span>
                  </div>
                  <div className="flex items-center gap-4 text-sm">
                    <span className="text-gray-500">
                      {item.totalShares.toLocaleString('pt-BR')} ações
                    </span>
                    <span className="font-medium text-gray-900">
                      {item.ownershipPercentage.toFixed(2)}%
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-gray-500 text-center py-8">Nenhuma classe de ações registrada</p>
          )}
        </Card>
      </div>

      {/* Table */}
      <div>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Detalhamento por Acionista</h2>
        <CapTableTable entries={data?.entries} isLoading={isLoading} />
      </div>

      {/* Round Simulator Modal */}
      <RoundSimulatorModal
        isOpen={showSimulator}
        onClose={() => setShowSimulator(false)}
        companyId={selectedCompanyId ?? undefined}
        onSimulationComplete={() => {
          // Optionally refresh cap table after simulation
          // refetch();
        }}
      />
    </div>
  );
}
