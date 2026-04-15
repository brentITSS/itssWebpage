import apiClient from './api';
import type { ReminderPriorityDto } from './propertyAdminService';

export type { ReminderPriorityDto };
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5001/api';

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
  reminderDate?: string;
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
  reminderDate?: string;
  isCompleted: boolean;
}

export interface OverdueRemindersQuery {
  propertyGroupId?: number;
  propertyId?: number;
  tenancyId?: number;
  tenantId?: number;
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
  reminderDate?: string | null;
  isCompleted?: boolean;
}

export const reminderService = {
  getReminders: async (): Promise<ReminderResponseDto[]> => {
    return await apiClient<ReminderResponseDto[]>('/reminders');
  },

  getReminder: async (id: number): Promise<ReminderResponseDto> => {
    return await apiClient<ReminderResponseDto>(`/reminders/${id}`);
  },

  getOverdueReminders: async (query?: OverdueRemindersQuery): Promise<ReminderResponseDto[]> => {
    const params = new URLSearchParams();
    if (query?.propertyGroupId) params.set('propertyGroupId', String(query.propertyGroupId));
    if (query?.propertyId) params.set('propertyId', String(query.propertyId));
    if (query?.tenancyId) params.set('tenancyId', String(query.tenancyId));
    if (query?.tenantId) params.set('tenantId', String(query.tenantId));
    const qs = params.toString();
    return await apiClient<ReminderResponseDto[]>(`/reminders/overdue${qs ? `?${qs}` : ''}`);
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

  downloadReminderAppointment: async (id: number): Promise<void> => {
    const token = localStorage.getItem('token');
    const response = await fetch(`${API_BASE_URL}/reminders/${id}/ics`, {
      method: 'GET',
      headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    });

    if (!response.ok) {
      const raw = await response.text();
      let msg = raw;
      try {
        const parsed = JSON.parse(raw) as { message?: string };
        if (parsed?.message) msg = parsed.message;
      } catch {
        /* keep raw text */
      }
      throw new Error(msg || 'Failed to download appointment');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `reminder-${id}.ics`;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    window.URL.revokeObjectURL(url);
  },
};
