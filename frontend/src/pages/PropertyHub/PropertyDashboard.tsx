import React, { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { propertyService, PropertyResponseDto } from '../../services/propertyService';
import { propertyAdminService } from '../../services/propertyAdminService';
import type { TenancyResponseDto } from '../../services/adminService';
import { reminderService, ReminderResponseDto } from '../../services/reminderService';
import { maintenanceService, MaintenanceResponseDto } from '../../services/maintenanceService';
import { contactLogService, ContactLogResponseDto } from '../../services/contactLogService';
import {
  countActiveTenantsForProperty,
  countContactLogsForProperty,
  countMaintenanceForProperty,
  countOpenRemindersForProperty,
  filterTenanciesForProperty,
} from './propertyHubMetrics';
import { formatDateUk } from '../../dateFormat';

const PropertyDashboard: React.FC = () => {
  const { propertyId: idParam } = useParams<{ propertyId: string }>();
  const propertyId = idParam ? parseInt(idParam, 10) : NaN;
  const navigate = useNavigate();

  const [property, setProperty] = useState<PropertyResponseDto | null>(null);
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [reminders, setReminders] = useState<ReminderResponseDto[]>([]);
  const [maintenance, setMaintenance] = useState<MaintenanceResponseDto[]>([]);
  const [contactLogs, setContactLogs] = useState<ContactLogResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!Number.isFinite(propertyId)) {
      setLoading(false);
      setError('Invalid property');
      return;
    }

    const load = async () => {
      try {
        setLoading(true);
        setError(null);
        const [properties, tenanciesData, rem, maint, logs] = await Promise.all([
          propertyService.getProperties(),
          propertyAdminService.getTenancies(),
          reminderService.getReminders(),
          maintenanceService.getMaintenanceRecords(),
          contactLogService.getContactLogs(),
        ]);
        const p = properties.find((x) => x.propertyId === propertyId) ?? null;
        if (!p) {
          setError('Property not found');
          setProperty(null);
        } else {
          setProperty(p);
        }
        setTenancies(tenanciesData);
        setReminders(rem);
        setMaintenance(maint);
        setContactLogs(logs);
      } catch (e: any) {
        setError(e.message || 'Failed to load dashboard');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [propertyId]);

  const scopedTenancies = useMemo(
    () => filterTenanciesForProperty(propertyId, tenancies),
    [propertyId, tenancies]
  );
  const scopedReminders = useMemo(
    () => reminders.filter((r) => r.propertyId === propertyId),
    [reminders, propertyId]
  );
  const scopedMaintenance = useMemo(
    () => maintenance.filter((m) => m.propertyId === propertyId),
    [maintenance, propertyId]
  );
  const scopedContactLogs = useMemo(
    () => contactLogs.filter((l) => l.propertyId === propertyId),
    [contactLogs, propertyId]
  );

  const activeTenants = property
    ? countActiveTenantsForProperty(property.propertyId, tenancies)
    : 0;
  const openReminders = property ? countOpenRemindersForProperty(property.propertyId, reminders) : 0;
  const maintCount = property ? countMaintenanceForProperty(property.propertyId, maintenance) : 0;
  const contactCount = property ? countContactLogsForProperty(property.propertyId, contactLogs) : 0;

  const recentMaintenance = useMemo(
    () =>
      [...scopedMaintenance]
        .sort((a, b) => {
          const da = a.workDate || a.createdDate || '';
          const db = b.workDate || b.createdDate || '';
          return db.localeCompare(da);
        })
        .slice(0, 4),
    [scopedMaintenance]
  );
  const recentReminders = useMemo(
    () =>
      [...scopedReminders]
        .filter((r) => !r.isCompleted)
        .sort((a, b) => (b.createdDate || '').localeCompare(a.createdDate || ''))
        .slice(0, 4),
    [scopedReminders]
  );
  const recentContactLogs = useMemo(
    () =>
      [...scopedContactLogs]
        .sort((a, b) => b.contactDate.localeCompare(a.contactDate))
        .slice(0, 4),
    [scopedContactLogs]
  );

  if (!Number.isFinite(propertyId)) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-6 text-red-800">
        Invalid property link.
        <button type="button" className="ml-4 underline" onClick={() => navigate('/Property Hub/Home')}>
          Home
        </button>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-10 text-center text-slate-600">
        Loading property…
      </div>
    );
  }

  if (error || !property) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-6 text-red-800">
        {error || 'Property not found'}
        <button type="button" className="ml-4 underline" onClick={() => navigate('/Property Hub/Home')}>
          Back to overview
        </button>
      </div>
    );
  }

  const q = `propertyId=${property.propertyId}`;

  const StatCard: React.FC<{ label: string; value: number; hint?: string }> = ({ label, value, hint }) => (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-2 text-3xl font-semibold text-slate-900">{value}</p>
      {hint && <p className="mt-1 text-xs text-slate-500">{hint}</p>}
    </div>
  );

  const Section: React.FC<{ title: string; viewAllTo: string; children: React.ReactNode }> = ({
    title,
    viewAllTo,
    children,
  }) => (
    <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between gap-2">
        <h3 className="text-lg font-semibold text-slate-900">{title}</h3>
        <Link
          to={viewAllTo}
          className="text-sm font-medium text-blue-600 hover:text-blue-800"
        >
          View all
        </Link>
      </div>
      {children}
    </section>
  );

  return (
    <div className="space-y-8">
      <div>
        <Link
          to="/Property Hub/Home"
          className="text-sm font-medium text-slate-600 hover:text-slate-900"
        >
          ← Property overview
        </Link>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight text-slate-900">{property.propertyName}</h1>
        <p className="mt-1 text-sm text-slate-500">
          {property.address}
          {property.postCode ? ` · ${property.postCode}` : ''}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Contact logs" value={contactCount} hint="Recorded for this property" />
        <StatCard label="Active tenants" value={activeTenants} hint="On active tenancies" />
        <StatCard label="Maintenance records" value={maintCount} hint="All jobs for this property" />
        <StatCard label="Open reminders" value={openReminders} hint="Not yet completed" />
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Section title="Recent maintenance" viewAllTo={`/Property Hub/Maintenance?${q}`}>
          {recentMaintenance.length === 0 ? (
            <p className="text-sm text-slate-500">No maintenance records yet.</p>
          ) : (
            <ul className="space-y-3">
              {recentMaintenance.map((m) => (
                <li
                  key={m.maintenanceId}
                  className="flex flex-col gap-1 rounded-lg border border-slate-100 bg-slate-50/80 px-3 py-2"
                >
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-medium text-slate-900">{m.summary || 'Maintenance'}</span>
                    <span className="rounded-full bg-slate-200 px-2 py-0.5 text-xs text-slate-700">
                      {m.maintenanceTypeName}
                    </span>
                    {m.maintenanceStatusName && (
                      <span className="rounded-full border border-slate-300 bg-white px-2 py-0.5 text-xs text-slate-700">
                        {m.maintenanceStatusName}
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-slate-500">
                    {m.workDate
                      ? `Work date ${formatDateUk(m.workDate)}`
                      : m.createdDate
                        ? `Logged ${formatDateUk(m.createdDate)}`
                        : ''}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </Section>

        <Section title="Open reminders" viewAllTo={`/Property Hub/Reminders?${q}`}>
          {recentReminders.length === 0 ? (
            <p className="text-sm text-slate-500">No open reminders.</p>
          ) : (
            <ul className="space-y-3">
              {recentReminders.map((r) => (
                <li
                  key={r.reminderId}
                  className="flex flex-col gap-1 rounded-lg border border-slate-100 bg-slate-50/80 px-3 py-2"
                >
                  <span className="font-medium text-slate-900">{r.title}</span>
                  {r.reminderPriorityName && (
                    <span
                      className="rounded-full px-2 py-0.5 text-xs font-medium text-white"
                      style={{ backgroundColor: r.reminderPriorityColor || '#64748b' }}
                    >
                      {r.reminderPriorityName}
                    </span>
                  )}
                  {r.notes && <p className="text-xs text-slate-600 line-clamp-2">{r.notes}</p>}
                  <p className="text-xs text-slate-500">
                    {r.createdDate ? `Created ${formatDateUk(r.createdDate)}` : ''}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </Section>

        <Section title="Tenants" viewAllTo={`/Property Hub/Tenants?${q}`}>
          {scopedTenancies.length === 0 ? (
            <p className="text-sm text-slate-500">No active tenancies for this property.</p>
          ) : (
            <ul className="space-y-3">
              {scopedTenancies.slice(0, 4).map((t) => (
                <li
                  key={t.tenancyId}
                  className="rounded-lg border border-slate-100 bg-slate-50/80 px-3 py-2 text-sm text-slate-700"
                >
                  <span className="font-medium">
                    {t.description?.slice(0, 60) || `Tenancy #${t.tenancyId}`}
                  </span>
                  <span className="ml-2 text-slate-500">
                    {(t.tenants?.length ?? 0)} {(t.tenants?.length === 1 ? 'tenant' : 'tenants')}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Section>

        <Section title="Recent contact logs" viewAllTo={`/Property Hub/Contact Logs?${q}`}>
          {recentContactLogs.length === 0 ? (
            <p className="text-sm text-slate-500">No contact logs for this property.</p>
          ) : (
            <ul className="space-y-3">
              {recentContactLogs.map((log) => (
                <li
                  key={log.contactLogId}
                  className="flex flex-col gap-1 rounded-lg border border-slate-100 bg-slate-50/80 px-3 py-2"
                >
                  <span className="font-medium text-slate-900">{log.subject}</span>
                  <p className="text-xs text-slate-500">
                    {log.contactLogTypeName} · {formatDateUk(log.contactDate)}
                    {log.tenantName ? ` · ${log.tenantName}` : ''}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </Section>
      </div>
    </div>
  );
};

export default PropertyDashboard;
