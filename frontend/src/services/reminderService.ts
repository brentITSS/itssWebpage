import apiClient from './api';

export interface ReminderResponseDto {
  reminderId: number;
  propertyGroupId?: number;
  propertyGroupName?: string;
  propertyId?: number;
  propertyName?: string;
  title: string;
  notes?: string;
  dueDate: string;
  isCompleted: boolean;
  createdDate?: string;
}

export interface CreateReminderRequest {
  propertyGroupId?: number;
  propertyId?: number;
  title: string;
  notes?: string;
  dueDate: string;
  isCompleted: boolean;
}

export interface UpdateReminderRequest {
  propertyGroupId?: number;
  propertyId?: number;
  title?: string;
  notes?: string;
  dueDate?: string;
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
};
