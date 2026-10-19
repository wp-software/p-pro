// Shareholder Types

// Re-export DocumentType from client.types
export { DocumentType } from './client.types';

export enum ShareholderType {
  Founder = 1,
  Investor = 2,
  Employee = 3,
  Advisor = 4,
  ESOP = 5,
  Other = 99,
}

export enum ShareholderStatus {
  Active = 1,
  Inactive = 2,
  Pending = 3,
  Exited = 4,
}

export enum Gender {
  Male = 1,
  Female = 2,
  Other = 3,
  NotInformed = 4,
}

export enum MaritalStatus {
  Single = 1,
  Married = 2,
  StableUnion = 3,
  Divorced = 4,
  Widowed = 5,
}

// Import DocumentType for local use
import { DocumentType } from './client.types';

export interface ShareholderCompanyConfig {
  id: string;
  name?: string;
  type: ShareholderType;
  earnOut: boolean;
  tagAlong: boolean;
  dragAlong: boolean;
  shareholdersAgreement: boolean;
  rightOfFirstRefusal: boolean;
  liquidationPreferenceRight: boolean;
  antiDilution: boolean;
}

export type ShareholderCompanyRequest = Omit<ShareholderCompanyConfig, 'id' | 'name'> & { companyId: string };

export interface Shareholder {
  id: string;
  clientId: string;
  companyId: string;
  companyName: string;
  companies?: ShareholderCompanyConfig[];
  name: string;
  document: string;
  documentFormatted: string;
  documentType: DocumentType;
  email?: string;
  phone?: string;
  type: ShareholderType;
  status: ShareholderStatus;
  notes?: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressZipCode?: string;
  addressCity?: string;
  addressState?: string;
  maritalStatus?: MaritalStatus;
  gender?: Gender;
  birthDate?: string;
  earnOut: boolean;
  tagAlong: boolean;
  dragAlong: boolean;
  shareholdersAgreement: boolean;
  rightOfFirstRefusal: boolean;
  liquidationPreferenceRight: boolean;
  antiDilution: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ShareholderListResponse {
  items: Shareholder[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreateShareholderRequest {
  companyId: string;
  companies?: ShareholderCompanyRequest[];
  name: string;
  document: string;
  documentType: DocumentType;
  type: ShareholderType;
  email?: string;
  phone?: string;
  notes?: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressZipCode?: string;
  addressCity?: string;
  addressState?: string;
  maritalStatus?: MaritalStatus;
  gender?: Gender;
  birthDate?: string;
  earnOut?: boolean;
  tagAlong?: boolean;
  dragAlong?: boolean;
  shareholdersAgreement?: boolean;
  rightOfFirstRefusal?: boolean;
  liquidationPreferenceRight?: boolean;
  antiDilution?: boolean;
}

export interface UpdateShareholderRequest {
  companyId?: string;
  companies?: ShareholderCompanyRequest[];
  name: string;
  document?: string;
  documentType?: DocumentType;
  type: ShareholderType;
  status: ShareholderStatus;
  email?: string;
  phone?: string;
  notes?: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressZipCode?: string;
  addressCity?: string;
  addressState?: string;
  maritalStatus?: MaritalStatus;
  gender?: Gender;
  birthDate?: string;
  earnOut?: boolean;
  tagAlong?: boolean;
  dragAlong?: boolean;
  shareholdersAgreement?: boolean;
  rightOfFirstRefusal?: boolean;
  liquidationPreferenceRight?: boolean;
  antiDilution?: boolean;
}

export interface ShareholderFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  type?: ShareholderType;
  status?: ShareholderStatus;
  companyId?: string;
}

// Helper functions for labels
export const shareholderTypeLabels: Record<ShareholderType, string> = {
  [ShareholderType.Founder]: 'Fundador',
  [ShareholderType.Investor]: 'Investidor',
  [ShareholderType.Employee]: 'Funcionário',
  [ShareholderType.Advisor]: 'Advisor',
  [ShareholderType.ESOP]: 'ESOP',
  [ShareholderType.Other]: 'Outro',
};

export const shareholderStatusLabels: Record<ShareholderStatus, string> = {
  [ShareholderStatus.Active]: 'Ativo',
  [ShareholderStatus.Inactive]: 'Inativo',
  [ShareholderStatus.Pending]: 'Pendente',
  [ShareholderStatus.Exited]: 'Saída',
};

export const genderLabels: Record<Gender, string> = {
  [Gender.Male]: 'Masculino',
  [Gender.Female]: 'Feminino',
  [Gender.Other]: 'Outro',
  [Gender.NotInformed]: 'Não informado',
};

export const maritalStatusLabels: Record<MaritalStatus, string> = {
  [MaritalStatus.Single]: 'Solteiro(a)',
  [MaritalStatus.Married]: 'Casado(a)',
  [MaritalStatus.StableUnion]: 'União estável',
  [MaritalStatus.Divorced]: 'Divorciado(a)',
  [MaritalStatus.Widowed]: 'Viúvo(a)',
};

export const documentTypeLabels: Record<DocumentType, string> = {
  [DocumentType.Cpf]: 'CPF',
  [DocumentType.Cnpj]: 'CNPJ',
};
