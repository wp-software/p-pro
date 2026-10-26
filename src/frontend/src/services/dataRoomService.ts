import { api } from './api';
import type { DataRoom, DataRoomFolder, DataRoomActivityLog, FinancialDocument } from '@/types/phase6';
import type { Document } from '@/types';

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
}

export interface CreateFolderRequest {
  name: string;
  description?: string;
  parentId?: string;
  visibility?: 'internal' | 'investors' | 'public';
}

export const dataRoomService = {
  async getDataRoom(): Promise<DataRoom> {
    const response = await api.get<ApiResponse<DataRoom>>('/dataroom');
    return response.data.data;
  },

  async getFolders(parentId?: string): Promise<DataRoomFolder[]> {
    const response = await api.get<ApiResponse<DataRoomFolder[]>>('/dataroom/folders', {
      params: parentId ? { parentId } : undefined,
    });
    return response.data.data;
  },

  async createFolder(data: CreateFolderRequest): Promise<DataRoomFolder> {
    const response = await api.post<ApiResponse<DataRoomFolder>>('/dataroom/folders', data);
    return response.data.data;
  },

  async deleteFolder(id: string): Promise<void> {
    await api.delete(`/dataroom/folders/${id}`);
  },

  async getDocumentsInFolder(folderId: string): Promise<Document[]> {
    const response = await api.get<ApiResponse<Document[]>>(`/dataroom/folders/${folderId}/documents`);
    return response.data.data;
  },

  async addDocumentToFolder(folderId: string, documentId: string): Promise<void> {
    await api.post(`/dataroom/folders/${folderId}/documents`, { documentId });
  },

  async removeDocumentFromFolder(folderId: string, documentId: string): Promise<void> {
    await api.delete(`/dataroom/folders/${folderId}/documents/${documentId}`);
  },

  async setFolderRoles(folderId: string, roles: string[]): Promise<void> {
    await api.put(`/dataroom/folders/${folderId}/roles`, { roles });
  },

  async getActivityLogs(folderId?: string, limit = 100): Promise<DataRoomActivityLog[]> {
    const response = await api.get<ApiResponse<DataRoomActivityLog[]>>('/dataroom/activity-logs', {
      params: { folderId, limit },
    });
    return response.data.data;
  },

  async uploadFinancialDocument(params: {
    file: File;
    year: number;
    month: number;
    periodId?: string;
    metricKey?: string;
    name?: string;
    documentType?: string;
  }): Promise<{ documentId: string; folderId: string; folderPath: string }> {
    const form = new FormData();
    form.append('file', params.file);
    form.append('year', String(params.year));
    form.append('month', String(params.month));
    if (params.periodId) form.append('periodId', params.periodId);
    if (params.metricKey) form.append('metricKey', params.metricKey);
    if (params.name) form.append('name', params.name);
    if (params.documentType) form.append('documentType', params.documentType);
    const response = await api.post<
      ApiResponse<{ documentId: string; folderId: string; folderPath: string }>
    >('/dataroom/financial-documents', form, { headers: { 'Content-Type': 'multipart/form-data' } });
    return response.data.data;
  },

  async getFinancialDocuments(periodId: string): Promise<FinancialDocument[]> {
    const response = await api.get<ApiResponse<FinancialDocument[]>>('/dataroom/financial-documents', {
      params: { periodId },
    });
    return response.data.data;
  },

  async removeFinancialDocument(documentId: string): Promise<void> {
    await api.delete(`/dataroom/financial-documents/${documentId}`);
  },

  async downloadDocument(folderId: string, documentId: string, fileName: string): Promise<void> {
    const response = await api.get<Blob>(`/dataroom/folders/${folderId}/documents/${documentId}/download`, {
      responseType: 'blob',
    });
    const url = URL.createObjectURL(response.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  },
};
