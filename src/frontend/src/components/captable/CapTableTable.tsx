import { useState, useMemo } from 'react';
import { ChevronUp, ChevronDown, Search, X } from 'lucide-react';
import { Card, Input, Badge } from '@/components/ui';
import { getShareholderTypeLabel, CapTableEntry } from '@/types';
import { formatCurrency, formatPercentage, formatNumber } from '@/utils/format';

interface CapTableTableProps {
  entries?: CapTableEntry[];
  isLoading?: boolean;
}

type SortField = 'shareholderName' | 'shareClassName' | 'totalShares' | 'totalValue' | 'ownershipPercentage';
type SortDirection = 'asc' | 'desc';

export function CapTableTable({ entries = [], isLoading }: CapTableTableProps) {
  const [search, setSearch] = useState('');
  const [sortField, setSortField] = useState<SortField>('ownershipPercentage');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  // Filtrar e ordenar dados
  const filteredAndSorted = useMemo(() => {
    let result = [...entries];

    // Filtrar por busca
    if (search.trim()) {
      const searchLower = search.toLowerCase();
      result = result.filter(
        entry =>
          entry.shareholderName.toLowerCase().includes(searchLower) ||
          entry.shareClassName.toLowerCase().includes(searchLower) ||
          entry.shareClassCode.toLowerCase().includes(searchLower)
      );
    }

    // Ordenar
    result.sort((a, b) => {
      let comparison = 0;
      switch (sortField) {
        case 'shareholderName':
          comparison = a.shareholderName.localeCompare(b.shareholderName);
          break;
        case 'shareClassName':
          comparison = a.shareClassName.localeCompare(b.shareClassName);
          break;
        case 'totalShares':
          comparison = a.totalShares - b.totalShares;
          break;
        case 'totalValue':
          comparison = a.totalValue - b.totalValue;
          break;
        case 'ownershipPercentage':
          comparison = a.ownershipPercentage - b.ownershipPercentage;
          break;
      }
      return sortDirection === 'asc' ? comparison : -comparison;
    });

    return result;
  }, [entries, search, sortField, sortDirection]);

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(prev => (prev === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortField(field);
      setSortDirection('desc');
    }
  };

  const SortIcon = ({ field }: { field: SortField }) => {
    if (sortField !== field) {
      return <ChevronUp className="w-4 h-4 text-gray-300" />;
    }
    return sortDirection === 'asc' ? (
      <ChevronUp className="w-4 h-4 text-primary" />
    ) : (
      <ChevronDown className="w-4 h-4 text-primary" />
    );
  };

  const getTypeBadgeVariant = (type: string): 'founder' | 'investor' | 'vesting' => {
    switch (type) {
      case 'Founder':
        return 'founder';
      case 'Investor':
        return 'investor';
      case 'ESOP':
        return 'vesting';
      default:
        return 'investor';
    }
  };

  if (isLoading) {
    return (
      <Card className="overflow-hidden">
        <div className="p-4 border-b border-gray-200">
          <div className="h-10 bg-gray-200 rounded animate-pulse w-64"></div>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Acionista', 'Tipo', 'Classe', 'Ações', 'Valor', '%'].map((_, i) => (
                  <th key={i} className="px-6 py-3">
                    <div className="h-4 bg-gray-200 rounded animate-pulse"></div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {[...Array(5)].map((_, i) => (
                <tr key={i}>
                  {[...Array(6)].map((_, j) => (
                    <td key={j} className="px-6 py-4">
                      <div className="h-4 bg-gray-200 rounded animate-pulse"></div>
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    );
  }

  return (
    <Card className="overflow-hidden">
      {/* Barra de busca */}
      <div className="p-4 border-b border-gray-200">
        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <Input
            type="text"
            placeholder="Buscar por acionista ou classe..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="pl-10 pr-10"
          />
          {search && (
            <button
              onClick={() => setSearch('')}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>

      {/* Tabela */}
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('shareholderName')}
              >
                <div className="flex items-center gap-1">
                  Acionista
                  <SortIcon field="shareholderName" />
                </div>
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tipo
              </th>
              <th
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('shareClassName')}
              >
                <div className="flex items-center gap-1">
                  Classe
                  <SortIcon field="shareClassName" />
                </div>
              </th>
              <th
                className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('totalShares')}
              >
                <div className="flex items-center justify-end gap-1">
                  Ações
                  <SortIcon field="totalShares" />
                </div>
              </th>
              <th
                className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('totalValue')}
              >
                <div className="flex items-center justify-end gap-1">
                  Valor
                  <SortIcon field="totalValue" />
                </div>
              </th>
              <th
                className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('ownershipPercentage')}
              >
                <div className="flex items-center justify-end gap-1">
                  Participação
                  <SortIcon field="ownershipPercentage" />
                </div>
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {filteredAndSorted.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-12 text-center text-gray-500">
                  {search ? 'Nenhum resultado encontrado' : 'Nenhum dado disponível'}
                </td>
              </tr>
            ) : (
              filteredAndSorted.map((entry, index) => (
                <tr 
                  key={`${entry.shareholderId}-${entry.shareClassId}-${index}`}
                  className="hover:bg-gray-50 transition-colors"
                >
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="flex-shrink-0 h-8 w-8 rounded-full bg-primary-100 flex items-center justify-center">
                        <span className="text-primary-700 font-medium text-sm">
                          {entry.shareholderName.charAt(0).toUpperCase()}
                        </span>
                      </div>
                      <div className="ml-3">
                        <p className="text-sm font-medium text-gray-900">{entry.shareholderName}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <Badge variant={getTypeBadgeVariant(entry.shareholderTypeDescription)}>
                      {getShareholderTypeLabel(entry.shareholderTypeDescription)}
                    </Badge>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                      {entry.shareClassCode}
                    </span>
                    <span className="ml-2 text-sm text-gray-500">{entry.shareClassName}</span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-900 font-medium">
                    {formatNumber(entry.totalShares)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-900">
                    {formatCurrency(entry.totalValue)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right">
                    <div className="flex items-center justify-end gap-2">
                      <div className="w-16 bg-gray-200 rounded-full h-2">
                        <div
                          className="bg-primary h-2 rounded-full"
                          style={{ width: `${Math.min(entry.ownershipPercentage, 100)}%` }}
                        />
                      </div>
                      <span className="text-sm font-medium text-gray-900 w-14 text-right">
                        {formatPercentage(entry.ownershipPercentage)}
                      </span>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Rodapé com contagem */}
      {filteredAndSorted.length > 0 && (
        <div className="px-6 py-3 bg-gray-50 border-t border-gray-200 text-sm text-gray-500">
          Exibindo {filteredAndSorted.length} de {entries.length} registros
        </div>
      )}
    </Card>
  );
}
