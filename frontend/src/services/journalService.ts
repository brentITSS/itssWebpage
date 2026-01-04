import apiClient from './api';

// Journal Log DTOs
export interface JournalLogResponseDto {
  journalLogId: number;
  propertyId: number;
  propertyName: string;
  tenancyId?: number;
  tenantId?: number;
  tenantName?: string;
  journalTypeId: number;
  journalTypeName: string;
  journalSubTypeId?: number;
  journalSubTypeName?: string;
  amount: number;
  description?: string;
  transactionDate: string;
  createdDate: string;
  attachments: AttachmentDto[];
}

export interface CreateJournalLogRequest {
  propertyId: number;
  tenancyId?: number;
  tenantId?: number;
  journalTypeId: number;
  journalSubTypeId?: number;
  amount: number;
  description?: string;
  transactionDate: string;
}

export interface UpdateJournalLogRequest {
  propertyId?: number;
  tenancyId?: number;
  tenantId?: number;
  journalTypeId?: number;
  journalSubTypeId?: number;
  amount?: number;
  description?: string;
  transactionDate?: string;
}

export interface JournalTypeDto {
  journalTypeId: number;
  journalTypeName: string;
  description?: string;
  subTypes: JournalSubTypeDto[];
}

export interface JournalSubTypeDto {
  journalSubTypeId: number;
  journalSubTypeName: string;
  description?: string;
}

export interface AttachmentDto {
  attachmentId: number;
  fileName: string;
  fileType?: string;
  fileSize: number;
  createdDate: string;
}

export const journalService = {
  // Journal Logs
  getJournalLogs: async (): Promise<JournalLogResponseDto[]> => {
    return await apiClient<JournalLogResponseDto[]>('/journals');
  },

  getJournalLog: async (id: number): Promise<JournalLogResponseDto> => {
    return await apiClient<JournalLogResponseDto>(`/journals/${id}`);
  },

  getJournalLogsByProperty: async (propertyId: number): Promise<JournalLogResponseDto[]> => {
    return await apiClient<JournalLogResponseDto[]>(`/journals/property/${propertyId}`);
  },

  createJournalLog: async (request: CreateJournalLogRequest): Promise<JournalLogResponseDto> => {
    return await apiClient<JournalLogResponseDto>('/journals', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateJournalLog: async (id: number, request: UpdateJournalLogRequest): Promise<JournalLogResponseDto> => {
    return await apiClient<JournalLogResponseDto>(`/journals/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteJournalLog: async (id: number): Promise<void> => {
    await apiClient<void>(`/journals/${id}`, {
      method: 'DELETE',
    });
  },

  // Journal Types
  getJournalTypes: async (): Promise<JournalTypeDto[]> => {
    return await apiClient<JournalTypeDto[]>('/journals/types');
  },

  // Attachments
  addAttachment: async (journalLogId: number, file: File): Promise<AttachmentDto> => {
    const formData = new FormData();
    formData.append('file', file);

    const token = localStorage.getItem('token');
    const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';

    const response = await fetch(`${API_BASE_URL}/journals/${journalLogId}/attachments`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
      body: formData,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  },

  deleteAttachment: async (attachmentId: number): Promise<void> => {
    await apiClient<void>(`/journals/attachments/${attachmentId}`, {
      method: 'DELETE',
    });
  },
};
