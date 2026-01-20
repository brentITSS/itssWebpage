import apiClient from './api';
import { PropertyGroupResponseDto, PropertyResponseDto, CreatePropertyGroupRequest, UpdatePropertyGroupRequest, CreatePropertyRequest, UpdatePropertyRequest } from './propertyService';
import { TenantResponseDto, CreateTenantRequest, UpdateTenantRequest, TenancyResponseDto, CreateTenancyRequest, UpdateTenancyRequest } from './adminService';

// Re-export property service types
export type { PropertyGroupResponseDto, PropertyResponseDto, CreatePropertyGroupRequest, UpdatePropertyGroupRequest, CreatePropertyRequest, UpdatePropertyRequest };

// Tenant DTOs
export type { TenantResponseDto, CreateTenantRequest, UpdateTenantRequest };

// Tenancy DTOs
export type { TenancyResponseDto, CreateTenancyRequest, UpdateTenancyRequest };

// Lookup DTOs
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

export interface ContactLogTypeDto {
  contactLogTypeId: number;
  contactLogTypeName: string;
  description?: string;
}

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

export interface CreateJournalTypeRequest {
  journalTypeName: string;
  description?: string;
}

export interface UpdateJournalTypeRequest {
  journalTypeName?: string;
  description?: string;
}

export interface CreateContactLogTypeRequest {
  contactLogTypeName: string;
  description?: string;
}

export interface UpdateContactLogTypeRequest {
  contactLogTypeName?: string;
  description?: string;
}

export const propertyAdminService = {
  // Property Groups
  getPropertyGroups: async (): Promise<PropertyGroupResponseDto[]> => {
    return await apiClient<PropertyGroupResponseDto[]>('/property-groups');
  },

  getPropertyGroup: async (id: number): Promise<PropertyGroupResponseDto> => {
    return await apiClient<PropertyGroupResponseDto>(`/property-groups/${id}`);
  },

  createPropertyGroup: async (request: CreatePropertyGroupRequest): Promise<PropertyGroupResponseDto> => {
    return await apiClient<PropertyGroupResponseDto>('/property-groups', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updatePropertyGroup: async (id: number, request: UpdatePropertyGroupRequest): Promise<PropertyGroupResponseDto> => {
    return await apiClient<PropertyGroupResponseDto>(`/property-groups/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deletePropertyGroup: async (id: number): Promise<void> => {
    await apiClient<void>(`/property-groups/${id}`, {
      method: 'DELETE',
    });
  },

  // Properties
  getProperties: async (): Promise<PropertyResponseDto[]> => {
    return await apiClient<PropertyResponseDto[]>('/properties');
  },

  getProperty: async (id: number): Promise<PropertyResponseDto> => {
    return await apiClient<PropertyResponseDto>(`/properties/${id}`);
  },

  createProperty: async (request: CreatePropertyRequest): Promise<PropertyResponseDto> => {
    return await apiClient<PropertyResponseDto>('/properties', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateProperty: async (id: number, request: UpdatePropertyRequest): Promise<PropertyResponseDto> => {
    return await apiClient<PropertyResponseDto>(`/properties/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteProperty: async (id: number): Promise<void> => {
    await apiClient<void>(`/properties/${id}`, {
      method: 'DELETE',
    });
  },

  // Tenants
  getTenants: async (): Promise<TenantResponseDto[]> => {
    return await apiClient<TenantResponseDto[]>('/tenants');
  },

  getTenant: async (id: number): Promise<TenantResponseDto> => {
    return await apiClient<TenantResponseDto>(`/tenants/${id}`);
  },

  createTenant: async (request: CreateTenantRequest): Promise<TenantResponseDto> => {
    return await apiClient<TenantResponseDto>('/tenants', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateTenant: async (id: number, request: UpdateTenantRequest): Promise<TenantResponseDto> => {
    return await apiClient<TenantResponseDto>(`/tenants/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteTenant: async (id: number): Promise<void> => {
    await apiClient<void>(`/tenants/${id}`, {
      method: 'DELETE',
    });
  },

  // Tenancies
  getTenancies: async (): Promise<TenancyResponseDto[]> => {
    return await apiClient<TenancyResponseDto[]>('/tenancies');
  },

  getTenancy: async (id: number): Promise<TenancyResponseDto> => {
    return await apiClient<TenancyResponseDto>(`/tenancies/${id}`);
  },

  createTenancy: async (request: CreateTenancyRequest): Promise<TenancyResponseDto> => {
    return await apiClient<TenancyResponseDto>('/tenancies', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateTenancy: async (id: number, request: UpdateTenancyRequest): Promise<TenancyResponseDto> => {
    return await apiClient<TenancyResponseDto>(`/tenancies/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteTenancy: async (id: number): Promise<void> => {
    await apiClient<void>(`/tenancies/${id}`, {
      method: 'DELETE',
    });
  },

  // Lookups - Journal Types
  getJournalTypes: async (): Promise<JournalTypeDto[]> => {
    return await apiClient<JournalTypeDto[]>('/lookups/journal-types');
  },

  createJournalType: async (request: CreateJournalTypeRequest): Promise<JournalTypeDto> => {
    return await apiClient<JournalTypeDto>('/lookups/journal-types', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateJournalType: async (id: number, request: UpdateJournalTypeRequest): Promise<JournalTypeDto> => {
    return await apiClient<JournalTypeDto>(`/lookups/journal-types/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteJournalType: async (id: number): Promise<void> => {
    await apiClient<void>(`/lookups/journal-types/${id}`, {
      method: 'DELETE',
    });
  },

  // Lookups - Contact Log Types
  getContactLogTypes: async (): Promise<ContactLogTypeDto[]> => {
    return await apiClient<ContactLogTypeDto[]>('/lookups/contact-log-types');
  },

  createContactLogType: async (request: CreateContactLogTypeRequest): Promise<ContactLogTypeDto> => {
    return await apiClient<ContactLogTypeDto>('/lookups/contact-log-types', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateContactLogType: async (id: number, request: UpdateContactLogTypeRequest): Promise<ContactLogTypeDto> => {
    return await apiClient<ContactLogTypeDto>(`/lookups/contact-log-types/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteContactLogType: async (id: number): Promise<void> => {
    await apiClient<void>(`/lookups/contact-log-types/${id}`, {
      method: 'DELETE',
    });
  },

  // Lookups - Tag Types
  getTagTypes: async (): Promise<TagTypeResponseDto[]> => {
    return await apiClient<TagTypeResponseDto[]>('/lookups/tag-types');
  },

  createTagType: async (request: CreateTagTypeRequest): Promise<TagTypeResponseDto> => {
    return await apiClient<TagTypeResponseDto>('/lookups/tag-types', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateTagType: async (id: number, request: UpdateTagTypeRequest): Promise<TagTypeResponseDto> => {
    return await apiClient<TagTypeResponseDto>(`/lookups/tag-types/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteTagType: async (id: number): Promise<void> => {
    await apiClient<void>(`/lookups/tag-types/${id}`, {
      method: 'DELETE',
    });
  },
};
