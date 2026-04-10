import type { TenancyResponseDto } from '../../services/adminService';
import type { ReminderResponseDto } from '../../services/reminderService';
import type { MaintenanceResponseDto } from '../../services/maintenanceService';
import type { ContactLogResponseDto } from '../../services/contactLogService';

/** Tenants on active tenancies for this property (from nested tenancy payloads). */
export function countActiveTenantsForProperty(
  propertyId: number,
  tenancies: TenancyResponseDto[]
): number {
  return tenancies
    .filter((t) => t.propertyId === propertyId && t.isActive !== false)
    .reduce((n, t) => n + (t.tenants?.length ?? 0), 0);
}

export function countOpenRemindersForProperty(
  propertyId: number,
  reminders: ReminderResponseDto[]
): number {
  return reminders.filter((r) => !r.isCompleted && r.propertyId === propertyId).length;
}

export function countMaintenanceForProperty(
  propertyId: number,
  rows: MaintenanceResponseDto[]
): number {
  return rows.filter((m) => m.propertyId === propertyId).length;
}

export function countContactLogsForProperty(
  propertyId: number,
  logs: ContactLogResponseDto[]
): number {
  return logs.filter((l) => l.propertyId === propertyId).length;
}

export function filterTenanciesForProperty(
  propertyId: number,
  tenancies: TenancyResponseDto[]
): TenancyResponseDto[] {
  return tenancies.filter((t) => t.propertyId === propertyId && t.isActive !== false);
}
