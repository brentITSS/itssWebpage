import apiClient from './api';
import type { MaintenanceTypeDto } from './propertyAdminService';

export type { MaintenanceTypeDto };

export interface MaintenanceResponseDto {
  maintenanceId: number;
  propertyGroupId: number;
  propertyGroupName?: string;
  propertyId: number;
  propertyName?: string;
  maintenanceTypeId: number;
  maintenanceTypeName: string;
  summary?: string;
  detailNotes?: string;
  workDate?: string;
  createdDate?: string;
}

export interface CreateMaintenanceRequest {
  propertyGroupId: number;
  propertyId: number;
  maintenanceTypeId: number;
  summary?: string;
  detailNotes?: string;
  workDate?: string;
}

export interface UpdateMaintenanceRequest {
  propertyGroupId?: number;
  propertyId?: number;
  maintenanceTypeId?: number;
  summary?: string;
  detailNotes?: string;
  workDate?: string;
}

export const maintenanceService = {
  getMaintenanceRecords: async (): Promise<MaintenanceResponseDto[]> => {
    return await apiClient<MaintenanceResponseDto[]>('/maintenance');
  },

  getMaintenanceRecord: async (id: number): Promise<MaintenanceResponseDto> => {
    return await apiClient<MaintenanceResponseDto>(`/maintenance/${id}`);
  },

  createMaintenanceRecord: async (request: CreateMaintenanceRequest): Promise<MaintenanceResponseDto> => {
    return await apiClient<MaintenanceResponseDto>('/maintenance', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateMaintenanceRecord: async (id: number, request: UpdateMaintenanceRequest): Promise<MaintenanceResponseDto> => {
    return await apiClient<MaintenanceResponseDto>(`/maintenance/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteMaintenanceRecord: async (id: number): Promise<void> => {
    await apiClient<void>(`/maintenance/${id}`, { method: 'DELETE' });
  },

  getMaintenanceTypes: async (): Promise<MaintenanceTypeDto[]> => {
    return await apiClient<MaintenanceTypeDto[]>('/lookups/maintenance-types');
  },
};
