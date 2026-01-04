import apiClient from './api';

// Property Group DTOs
export interface PropertyGroupResponseDto {
  propertyGroupId: number;
  propertyGroupName: string;
  description?: string;
  createdDate: string;
  propertyCount: number;
}

// Property DTOs
export interface PropertyResponseDto {
  propertyId: number;
  propertyGroupId: number;
  propertyGroupName: string;
  propertyName: string;
  address?: string;
  postCode?: string;
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
};
