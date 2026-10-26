// Share, Transaction and CapTable Types

import { ShareholderType } from './shareholder.types';

// ============================================================================
// ENUMS
// ============================================================================

export enum ShareOrigin {
  Founding = 1,
  Investment = 2,
  OptionExercise = 3,
  Transfer = 4,
  Conversion = 5,
  Bonus = 6,
  Other = 99,
}

export enum ShareStatus {
  Active = 1,
  Transferred = 2,
  Cancelled = 3,
  Converted = 4,
}

export enum TransactionType {
  Issue = 1,
  Transfer = 2,
  Cancel = 3,
  Convert = 4,
  Split = 5,
  Reverse = 6,
}

// ============================================================================
// SHARE INTERFACES
// ============================================================================

export interface Share {
  id: string;
  clientId: string;
  companyId: string;
  companyName: string;
  shareholderId: string;
  shareholderName: string;
  shareClassId: string;
  shareClassName: string;
  shareClassCode: string;
  certificateNumber?: string;
  quantity: number;
  acquisitionPrice: number;
  totalCost: number;
  acquisitionDate: string;
  origin: ShareOrigin;
  originDescription: string;
  originTransactionId?: string;
  status: ShareStatus;
  statusDescription: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ShareSummary {
  id: string;
  shareholderName: string;
  shareClassCode: string;
  quantity: number;
  status: ShareStatus;
}

export interface ShareListResponse {
  items: Share[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  totalShares: number;
  totalValue: number;
}

// ============================================================================
// SHARE REQUEST TYPES
// ============================================================================

export interface CreateShareRequest {
  companyId: string;
  shareholderId: string;
  shareClassId: string;
  certificateNumber?: string;
  quantity: number;
  acquisitionPrice: number;
  acquisitionDate: string;
  notes?: string;
}

export interface UpdateShareRequest {
  certificateNumber?: string;
  notes?: string;
}

// ============================================================================
// TRANSACTION INTERFACES
// ============================================================================

export interface ShareTransaction {
  id: string;
  clientId: string;
  companyId: string;
  companyName: string;
  transactionType: TransactionType;
  transactionTypeDescription: string;
  transactionNumber?: string;
  referenceDate: string;
  shareId?: string;
  shareClassId: string;
  shareClassName: string;
  shareClassCode: string;
  quantity: number;
  pricePerShare: number;
  totalValue: number;
  fromShareholderId?: string;
  fromShareholderName?: string;
  toShareholderId?: string;
  toShareholderName?: string;
  reason?: string;
  documentReference?: string;
  notes?: string;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: string;
  createdAt: string;
}

export interface TransactionListResponse {
  items: ShareTransaction[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  totalQuantity: number;
  totalValue: number;
}

// ============================================================================
// TRANSACTION REQUEST TYPES
// ============================================================================

export interface IssueSharesRequest {
  companyId: string;
  shareholderId: string;
  shareClassId: string;
  quantity: number;
  pricePerShare: number;
  referenceDate: string;
  certificateNumber?: string;
  transactionNumber?: string;
  reason?: string;
  documentReference?: string;
  notes?: string;
}

export interface IssueSharesBatchRequest {
  companyId: string;
  shareClassId: string;
  totalQuantity: number;
  pricePerShare: number;
  referenceDate: string;
  allocations: ShareAllocation[];
  reason?: string;
  documentReference?: string;
  notes?: string;
}

export interface ShareAllocation {
  shareholderId: string;
  quantity: number;
}

export interface TransferSharesRequest {
  companyId: string;
  fromShareholderId: string;
  toShareholderId: string;
  shareClassId: string;
  quantity: number;
  pricePerShare: number;
  referenceDate: string;
  transactionNumber?: string;
  reason?: string;
  documentReference?: string;
  notes?: string;
}

export interface CancelSharesRequest {
  companyId: string;
  shareholderId: string;
  shareClassId: string;
  quantity: number;
  referenceDate: string;
  reason: string;
  transactionNumber?: string;
  documentReference?: string;
  notes?: string;
}

export interface ConvertSharesRequest {
  companyId: string;
  shareholderId: string;
  fromShareClassId: string;
  toShareClassId: string;
  quantity: number;
  referenceDate: string;
  transactionNumber?: string;
  reason?: string;
  documentReference?: string;
  notes?: string;
}

// ============================================================================
// CAP TABLE INTERFACES
// ============================================================================

export interface CapTableEntry {
  shareholderId: string;
  shareholderName: string;
  shareholderType: ShareholderType;
  shareholderTypeDescription: string;
  shareClassId: string;
  shareClassName: string;
  shareClassCode: string;
  totalShares: number;
  totalValue: number;
  ownershipPercentage: number;
  votingPercentage: number;
  fullyDilutedPercentage: number;
}

export interface CapTableSummaryByType {
  type: ShareholderType;
  typeDescription: string;
  shareholderCount: number;
  totalShares: number;
  ownershipPercentage: number;
}

export interface CapTableSummaryByClass {
  shareClassId: string;
  shareClassName: string;
  shareClassCode: string;
  totalShares: number;
  ownershipPercentage: number;
}

export interface CapTable {
  companyId: string;
  companyName: string;
  asOfDate: string;
  totalShares: number;
  totalValue: number;
  totalVotingShares: number;
  entries: CapTableEntry[];
  summaryByType: CapTableSummaryByType[];
  summaryByClass: CapTableSummaryByClass[];
}

// ============================================================================
// FILTER AND QUERY TYPES
// ============================================================================

export interface ShareFilters {
  companyId?: string;
  shareholderId?: string;
  shareClassId?: string;
  status?: ShareStatus;
  origin?: ShareOrigin;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface TransactionFilters {
  companyId?: string;
  shareholderId?: string;
  shareClassId?: string;
  transactionType?: TransactionType;
  fromDate?: string;
  toDate?: string;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

export const getShareOriginLabel = (origin: ShareOrigin): string => {
  const labels: Record<ShareOrigin, string> = {
    [ShareOrigin.Founding]: 'Fundação',
    [ShareOrigin.Investment]: 'Investimento',
    [ShareOrigin.OptionExercise]: 'Exercício de Opção',
    [ShareOrigin.Transfer]: 'Transferência',
    [ShareOrigin.Conversion]: 'Conversão',
    [ShareOrigin.Bonus]: 'Bonificação',
    [ShareOrigin.Other]: 'Outro',
  };
  return labels[origin] || 'Desconhecido';
};

export const getShareStatusLabel = (status: ShareStatus): string => {
  const labels: Record<ShareStatus, string> = {
    [ShareStatus.Active]: 'Ativa',
    [ShareStatus.Transferred]: 'Transferida',
    [ShareStatus.Cancelled]: 'Cancelada',
    [ShareStatus.Converted]: 'Convertida',
  };
  return labels[status] || 'Desconhecido';
};

export const getTransactionTypeLabel = (type: TransactionType): string => {
  const labels: Record<TransactionType, string> = {
    [TransactionType.Issue]: 'Emissão',
    [TransactionType.Transfer]: 'Transferência',
    [TransactionType.Cancel]: 'Cancelamento',
    [TransactionType.Convert]: 'Conversão',
    [TransactionType.Split]: 'Desdobramento',
    [TransactionType.Reverse]: 'Grupamento',
  };
  return labels[type] || 'Desconhecido';
};

export const getShareStatusColor = (status: ShareStatus): string => {
  const colors: Record<ShareStatus, string> = {
    [ShareStatus.Active]: 'green',
    [ShareStatus.Transferred]: 'blue',
    [ShareStatus.Cancelled]: 'red',
    [ShareStatus.Converted]: 'purple',
  };
  return colors[status] || 'gray';
};

export const getTransactionTypeColor = (type: TransactionType): string => {
  const colors: Record<TransactionType, string> = {
    [TransactionType.Issue]: 'green',
    [TransactionType.Transfer]: 'blue',
    [TransactionType.Cancel]: 'red',
    [TransactionType.Convert]: 'purple',
    [TransactionType.Split]: 'orange',
    [TransactionType.Reverse]: 'orange',
  };
  return colors[type] || 'gray';
};
