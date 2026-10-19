import type { FinancialMetricDefinition } from '@/types';
import { formatCurrency } from './format';

type MetricFormat = Pick<FinancialMetricDefinition, 'dataType' | 'unit'>;

export function formatMetricValue(value: number | null | undefined, def: MetricFormat): string {
  if (value == null) return '—';
  const nf = (max: number) => new Intl.NumberFormat('pt-BR', { maximumFractionDigits: max });
  switch (def.dataType) {
    case 'currency':
      return formatCurrency(value);
    case 'percentage':
      return `${nf(2).format(value)}%`;
    case 'integer':
      return `${nf(0).format(value)}${def.unit ? ` ${def.unit}` : ''}`;
    default:
      return `${nf(2).format(value)}${def.unit ? ` ${def.unit}` : ''}`;
  }
}

export function groupByCategory<T extends { category: string }>(items: T[]): [string, T[]][] {
  const groups = new Map<string, T[]>();
  for (const item of items) {
    const list = groups.get(item.category) ?? [];
    list.push(item);
    groups.set(item.category, list);
  }
  return [...groups.entries()];
}
