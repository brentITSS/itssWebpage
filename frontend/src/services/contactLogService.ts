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

// Contact Log DTOs
export interface ContactLogResponseDto {
  contactLogId: number;
  propertyId: number;
  propertyName: string;
  propertyGroupId?: number;
  propertyGroupName?: string;
  tenantId?: number;
  tenantName?: string;
  contactLogTypeId: number;
  contactLogTypeName: string;
  subject: string;
  notes: string;
  contactDate: string;
  createdDate: string;
  hasCalendarAppointment: boolean;
  calendarDate?: string;
  attachments: AttachmentDto[];
  tags: TagDto[];
}

export interface CreateContactLogRequest {
  propertyId: number;
  tenantId?: number;
  contactLogTypeId: number;
  subject: string;
  notes: string;
  contactDate: string;
  addToCalendar?: boolean;
  calendarDate?: string;
}

export interface UpdateContactLogRequest {
  propertyId?: number;
  tenantId?: number;
  contactLogTypeId?: number;
  subject?: string;
  notes?: string;
  contactDate?: string;
  addToCalendar?: boolean;
  calendarDate?: string;
}

export interface ContactLogTypeDto {
  contactLogTypeId: number;
  contactLogTypeName: string;
  description?: string;
}

export interface AttachmentDto {
  attachmentId: number;
  fileName: string;
  fileType?: string;
  fileSize: number;
  createdDate: string;
}

export interface TagDto {
  tagLogId: number;
  tagTypeId: number;
  tagTypeName: string;
  color?: string;
  createdDate: string;
}

export interface DeleteImpactDto {
  entityId: number;
  attachmentCount: number;
  tagCount: number;
  calendarAppointmentCount: number;
}

export const contactLogService = {
  // Contact Logs
  getContactLogs: async (): Promise<ContactLogResponseDto[]> => {
    return await apiClient<ContactLogResponseDto[]>('/contact-logs');
  },

  getContactLog: async (id: number): Promise<ContactLogResponseDto> => {
    return await apiClient<ContactLogResponseDto>(`/contact-logs/${id}`);
  },

  getContactLogsByProperty: async (propertyId: number): Promise<ContactLogResponseDto[]> => {
    return await apiClient<ContactLogResponseDto[]>(`/contact-logs/property/${propertyId}`);
  },

  getContactLogsByTenant: async (tenantId: number): Promise<ContactLogResponseDto[]> => {
    return await apiClient<ContactLogResponseDto[]>(`/contact-logs/tenant/${tenantId}`);
  },

  createContactLog: async (request: CreateContactLogRequest): Promise<ContactLogResponseDto> => {
    return await apiClient<ContactLogResponseDto>('/contact-logs', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateContactLog: async (id: number, request: UpdateContactLogRequest): Promise<ContactLogResponseDto> => {
    return await apiClient<ContactLogResponseDto>(`/contact-logs/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteContactLog: async (id: number): Promise<void> => {
    await apiClient<void>(`/contact-logs/${id}`, {
      method: 'DELETE',
    });
  },

  getDeleteImpact: async (id: number): Promise<DeleteImpactDto> => {
    return await apiClient<DeleteImpactDto>(`/contact-logs/${id}/delete-impact`);
  },

  // Contact Log Types
  getContactLogTypes: async (): Promise<ContactLogTypeDto[]> => {
    return await apiClient<ContactLogTypeDto[]>('/contact-logs/types');
  },

  // Attachments
  addAttachment: async (contactLogId: number, file: File): Promise<AttachmentDto> => {
    const formData = new FormData();
    formData.append('file', file);

    const token = localStorage.getItem('token');
    const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';

    const response = await fetch(`${API_BASE_URL}/contact-logs/${contactLogId}/attachments`, {
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
    await apiClient<void>(`/contact-logs/attachments/${attachmentId}`, {
      method: 'DELETE',
    });
  },

  downloadAttachment: async (attachmentId: number): Promise<{ blob: Blob; fileName: string }> => {
    const token = localStorage.getItem('token');
    const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';
    const response = await fetch(`${API_BASE_URL}/contact-logs/attachments/${attachmentId}/download`, {
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
      `contact-attachment-${attachmentId}`,
      blob.type || response.headers.get('Content-Type')
    );
    return { blob, fileName };
  },
};
