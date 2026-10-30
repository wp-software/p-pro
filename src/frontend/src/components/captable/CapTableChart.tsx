import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Legend } from 'recharts';
import { Card } from '@/components/ui';
import { CapTable, getShareholderTypeLabel } from '@/types';
import { formatCurrency, formatPercentage } from '@/utils/format';

// Paleta de cores para o gráfico
const COLORS = [
  '#6366f1', // indigo
  '#22c55e', // green
  '#f59e0b', // amber
  '#ef4444', // red
  '#8b5cf6', // violet
  '#06b6d4', // cyan
  '#ec4899', // pink
  '#14b8a6', // teal
  '#f97316', // orange
  '#84cc16', // lime
];

interface CapTableChartProps {
  data?: CapTable | null;
  isLoading?: boolean;
  view?: 'type' | 'class';
}

interface ChartDataItem {
  name: string;
  value: number;
  shares: number;
  percentage: number;
}

export function CapTableChart({ 
  data, 
  isLoading,
  view = 'type' 
}: CapTableChartProps) {
  if (isLoading) {
    return (
      <Card className="p-6">
        <div className="h-[300px] flex items-center justify-center">
          <div className="animate-pulse flex flex-col items-center gap-4">
            <div className="w-48 h-48 rounded-full bg-gray-200"></div>
            <div className="flex gap-4">
              {[...Array(3)].map((_, i) => (
                <div key={i} className="h-4 w-20 bg-gray-200 rounded"></div>
              ))}
            </div>
          </div>
        </div>
      </Card>
    );
  }

  // Preparar dados para o gráfico
  const chartData: ChartDataItem[] = [];

  if (view === 'type' && data?.summaryByType) {
    data.summaryByType.forEach(item => {
      chartData.push({
        name: getTypeName(item.typeDescription),
        value: item.totalShares,
        shares: item.totalShares,
        percentage: item.ownershipPercentage,
      });
    });
  } else if (view === 'class' && data?.summaryByClass) {
    data.summaryByClass.forEach(item => {
      chartData.push({
        name: item.shareClassName,
        value: item.totalShares,
        shares: item.totalShares,
        percentage: item.ownershipPercentage,
      });
    });
  } else if (data?.entries) {
    // Fallback: agrupar por shareholder
    const grouped = new Map<string, ChartDataItem>();
    data.entries.forEach(entry => {
      const key = entry.shareholderName;
      const existing = grouped.get(key);
      if (existing) {
        existing.shares += entry.totalShares;
        existing.value += entry.totalValue;
      } else {
        grouped.set(key, {
          name: entry.shareholderName,
          value: entry.totalValue,
          shares: entry.totalShares,
          percentage: entry.ownershipPercentage,
        });
      }
    });
    grouped.forEach(item => chartData.push(item));
  }

  if (chartData.length === 0) {
    return (
      <Card className="p-6">
        <div className="h-[300px] flex items-center justify-center text-gray-500">
          Nenhum dado disponível para exibir
        </div>
      </Card>
    );
  }

  return (
    <Card className="p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Distribuição {view === 'type' ? 'por Tipo de Acionista' : 'por Classe de Ação'}
      </h3>
      <div className="h-[300px]">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={chartData}
              cx="50%"
              cy="50%"
              innerRadius={60}
              outerRadius={100}
              paddingAngle={2}
              dataKey="shares"
              nameKey="name"
              label={({ name, percentage }) => `${name}: ${percentage.toFixed(1)}%`}
              labelLine={false}
            >
              {chartData.map((_, index) => (
                <Cell 
                  key={`cell-${index}`} 
                  fill={COLORS[index % COLORS.length]}
                  className="transition-opacity hover:opacity-80"
                />
              ))}
            </Pie>
            <Tooltip
              content={({ active, payload }) => {
                if (!active || !payload?.[0]) return null;
                const item = payload[0].payload as ChartDataItem;
                return (
                  <div className="bg-white border border-gray-200 rounded-lg shadow-lg p-3 space-y-1">
                    <div className="font-semibold text-gray-900">{item.name}</div>
                    <div className="text-sm text-gray-700">{item.shares.toLocaleString('pt-BR')} ações</div>
                    <div className="text-sm text-gray-600">{formatCurrency(item.value)}</div>
                    <div className="text-sm text-gray-600">{formatPercentage(item.percentage)}</div>
                  </div>
                );
              }}
            />
            <Legend
              verticalAlign="bottom"
              height={36}
              formatter={(value: string) => (
                <span className="text-sm text-gray-700">{value}</span>
              )}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </Card>
  );
}

function getTypeName(type: string): string {
  return getShareholderTypeLabel(type);
}
