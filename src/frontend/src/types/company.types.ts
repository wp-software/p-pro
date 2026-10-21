export enum LegalForm {
  LTDA = 1,
  SA = 2,
  EI = 3,
  EIRELI = 4,
  SLU = 5,
  Outros = 6,
}

export const LegalFormLabels: Record<LegalForm, string> = {
  [LegalForm.LTDA]: 'Sociedade Limitada (LTDA)',
  [LegalForm.SA]: 'Sociedade Anônima (S.A.)',
  [LegalForm.EI]: 'Empresário Individual (EI)',
  [LegalForm.EIRELI]: 'EIRELI',
  [LegalForm.SLU]: 'Sociedade Limitada Unipessoal (SLU)',
  [LegalForm.Outros]: 'Outros',
};

export interface Company {
  id: string;
  name: string;
  tradingName?: string;
  cnpj: string;
  cnpjFormatted: string;
  legalForm: string;
  foundationDate: string;
  totalShares: number;
  sharePrice: number;
  currency: string;
  valuation: number;
  logoUrl?: string;
  
  // Address fields
  cep?: string;
  street?: string;
  number?: string;
  complement?: string;
  neighborhood?: string;
  city?: string;
  state?: string;
  
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface CompanySummary {
  id: string;
  name: string;
  cnpjFormatted: string;
  valuation: number;
  status: string;
  totalUsers: number;
}

export interface CreateCompanyRequest {
  name: string;
  tradingName?: string;
  cnpj: string;
  legalForm: LegalForm;
  foundationDate: string;
  totalShares: number;
  sharePrice: number;
  currency?: string;
}

export interface UpdateCompanyRequest {
  name: string;
  tradingName?: string;
  logoUrl?: string;
  totalShares?: number;
  sharePrice?: number;
  currency?: string;
  
  // Address fields
  cep?: string;
  street?: string;
  number?: string;
  complement?: string;
  neighborhood?: string;
  city?: string;
  state?: string;
}

export interface UpdateShareInfoRequest {
  totalShares: number;
  sharePrice: number;
}

export interface CompanyListResponse {
  items: Company[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
