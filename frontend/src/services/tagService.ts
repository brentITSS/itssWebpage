import apiClient from './api';

// Tag Type DTOs
export interface TagTypeResponseDto {
  tagTypeId: number;
  tagTypeName: string;
  color?: string;
  description?: string;
  createdDate: string;
}

export interface CreateTagTypeRequest {
  tagTypeName: string;
  color?: string;
  description?: string;
}

export interface UpdateTagTypeRequest {
  tagTypeName?: string;
  color?: string;
  description?: string;
}

// Tag Log DTOs
export interface TagDto {
  tagLogId: number;
  tagTypeId: number;
  tagTypeName: string;
  color?: string;
  entityType: string;
  entityId: number;
  createdDate: string;
}

export interface CreateTagLogRequest {
  tagTypeId: number;
  entityType: string;
  entityId: number;
}

export const tagService = {
  // Tag Types
  getTagTypes: async (): Promise<TagTypeResponseDto[]> => {
    return await apiClient<TagTypeResponseDto[]>('/tags');
  },

  getTagType: async (id: number): Promise<TagTypeResponseDto> => {
    return await apiClient<TagTypeResponseDto>(`/tags/${id}`);
  },

  createTagType: async (request: CreateTagTypeRequest): Promise<TagTypeResponseDto> => {
    return await apiClient<TagTypeResponseDto>('/tags', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateTagType: async (id: number, request: UpdateTagTypeRequest): Promise<TagTypeResponseDto> => {
    return await apiClient<TagTypeResponseDto>(`/tags/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteTagType: async (id: number): Promise<void> => {
    await apiClient<void>(`/tags/${id}`, {
      method: 'DELETE',
    });
  },

  // Tag Logs
  getTagLogsByEntity: async (entityType: string, entityId: number): Promise<TagDto[]> => {
    return await apiClient<TagDto[]>(`/tag-log?entityType=${encodeURIComponent(entityType)}&entityId=${entityId}`);
  },

  createTagLog: async (request: CreateTagLogRequest): Promise<TagDto> => {
    return await apiClient<TagDto>('/tag-log', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  deleteTagLog: async (tagLogId: number): Promise<void> => {
    await apiClient<void>(`/tag-log/${tagLogId}`, {
      method: 'DELETE',
    });
  },
};
