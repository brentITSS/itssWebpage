import apiClient from './api';
import type { ReminderPriorityDto } from './propertyAdminService';

export type { ReminderPriorityDto };

export interface ReminderResponseDto {
  reminderId: number;
  tenantId?: number;
  tenantName?: string;
  tenancyId?: number;
  tenancySummary?: string;
  propertyGroupId?: number;
  propertyGroupName?: string;
  propertyId?: number;
  propertyName?: string;
  title: string;
  reminderPriorityId?: number;
  reminderPriorityName?: string;
  reminderPriorityColor?: string;
  notes?: string;
  createdBy?: string;
  createdDate?: string;
  reminderActive?: boolean;
  isCompleted: boolean;
}

export interface CreateReminderRequest {
  tenantId?: number;
  tenancyId?: number;
  propertyGroupId?: number;
  propertyId?: number;
  title: string;
  reminderPriorityId?: number;
  notes?: string;
  isCompleted: boolean;
}

export interface UpdateReminderRequest {
  /** Use null to clear a link (required for full replacement on the API). */
  tenantId?: number | null;
  tenancyId?: number | null;
  propertyGroupId?: number | null;
  propertyId?: number | null;
  reminderPriorityId?: number | null;
  title?: string;
  notes?: string | null;
  isCompleted?: boolean;
}

export const reminderService = {
  getReminders: async (): Promise<ReminderResponseDto[]> => {
    return await apiClient<ReminderResponseDto[]>('/reminders');
  },

  getReminder: async (id: number): Promise<ReminderResponseDto> => {
    return await apiClient<ReminderResponseDto>(`/reminders/${id}`);
  },

  createReminder: async (request: CreateReminderRequest): Promise<ReminderResponseDto> => {
    return await apiClient<ReminderResponseDto>('/reminders', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateReminder: async (id: number, request: UpdateReminderRequest): Promise<ReminderResponseDto> => {
    return await apiClient<ReminderResponseDto>(`/reminders/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteReminder: async (id: number): Promise<void> => {
    await apiClient<void>(`/reminders/${id}`, { method: 'DELETE' });
  },

  getReminderPriorities: async (): Promise<ReminderPriorityDto[]> => {
    return await apiClient<ReminderPriorityDto[]>('/lookups/reminder-priorities');
  },
};
