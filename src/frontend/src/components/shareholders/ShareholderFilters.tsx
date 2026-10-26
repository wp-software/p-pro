import { Search, X } from 'lucide-react';
import { Button } from '@/components/ui';
import {
  ShareholderType,
  ShareholderStatus,
  shareholderTypeLabels,
  shareholderStatusLabels,
} from '@/types';

interface ShareholderFiltersProps {
  search: string;
  onSearchChange: (value: string) => void;
  typeFilter: ShareholderType | null;
  onTypeFilterChange: (value: ShareholderType | null) => void;
  statusFilter: ShareholderStatus | null;
  onStatusFilterChange: (value: ShareholderStatus | null) => void;
  onClearFilters: () => void;
}

const typeOptions = [
  { value: null, label: 'Todos os Tipos' },
  { value: ShareholderType.Founder, label: shareholderTypeLabels[ShareholderType.Founder] },
  { value: ShareholderType.Investor, label: shareholderTypeLabels[ShareholderType.Investor] },
  { value: ShareholderType.Employee, label: shareholderTypeLabels[ShareholderType.Employee] },
  { value: ShareholderType.Advisor, label: shareholderTypeLabels[ShareholderType.Advisor] },
  { value: ShareholderType.Partner, label: shareholderTypeLabels[ShareholderType.Partner] },
  { value: ShareholderType.Other, label: shareholderTypeLabels[ShareholderType.Other] },
];

const statusOptions = [
  { value: null, label: 'Todos os Status' },
  { value: ShareholderStatus.Active, label: shareholderStatusLabels[ShareholderStatus.Active] },
  { value: ShareholderStatus.Inactive, label: shareholderStatusLabels[ShareholderStatus.Inactive] },
  { value: ShareholderStatus.Pending, label: shareholderStatusLabels[ShareholderStatus.Pending] },
];

export function ShareholderFilters({
  search,
  onSearchChange,
  typeFilter,
  onTypeFilterChange,
  statusFilter,
  onStatusFilterChange,
  onClearFilters,
}: ShareholderFiltersProps) {
  const hasFilters = search || typeFilter !== null || statusFilter !== null;

  return (
    <div className="flex flex-wrap items-center gap-4">
      {/* Search Input */}
      <div className="relative flex-1 min-w-[240px] max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-primary-400" />
        <input
          type="text"
          placeholder="Buscar por nome ou documento..."
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          className="w-full pl-10 pr-4 py-2.5 border border-primary-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-accent bg-white"
        />
        {search && (
          <button
            onClick={() => onSearchChange('')}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-primary-400 hover:text-primary-600"
          >
            <X className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Type Filter */}
      <select
        value={typeFilter ?? ''}
        onChange={(e) =>
          onTypeFilterChange(e.target.value ? (Number(e.target.value) as ShareholderType) : null)
        }
        className="px-4 py-2.5 border border-primary-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-accent bg-white text-sm"
      >
        {typeOptions.map((option) => (
          <option key={option.label} value={option.value ?? ''}>
            {option.label}
          </option>
        ))}
      </select>

      {/* Status Filter */}
      <select
        value={statusFilter ?? ''}
        onChange={(e) =>
          onStatusFilterChange(e.target.value ? (Number(e.target.value) as ShareholderStatus) : null)
        }
        className="px-4 py-2.5 border border-primary-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-accent bg-white text-sm"
      >
        {statusOptions.map((option) => (
          <option key={option.label} value={option.value ?? ''}>
            {option.label}
          </option>
        ))}
      </select>

      {/* Clear Filters */}
      {hasFilters && (
        <Button variant="ghost" size="sm" onClick={onClearFilters}>
          <X className="w-4 h-4 mr-1" />
          Limpar
        </Button>
      )}
    </div>
  );
}
