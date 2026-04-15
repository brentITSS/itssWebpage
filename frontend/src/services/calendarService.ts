import apiClient from './api';

export interface CalendarEventDto {
  eventType: string;
  sourceId: number;
  title: string;
  start: string;
  end?: string;
  isAllDay: boolean;
  description?: string;
  isCompleted: boolean;
  color?: string;
  propertyGroupId?: number;
  propertyGroupName?: string;
  propertyId?: number;
  propertyName?: string;
  tenancyId?: number;
  tenancySummary?: string;
  tenantId?: number;
  tenantName?: string;
}

export interface CalendarEventsQuery {
  from?: string;
  to?: string;
  propertyGroupId?: number;
  propertyId?: number;
  tenancyId?: number;
  tenantId?: number;
  includeCompleted?: boolean;
}

const queryToString = (query: CalendarEventsQuery): string => {
  const params = new URLSearchParams();
  if (query.from) params.set('from', query.from);
  if (query.to) params.set('to', query.to);
  if (query.propertyGroupId) params.set('propertyGroupId', String(query.propertyGroupId));
  if (query.propertyId) params.set('propertyId', String(query.propertyId));
  if (query.tenancyId) params.set('tenancyId', String(query.tenancyId));
  if (query.tenantId) params.set('tenantId', String(query.tenantId));
  if (query.includeCompleted !== undefined) params.set('includeCompleted', String(query.includeCompleted));
  const queryString = params.toString();
  return queryString ? `?${queryString}` : '';
};

export const calendarService = {
  getEvents: async (query: CalendarEventsQuery): Promise<CalendarEventDto[]> => {
    return await apiClient<CalendarEventDto[]>(`/calendar/events${queryToString(query)}`);
  },
};
