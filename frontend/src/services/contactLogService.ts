import apiClient from './api';

// Contact Log DTOs
export interface ContactLogResponseDto {
  contactLogId: number;
  propertyId: number;
  propertyName: string;
  tenantId?: number;
  tenantName?: string;
  contactLogTypeId: number;
  contactLogTypeName: string;
  subject: string;
  notes: string;
  contactDate: string;
  createdDate: string;
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
}

export interface UpdateContactLogRequest {
  propertyId?: number;
  tenantId?: number;
  contactLogTypeId?: number;
  subject?: string;
  notes?: string;
  contactDate?: string;
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
};
