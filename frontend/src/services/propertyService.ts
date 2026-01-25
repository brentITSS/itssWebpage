import apiClient from './api';

// Property Group DTOs
export interface CreatePropertyGroupRequest {
  propertyGroupName: string;
  description?: string;
  isActive?: boolean;
}

export interface UpdatePropertyGroupRequest {
  propertyGroupName?: string;
  description?: string;
  isActive?: boolean;
}

export interface PropertyGroupResponseDto {
  propertyGroupId: number;
  propertyGroupName: string;
  description?: string;
  isActive?: boolean;
  createdDate: string;
  propertyCount: number;
}

// Property DTOs
export interface CreatePropertyRequest {
  propertyGroupId: number;
  propertyName: string;
  address?: string;
  postCode?: string;
  isActive?: boolean;
}

export interface UpdatePropertyRequest {
  propertyGroupId?: number;
  propertyName?: string;
  address?: string;
  postCode?: string;
  isActive?: boolean;
}

export interface PropertyResponseDto {
  propertyId: number;
  propertyGroupId: number;
  propertyGroupName: string;
  propertyName: string;
  address?: string;
  postCode?: string;
  isActive?: boolean;
  createdDate: string;
}

export const propertyService = {
  // Property Groups
  getPropertyGroups: async (): Promise<PropertyGroupResponseDto[]> => {
    return await apiClient<PropertyGroupResponseDto[]>('/property-groups');
  },

  getPropertyGroup: async (id: number): Promise<PropertyGroupResponseDto> => {
    return await apiClient<PropertyGroupResponseDto>(`/property-groups/${id}`);
  },

  // Properties
  getProperties: async (): Promise<PropertyResponseDto[]> => {
    return await apiClient<PropertyResponseDto[]>('/properties');
  },

  getProperty: async (id: number): Promise<PropertyResponseDto> => {
    return await apiClient<PropertyResponseDto>(`/properties/${id}`);
  },

  // Property Group User management
  getPropertyGroupUsers: async (propertyGroupId: number): Promise<any[]> => {
    return await apiClient<any[]>(`/property-groups/${propertyGroupId}/users`);
  },

  assignUserToPropertyGroup: async (propertyGroupId: number, userId: number): Promise<void> => {
    return await apiClient<void>(`/property-groups/${propertyGroupId}/users`, {
      method: 'POST',
      body: JSON.stringify({ userId }),
    });
  },

  removeUserFromPropertyGroup: async (propertyGroupId: number, userId: number): Promise<void> => {
    return await apiClient<void>(`/property-groups/${propertyGroupId}/users/${userId}`, {
      method: 'DELETE',
    });
  },
};
