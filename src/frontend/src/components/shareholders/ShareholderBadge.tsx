import type { ComponentType } from 'react';
import { cn } from '@/utils/cn';
import {
  Briefcase,
  Building2,
  CheckCircle2,
  CircleDashed,
  Clock3,
  TrendingUp,
  User,
  UserCheck,
  UserMinus,
  Users,
} from 'lucide-react';
import { ShareholderType, ShareholderStatus, shareholderTypeLabels, shareholderStatusLabels } from '@/types';

interface ShareholderTypeBadgeProps {
  type: ShareholderType;
  className?: string;
}

interface ShareholderStatusBadgeProps {
  status: ShareholderStatus;
  className?: string;
}

const typeColors: Record<ShareholderType, string> = {
  [ShareholderType.Founder]: 'bg-purple-100 text-purple-700 border-purple-200',
  [ShareholderType.Investor]: 'bg-blue-100 text-blue-700 border-blue-200',
  [ShareholderType.Employee]: 'bg-green-100 text-green-700 border-green-200',
  [ShareholderType.Advisor]: 'bg-amber-100 text-amber-700 border-amber-200',
  [ShareholderType.ESOP]: 'bg-cyan-100 text-cyan-700 border-cyan-200',
  [ShareholderType.Partner]: 'bg-indigo-100 text-indigo-700 border-indigo-200',
  [ShareholderType.Other]: 'bg-gray-100 text-gray-700 border-gray-200',
};

const typeIcons: Record<ShareholderType, ComponentType<{ className?: string }>> = {
  [ShareholderType.Founder]: Building2,
  [ShareholderType.Investor]: TrendingUp,
  [ShareholderType.Employee]: User,
  [ShareholderType.Advisor]: Briefcase,
  [ShareholderType.ESOP]: Users,
  [ShareholderType.Partner]: Users,
  [ShareholderType.Other]: CircleDashed,
};

const statusColors: Record<ShareholderStatus, string> = {
  [ShareholderStatus.Active]: 'bg-success-100 text-success-700 border-success-200',
  [ShareholderStatus.Inactive]: 'bg-gray-100 text-gray-600 border-gray-200',
  [ShareholderStatus.Pending]: 'bg-warning-100 text-warning-700 border-warning-200',
  [ShareholderStatus.Exited]: 'bg-red-100 text-red-600 border-red-200',
};

const statusIcons: Record<ShareholderStatus, ComponentType<{ className?: string }>> = {
  [ShareholderStatus.Active]: CheckCircle2,
  [ShareholderStatus.Inactive]: UserMinus,
  [ShareholderStatus.Pending]: Clock3,
  [ShareholderStatus.Exited]: UserCheck,
};

function resolveShareholderType(type: ShareholderType): ShareholderType | undefined {
  if (type in typeIcons) {
    return type;
  }

  const raw = String(type);
  const numeric = Number(raw);

  if (!Number.isNaN(numeric) && numeric in typeIcons) {
    return numeric as ShareholderType;
  }

  const byName = (ShareholderType as unknown as Record<string, number>)[raw];
  if (typeof byName === 'number' && byName in typeIcons) {
    return byName as ShareholderType;
  }

  return undefined;
}

function resolveShareholderStatus(status: ShareholderStatus): ShareholderStatus | undefined {
  if (status in statusIcons) {
    return status;
  }

  const raw = String(status);
  const numeric = Number(raw);

  if (!Number.isNaN(numeric) && numeric in statusIcons) {
    return numeric as ShareholderStatus;
  }

  const byName = (ShareholderStatus as unknown as Record<string, number>)[raw];
  if (typeof byName === 'number' && byName in statusIcons) {
    return byName as ShareholderStatus;
  }

  return undefined;
}

export function ShareholderTypeBadge({ type, className }: ShareholderTypeBadgeProps) {
  const resolvedType = resolveShareholderType(type);
  const TypeIcon = resolvedType ? typeIcons[resolvedType] : undefined;
  const typeLabel = resolvedType
    ? shareholderTypeLabels[resolvedType]
    : 'Tipo indefinido';

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium border',
        resolvedType
          ? typeColors[resolvedType]
          : 'bg-gray-100 text-gray-700 border-gray-200',
        className
      )}
    >
      {TypeIcon && <TypeIcon className="w-3.5 h-3.5" />}
      {typeLabel}
    </span>
  );
}

export function ShareholderStatusBadge({ status, className }: ShareholderStatusBadgeProps) {
  const resolvedStatus = resolveShareholderStatus(status);
  const StatusIcon = resolvedStatus ? statusIcons[resolvedStatus] : undefined;
  const statusLabel = resolvedStatus
    ? shareholderStatusLabels[resolvedStatus]
    : 'Status indefinido';

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium border',
        resolvedStatus
          ? statusColors[resolvedStatus]
          : 'bg-gray-100 text-gray-700 border-gray-200',
        className
      )}
    >
      {StatusIcon && <StatusIcon className="w-3.5 h-3.5" />}
      {statusLabel}
    </span>
  );
}
