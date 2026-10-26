// Document Types — mirrors backend DTOs

// ─── Enums ────────────────────────────────────────────────────────────────────

export enum DocumentVisibility {
  Admin = 'admin',
  Internal = 'internal',
  Shareholder = 'shareholder',
  Public = 'public',
}

// ─── Labels ───────────────────────────────────────────────────────────────────

export const documentVisibilityLabels: Record<string, string> = {
  admin: 'Administrador',
  internal: 'Interno',
  shareholder: 'Acionista',
  public: 'Público',
};

export const documentVisibilityColors: Record<string, string> = {
  admin: 'bg-red-100 text-red-700',
  internal: 'bg-blue-100 text-blue-700',
  shareholder: 'bg-purple-100 text-purple-700',
  public: 'bg-green-100 text-green-700',
};

// ─── Interfaces ───────────────────────────────────────────────────────────────

export interface Document {
  id: string;
  clientId: string;
  companyId: string;
  name: string;
  documentType: string;
  description: string | null;
  fileName: string;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  mimeType: string;
  storagePath: string;
  downloadUrl: string | null;
  entityType: string | null;
  entityId: string | null;
  source?: 'dataroom' | 'financial' | 'documents';
  visibility: string;
  isVerified: boolean;
  verifiedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface DocumentListResponse {
  items: Document[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ─── Filters ──────────────────────────────────────────────────────────────────

export interface DocumentFilters {
  companyId?: string;
  page?: number;
  pageSize?: number;
  documentType?: string;
  visibility?: string;
  search?: string;
  entityType?: string;
  entityId?: string;
}

// ─── Requests ─────────────────────────────────────────────────────────────────

export interface CreateDocumentRequest {
  companyId: string;
  name: string;
  documentType: string;
  description?: string;
  fileName: string;
  fileSizeBytes: number;
  mimeType: string;
  storagePath: string;
  visibility?: string;
  entityType?: string;
  entityId?: string;
  downloadUrl?: string;
}

export interface UpdateDocumentMetadataRequest {
  name: string;
  description?: string;
  visibility: string;
}
