import apiClient from './api';

const extensionFromContentType = (contentType: string | null): string => {
  const normalized = (contentType || '').toLowerCase();
  if (normalized.includes('pdf')) return '.pdf';
  if (normalized.includes('json')) return '.json';
  if (normalized.includes('csv')) return '.csv';
  if (normalized.includes('xml')) return '.xml';
  if (normalized.includes('plain')) return '.txt';
  if (normalized.includes('msword')) return '.doc';
  if (normalized.includes('wordprocessingml')) return '.docx';
  if (normalized.includes('excel')) return '.xls';
  if (normalized.includes('spreadsheetml')) return '.xlsx';
  if (normalized.includes('png')) return '.png';
  if (normalized.includes('jpeg')) return '.jpg';
  if (normalized.includes('gif')) return '.gif';
  return '.bin';
};

const parseDownloadFileName = (
  disposition: string,
  fallbackBaseName: string,
  contentType: string | null
): string => {
  const utf8Match = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  const plainMatch = disposition.match(/filename="?([^";]+)"?/i);
  const raw = (utf8Match?.[1] || plainMatch?.[1] || '').trim();
  const decoded = raw ? decodeURIComponent(raw.replace(/"/g, '')) : '';
  if (decoded) return decoded;
  return `${fallbackBaseName}${extensionFromContentType(contentType)}`;
};

// Journal Log DTOs
export interface JournalLogResponseDto {
  journalLogId: number;
  propertyId: number;
  propertyName: string;
  propertyGroupId?: number;
  propertyGroupName?: string;
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
  hasCalendarAppointment: boolean;
  calendarDate?: string;
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
  addToCalendar?: boolean;
  calendarDate?: string;
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
  addToCalendar?: boolean;
  calendarDate?: string;
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

export interface DeleteImpactDto {
  entityId: number;
  attachmentCount: number;
  tagCount: number;
  calendarAppointmentCount: number;
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

  getDeleteImpact: async (id: number): Promise<DeleteImpactDto> => {
    return await apiClient<DeleteImpactDto>(`/journals/${id}/delete-impact`);
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

  downloadAttachment: async (attachmentId: number): Promise<{ blob: Blob; fileName: string }> => {
    const token = localStorage.getItem('token');
    const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';
    const response = await fetch(`${API_BASE_URL}/journals/attachments/${attachmentId}/download`, {
      method: 'GET',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    if (!response.ok) {
      const err = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(err.message || `HTTP error! status: ${response.status}`);
    }

    const blob = await response.blob();
    const disposition = response.headers.get('Content-Disposition') || '';
    const fileName = parseDownloadFileName(
      disposition,
      `journal-attachment-${attachmentId}`,
      blob.type || response.headers.get('Content-Type')
    );
    return { blob, fileName };
  },
};
