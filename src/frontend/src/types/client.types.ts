export enum DocumentType {
  Cpf = 1,
  Cnpj = 2,
}

export enum ClientStatus {
  Active = 1,
  Inactive = 2,
  Suspended = 3,
}

export interface Client {
  id: string;
  name: string;
  document: string;
  documentType: DocumentType;
  email: string;
  phone?: string;
  status: ClientStatus;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateClientRequest {
  name: string;
  document: string;
  documentType: DocumentType;
  email: string;
  phone?: string;
}

export interface OnboardClientRequest {
  client: CreateClientRequest & { tradingName?: string };
  company: {
    name: string;
    tradingName?: string;
    cnpj: string;
    legalForm: number;
    foundationDate: string;
    totalShares: number;
    sharePrice: number;
    currency: string;
  };
  administratorName: string;
  administratorEmail: string;
}

export interface ClientOnboardingResponse {
  clientId: string;
  companyId: string;
  administratorId: string;
  activationEmailSent: boolean;
}

export interface CoreClientSummary {
  id: string;
  name: string;
  tradingName?: string;
  documentFormatted: string;
  email: string;
  status: string;
  totalCompanies: number;
  totalUsers: number;
}

export interface CoreClientPage {
  items: CoreClientSummary[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface UpdateClientRequest {
  name?: string;
  email?: string;
  phone?: string;
}

export interface ClientListResponse {
  items: Client[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ClientCompany {
  id: string;
  name: string;
  cnpj?: string;
  cnpjFormatted?: string;
  valuation?: number;
  status: string | number;
  createdAt?: string;
}
