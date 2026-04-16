import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { calendarService, CalendarEventDto } from '../../../services/calendarService';
import { propertyService, PropertyGroupResponseDto, PropertyResponseDto } from '../../../services/propertyService';
import { propertyAdminService, TenantResponseDto, TenancyResponseDto } from '../../../services/propertyAdminService';
import { reminderService, ReminderResponseDto } from '../../../services/reminderService';
import { formatDateUk } from '../../../dateFormat';

const weekdayLabels = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

const startOfMonth = (value: Date): Date => new Date(value.getFullYear(), value.getMonth(), 1);
const endOfMonth = (value: Date): Date => new Date(value.getFullYear(), value.getMonth() + 1, 0);
const addMonths = (value: Date, months: number): Date => new Date(value.getFullYear(), value.getMonth() + months, 1);

const toYmd = (value: Date): string => {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const stripTime = (d: Date): Date => {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
};

const startOfToday = (): Date => stripTime(new Date());

/** Monday as first day of week (UK). */
const startOfWeekMonday = (ref: Date): Date => {
  const x = stripTime(ref);
  const day = x.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  x.setDate(x.getDate() + diff);
  return x;
};

const endOfWeekMonday = (ref: Date): Date => {
  const s = startOfWeekMonday(ref);
  const e = new Date(s);
  e.setDate(e.getDate() + 6);
  return e;
};

const sameYmd = (a: Date, b: Date): boolean => toYmd(a) === toYmd(b);

const eventDay = (e: CalendarEventDto): Date => stripTime(new Date(e.start));

const buildCalendarDays = (month: Date): Date[] => {
  const first = startOfMonth(month);
  const firstGridDay = new Date(first);
  firstGridDay.setDate(firstGridDay.getDate() - firstGridDay.getDay());

  return Array.from({ length: 42 }, (_, index) => {
    const day = new Date(firstGridDay);
    day.setDate(firstGridDay.getDate() + index);
    return day;
  });
};

const monthTitle = (value: Date): string =>
  value.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });

type QuickFilter = 'all' | 'today' | 'overdue' | 'thisWeek';

const buildEventDescriptionFromReminder = (r: ReminderResponseDto): string => {
  const lines: string[] = [];
  if (r.notes?.trim()) lines.push(r.notes.trim());
  if (r.propertyGroupName) lines.push(`Property group: ${r.propertyGroupName}`);
  if (r.propertyName) lines.push(`Property: ${r.propertyName}`);
  if (r.tenancySummary) lines.push(`Tenancy: ${r.tenancySummary}`);
  if (r.tenantName) lines.push(`Tenant: ${r.tenantName}`);
  if (r.reminderPriorityName) lines.push(`Priority: ${r.reminderPriorityName}`);
  return lines.length > 0 ? lines.join('\n') : 'Reminder from Property Hub.';
};

const reminderResponseToCalendarEvent = (r: ReminderResponseDto): CalendarEventDto => ({
  eventType: 'reminder',
  sourceId: r.reminderId,
  title: r.title,
  start: r.reminderDate || r.createdDate || '',
  isAllDay: true,
  description: buildEventDescriptionFromReminder(r),
  isCompleted: r.isCompleted,
  color: r.reminderPriorityColor || '#b45309',
  propertyGroupId: r.propertyGroupId,
  propertyGroupName: r.propertyGroupName,
  propertyId: r.propertyId,
  propertyName: r.propertyName,
  tenancyId: r.tenancyId,
  tenancySummary: r.tenancySummary,
  tenantId: r.tenantId,
  tenantName: r.tenantName,
});

/** Hover preview: uses calendar event payload only (no extra fetch). */
const CalendarReminderChip: React.FC<{
  event: CalendarEventDto;
  onSelect: () => void;
}> = ({ event, onSelect }) => {
  const descPreview = (() => {
    const t = event.description?.trim();
    if (!t) return null;
    return t.length > 180 ? `${t.slice(0, 180)}…` : t;
  })();

  const nativeTitle = [
    event.title,
    formatDateUk(event.start),
    event.isCompleted ? 'Completed' : 'Open',
    event.propertyName,
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <div className="group relative z-20">
      <button
        type="button"
        className={[
          'relative z-10 block w-full truncate rounded px-2 py-1 text-left text-xs text-white',
          event.isCompleted ? 'opacity-70 line-through' : '',
        ].join(' ')}
        style={{ backgroundColor: event.color || '#2563eb' }}
        onClick={(ev) => {
          ev.stopPropagation();
          onSelect();
        }}
        title={nativeTitle}
      >
        {event.title}
      </button>
      {/* Wrapper keeps hover while moving from chip into the panel; inner is scrollable and receives pointer events. */}
      <div
        className="invisible absolute left-0 top-full z-[100] w-[min(272px,calc(100vw-2rem))] pt-1 opacity-0 transition-opacity duration-150 ease-out group-hover:visible group-hover:opacity-100"
      >
        <div
          role="tooltip"
          className="max-h-48 overflow-y-auto rounded-lg border border-slate-200 bg-white p-2.5 text-left text-xs text-slate-700 shadow-xl ring-1 ring-black/5 [word-break:break-word]"
        >
          <p className="font-semibold leading-snug text-slate-900">{event.title}</p>
          <p className="mt-0.5 text-[11px] text-slate-500">{formatDateUk(event.start)}</p>
          <p className="mt-1 text-[11px] font-medium text-slate-600">
            {event.isCompleted ? 'Completed' : 'Open'}
            {event.propertyName ? ` · ${event.propertyName}` : ''}
          </p>
          {(event.tenancySummary || event.tenantName) && (
            <p className="mt-1 text-[11px] text-slate-500">
              {[event.tenancySummary, event.tenantName].filter(Boolean).join(' · ')}
            </p>
          )}
          {descPreview && (
            <p className="mt-1.5 border-t border-slate-100 pt-1.5 text-[11px] leading-relaxed text-slate-600 whitespace-pre-wrap">
              {descPreview}
            </p>
          )}
          <p className="mt-1.5 text-[10px] text-slate-400">Click for full details</p>
        </div>
      </div>
    </div>
  );
};

const RemindersCalendar: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  /** When present in the URL, "back" from calendar should return to property overview (workstream users). */
  const returnPropertyIdFromUrl = useMemo(() => {
    const raw = searchParams.get('propertyId');
    if (!raw) return null;
    const n = parseInt(raw, 10);
    return Number.isFinite(n) && n > 0 ? n : null;
  }, [searchParams]);

  const [month, setMonth] = useState<Date>(startOfMonth(new Date()));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [events, setEvents] = useState<CalendarEventDto[]>([]);
  const [groups, setGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);

  const [propertyGroupId, setPropertyGroupId] = useState<number | ''>('');
  const [propertyId, setPropertyId] = useState<number | ''>('');
  const [tenancyId, setTenancyId] = useState<number | ''>('');
  const [tenantId, setTenantId] = useState<number | ''>('');
  const [includeCompleted, setIncludeCompleted] = useState(true);

  const [quickFilter, setQuickFilter] = useState<QuickFilter>('all');

  const [popoverEvent, setPopoverEvent] = useState<CalendarEventDto | null>(null);
  const [popoverReminder, setPopoverReminder] = useState<ReminderResponseDto | null>(null);
  const [popoverLoading, setPopoverLoading] = useState(false);
  const [popoverError, setPopoverError] = useState<string | null>(null);
  const [icsBusy, setIcsBusy] = useState(false);

  const [overflowDayYmd, setOverflowDayYmd] = useState<string | null>(null);

  const [overdueReminders, setOverdueReminders] = useState<ReminderResponseDto[]>([]);
  const [overdueLoading, setOverdueLoading] = useState(false);
  const [overdueError, setOverdueError] = useState<string | null>(null);
  const [overduePanelOpen, setOverduePanelOpen] = useState(true);

  useEffect(() => {
    if (!popoverEvent) {
      setPopoverReminder(null);
      setPopoverError(null);
      setPopoverLoading(false);
      return;
    }
    if (popoverEvent.eventType !== 'reminder') {
      setPopoverReminder(null);
      setPopoverError(null);
      setPopoverLoading(false);
      return;
    }
    let cancelled = false;
    setPopoverLoading(true);
    setPopoverError(null);
    reminderService
      .getReminder(popoverEvent.sourceId)
      .then((r) => {
        if (!cancelled) setPopoverReminder(r);
      })
      .catch((err: any) => {
        if (!cancelled) setPopoverError(err.message || 'Failed to load reminder');
      })
      .finally(() => {
        if (!cancelled) setPopoverLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [popoverEvent]);

  useEffect(() => {
    if (!propertyId) return;
    const selected = properties.find((p) => p.propertyId === propertyId);
    if (!selected) return;
    setPropertyGroupId(selected.propertyGroupId);
  }, [propertyId, properties]);

  const propertiesForGroup = useMemo(() => {
    if (!propertyGroupId) return properties;
    return properties.filter((p) => p.propertyGroupId === propertyGroupId);
  }, [properties, propertyGroupId]);

  const tenanciesForProperty = useMemo(() => {
    if (!propertyId) return tenancies;
    return tenancies.filter((t) => t.propertyId === propertyId);
  }, [tenancies, propertyId]);

  const tenantsForTenancy = useMemo(() => {
    if (!tenancyId) return tenants;
    const tenancy = tenancies.find((t) => t.tenancyId === tenancyId);
    if (!tenancy) return [];
    const tenancyTenantIds = new Set(tenancy.tenants.map((t) => t.tenantId));
    return tenants.filter((t) => tenancyTenantIds.has(t.tenantId));
  }, [tenancies, tenants, tenancyId]);

  const loadLookups = useCallback(async () => {
    const [groupList, propertyList, tenancyList, tenantList] = await Promise.all([
      propertyService.getPropertyGroups(),
      propertyService.getProperties(),
      propertyAdminService.getTenancies(),
      propertyAdminService.getTenants(),
    ]);
    setGroups(groupList);
    setProperties(propertyList);
    setTenancies(tenancyList);
    setTenants(tenantList);
  }, []);

  const loadEvents = useCallback(async () => {
    let fromStr: string;
    let toStr: string;
    if (quickFilter === 'thisWeek') {
      const ws = startOfWeekMonday(new Date());
      const we = endOfWeekMonday(new Date());
      fromStr = toYmd(ws);
      toStr = toYmd(we);
    } else {
      const from = startOfMonth(month);
      const to = endOfMonth(month);
      fromStr = toYmd(from);
      toStr = toYmd(to);
    }

    const data = await calendarService.getEvents({
      from: fromStr,
      to: toStr,
      propertyGroupId: propertyGroupId || undefined,
      propertyId: propertyId || undefined,
      tenancyId: tenancyId || undefined,
      tenantId: tenantId || undefined,
      includeCompleted,
    });
    setEvents(data);
  }, [month, propertyGroupId, propertyId, tenancyId, tenantId, includeCompleted, quickFilter]);

  const loadOverdue = useCallback(async () => {
    const data = await reminderService.getOverdueReminders({
      propertyGroupId: propertyGroupId || undefined,
      propertyId: propertyId || undefined,
      tenancyId: tenancyId || undefined,
      tenantId: tenantId || undefined,
    });
    setOverdueReminders(data);
  }, [propertyGroupId, propertyId, tenancyId, tenantId]);

  useEffect(() => {
    const propertyIdParam = searchParams.get('propertyId');
    if (!propertyIdParam) return;
    const parsed = parseInt(propertyIdParam, 10);
    if (!Number.isFinite(parsed)) return;
    setPropertyId(parsed);
  }, [searchParams]);

  useEffect(() => {
    const run = async () => {
      try {
        setLoading(true);
        setError(null);
        await loadLookups();
      } catch (err: any) {
        setError(err.message || 'Failed to load calendar filters');
      } finally {
        setLoading(false);
      }
    };
    run();
  }, [loadLookups]);

  useEffect(() => {
    const run = async () => {
      try {
        setLoading(true);
        setError(null);
        await loadEvents();
      } catch (err: any) {
        setError(err.message || 'Failed to load calendar events');
      } finally {
        setLoading(false);
      }
    };
    run();
  }, [loadEvents]);

  useEffect(() => {
    const run = async () => {
      try {
        setOverdueLoading(true);
        setOverdueError(null);
        await loadOverdue();
      } catch (err: any) {
        setOverdueError(err.message || 'Failed to load overdue reminders');
      } finally {
        setOverdueLoading(false);
      }
    };
    run();
  }, [loadOverdue]);

  const applyQuickFilter = (q: QuickFilter) => {
    setQuickFilter(q);
    if (q === 'today' || q === 'thisWeek' || q === 'overdue') {
      setMonth(startOfMonth(new Date()));
    }
  };

  const changeMonth = (delta: number) => {
    if (quickFilter === 'thisWeek') {
      setQuickFilter('all');
    }
    setMonth((m) => addMonths(m, delta));
  };

  const filteredEvents = useMemo(() => {
    const today = startOfToday();
    switch (quickFilter) {
      case 'today':
        return events.filter((e) => sameYmd(eventDay(e), today));
      case 'overdue':
        return events.filter((e) => !e.isCompleted && eventDay(e) < today);
      case 'thisWeek':
        return events;
      default:
        return events;
    }
  }, [events, quickFilter]);

  const days = useMemo(() => buildCalendarDays(month), [month]);
  const eventsByDay = useMemo(() => {
    const map = new Map<string, CalendarEventDto[]>();
    filteredEvents.forEach((event) => {
      const key = toYmd(eventDay(event));
      const list = map.get(key) || [];
      list.push(event);
      map.set(key, list);
    });
    return map;
  }, [filteredEvents]);

  const overflowDayEvents = useMemo(() => {
    if (!overflowDayYmd) return [];
    return eventsByDay.get(overflowDayYmd) || [];
  }, [overflowDayYmd, eventsByDay]);

  const openEventPopover = (e: CalendarEventDto) => {
    setOverflowDayYmd(null);
    setPopoverEvent(e);
  };

  /** Close detail modal and refresh overdue list + calendar events (e.g. after edits elsewhere or future inline actions). */
  const closeReminderPopover = useCallback(async () => {
    setPopoverEvent(null);
    try {
      await Promise.all([loadOverdue(), loadEvents()]);
    } catch (err: any) {
      setOverdueError(err.message || 'Failed to refresh overdue list');
      setError(err.message || 'Failed to refresh calendar');
    }
  }, [loadOverdue, loadEvents]);

  useEffect(() => {
    if (!popoverEvent && !overflowDayYmd) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key !== 'Escape') return;
      if (overflowDayYmd) {
        setOverflowDayYmd(null);
      } else if (popoverEvent) {
        void closeReminderPopover();
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [popoverEvent, overflowDayYmd, closeReminderPopover]);

  const handleDownloadIcs = async (reminderId: number) => {
    try {
      setIcsBusy(true);
      await reminderService.downloadReminderAppointment(reminderId);
    } catch (err: any) {
      setPopoverError(err.message || 'Failed to download appointment');
    } finally {
      setIcsBusy(false);
    }
  };

  const chipClass = (active: boolean) =>
    [
      'rounded-full px-3 py-1 text-xs font-medium transition',
      active ? 'bg-slate-900 text-white' : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50',
    ].join(' ');

  const eventTypeLabel = (eventType: string): string => {
    switch (eventType) {
      case 'maintenance':
        return 'Maintenance';
      case 'contactLog':
        return 'Contact log';
      case 'journalLog':
        return 'Journal log';
      default:
        return 'Reminder';
    }
  };

  const eventFullPagePath = (event: CalendarEventDto): string => {
    const contextQs =
      returnPropertyIdFromUrl != null ? `?propertyId=${returnPropertyIdFromUrl}` : '';

    switch (event.eventType) {
      case 'maintenance':
        return `/Property Hub/Maintenance/${event.sourceId}${contextQs}`;
      case 'contactLog':
        return `/Property Hub/Contact Logs/${event.sourceId}${contextQs}`;
      case 'journalLog':
        return `/Property Hub/Journal Logs/${event.sourceId}${contextQs}`;
      default:
        return `/Property Hub/Reminders/${event.sourceId}${contextQs}`;
    }
  };

  if (loading && groups.length === 0) return <div className="py-8 text-center">Loading...</div>;

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-2xl font-bold text-gray-900">Property Calendar</h2>
        <div className="flex gap-2">
          <button
            type="button"
            className="rounded-md border border-gray-300 px-4 py-2 text-sm"
            onClick={() =>
              returnPropertyIdFromUrl
                ? navigate(`/Property Hub/Property/${returnPropertyIdFromUrl}`)
                : navigate('/Property Hub/Reminders')
            }
          >
            {returnPropertyIdFromUrl ? 'Property overview' : 'List view'}
          </button>
          <button
            type="button"
            className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
            onClick={() =>
              navigate(
                returnPropertyIdFromUrl
                  ? `/Property Hub/Reminders/New?propertyId=${returnPropertyIdFromUrl}`
                  : '/Property Hub/Reminders/New'
              )
            }
          >
            New reminder
          </button>
        </div>
      </div>

      {error && (
        <div className="mb-4 rounded border border-red-200 bg-red-50 px-4 py-3 text-red-700">{error}</div>
      )}

      <div className="mb-3 flex flex-wrap items-center gap-2">
        <span className="text-xs font-semibold uppercase text-slate-500">Quick filters</span>
        <button type="button" className={chipClass(quickFilter === 'all')} onClick={() => applyQuickFilter('all')}>
          All
        </button>
        <button type="button" className={chipClass(quickFilter === 'today')} onClick={() => applyQuickFilter('today')}>
          Today
        </button>
        <button
          type="button"
          className={chipClass(quickFilter === 'overdue')}
          onClick={() => applyQuickFilter('overdue')}
        >
          Overdue
        </button>
        <button
          type="button"
          className={chipClass(quickFilter === 'thisWeek')}
          onClick={() => applyQuickFilter('thisWeek')}
        >
          This week
        </button>
        {quickFilter === 'thisWeek' && (
          <span className="text-xs text-slate-500">Showing this calendar week. Month navigation resets the filter.</span>
        )}
        {(quickFilter === 'today' || quickFilter === 'overdue') && (
          <span className="text-xs text-slate-500">Applied to the visible month.</span>
        )}
      </div>

      <div className="mb-4 grid grid-cols-1 gap-4 rounded-lg bg-white p-4 shadow md:grid-cols-6">
        <div>
          <label className="mb-1 block text-xs font-semibold uppercase text-gray-600">Property group</label>
          <select
            className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            value={propertyGroupId === '' ? '' : String(propertyGroupId)}
            onChange={(e) => {
              setPropertyGroupId(e.target.value ? parseInt(e.target.value, 10) : '');
              setPropertyId('');
              setTenancyId('');
              setTenantId('');
            }}
          >
            <option value="">All</option>
            {groups.map((group) => (
              <option key={group.propertyGroupId} value={group.propertyGroupId}>
                {group.propertyGroupName}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-semibold uppercase text-gray-600">Property</label>
          <select
            className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            value={propertyId === '' ? '' : String(propertyId)}
            onChange={(e) => {
              setPropertyId(e.target.value ? parseInt(e.target.value, 10) : '');
              setTenancyId('');
              setTenantId('');
            }}
          >
            <option value="">All</option>
            {propertiesForGroup.map((property) => (
              <option key={property.propertyId} value={property.propertyId}>
                {property.propertyName}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-semibold uppercase text-gray-600">Tenancy</label>
          <select
            className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            value={tenancyId === '' ? '' : String(tenancyId)}
            onChange={(e) => {
              setTenancyId(e.target.value ? parseInt(e.target.value, 10) : '');
              setTenantId('');
            }}
          >
            <option value="">All</option>
            {tenanciesForProperty.map((tenancy) => (
              <option key={tenancy.tenancyId} value={tenancy.tenancyId}>
                {tenancy.description || `Tenancy #${tenancy.tenancyId}`}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-semibold uppercase text-gray-600">Tenant</label>
          <select
            className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            value={tenantId === '' ? '' : String(tenantId)}
            onChange={(e) => setTenantId(e.target.value ? parseInt(e.target.value, 10) : '')}
          >
            <option value="">All</option>
            {tenantsForTenancy.map((tenant) => (
              <option key={tenant.tenantId} value={tenant.tenantId}>
                {tenant.firstName} {tenant.lastName}
              </option>
            ))}
          </select>
        </div>
        <div className="flex items-end">
          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input
              type="checkbox"
              checked={includeCompleted}
              onChange={(e) => setIncludeCompleted(e.target.checked)}
            />
            Include completed
          </label>
        </div>
        <div className="flex items-end justify-end text-sm text-gray-500">
          {filteredEvents.length} event{filteredEvents.length === 1 ? '' : 's'} shown
        </div>
      </div>

      <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50/90 shadow-sm">
        <button
          type="button"
          className="flex w-full items-center justify-between gap-2 px-4 py-3 text-left hover:bg-amber-100/50"
          onClick={() => setOverduePanelOpen((o) => !o)}
          aria-expanded={overduePanelOpen}
        >
          <div className="min-w-0">
            <span className="font-semibold text-amber-950">Overdue reminders</span>
            <span className="ml-2 text-sm font-normal text-amber-900/85">
              {overdueLoading ? 'Loading…' : `${overdueReminders.length} open`}
            </span>
          </div>
          <span className="shrink-0 text-slate-600" aria-hidden>
            {overduePanelOpen ? '▼' : '▶'}
          </span>
        </button>
        {overduePanelOpen && (
          <div className="border-t border-amber-200 px-4 pb-4 pt-2">
            {overdueError && (
              <p className="mb-2 text-sm text-red-700" role="alert">
                {overdueError}
              </p>
            )}
            {!overdueLoading && overdueReminders.length === 0 && !overdueError && (
              <p className="text-sm text-amber-900/85">No overdue reminders for the current filters.</p>
            )}
            {overdueReminders.length > 0 && (
              <ul className="max-h-56 space-y-1.5 overflow-y-auto pr-1">
                {overdueReminders.map((r) => (
                  <li key={r.reminderId}>
                    <button
                      type="button"
                      className="flex w-full flex-col gap-0.5 rounded-md border border-amber-200/90 bg-white px-3 py-2 text-left text-sm shadow-sm hover:bg-amber-50/80"
                      onClick={() => openEventPopover(reminderResponseToCalendarEvent(r))}
                    >
                      <span className="font-medium text-slate-900">{r.title}</span>
                      <span className="text-xs text-slate-600">
                        Due {formatDateUk(r.reminderDate)}
                        {r.propertyName ? ` · ${r.propertyName}` : ''}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
            <p className="mt-2 text-xs text-amber-900/75">
              Open reminders with a reminder date before today (server UTC). Respects the property group, property,
              tenancy, and tenant filters above.
            </p>
          </div>
        )}
      </div>

      <div className="rounded-lg bg-white p-4 shadow">
        <div className="mb-4 flex items-center justify-between">
          <button
            type="button"
            className="rounded border border-gray-300 px-3 py-1 text-sm"
            onClick={() => changeMonth(-1)}
          >
            Previous
          </button>
          <h3 className="text-lg font-semibold text-gray-900">{monthTitle(month)}</h3>
          <button
            type="button"
            className="rounded border border-gray-300 px-3 py-1 text-sm"
            onClick={() => changeMonth(1)}
          >
            Next
          </button>
        </div>

        <div className="grid grid-cols-7 gap-2">
          {weekdayLabels.map((label) => (
            <div key={label} className="px-2 py-1 text-xs font-semibold uppercase text-gray-500">
              {label}
            </div>
          ))}
          {days.map((day) => {
            const key = toYmd(day);
            const dayEvents = eventsByDay.get(key) || [];
            const isCurrentMonth = day.getMonth() === month.getMonth();
            const isToday = sameYmd(day, new Date());
            return (
              <div
                key={key}
                className={[
                  'min-h-[130px] overflow-visible rounded border p-2',
                  isCurrentMonth ? 'border-gray-200 bg-white' : 'border-gray-100 bg-gray-50',
                  isToday ? 'ring-2 ring-blue-400 ring-offset-1' : '',
                ].join(' ')}
              >
                <div
                  className={['mb-2 text-xs font-semibold', isCurrentMonth ? 'text-gray-800' : 'text-gray-400'].join(' ')}
                >
                  {day.getDate()}
                </div>
                <div className="space-y-1 overflow-visible">
                  {dayEvents.slice(0, 3).map((event) => (
                    <CalendarReminderChip
                      key={`${event.eventType}-${event.sourceId}`}
                      event={event}
                      onSelect={() => openEventPopover(event)}
                    />
                  ))}
                  {dayEvents.length > 3 && (
                    <button
                      type="button"
                      className="w-full truncate px-1 text-left text-xs text-blue-700 underline"
                      onClick={(ev) => {
                        ev.stopPropagation();
                        setPopoverEvent(null);
                        setOverflowDayYmd(key);
                      }}
                    >
                      +{dayEvents.length - 3} more
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Day overflow list */}
      {overflowDayYmd && (
        <div
          className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4"
          role="presentation"
          onClick={() => setOverflowDayYmd(null)}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="day-overflow-title"
            className="max-h-[80vh] w-full max-w-md overflow-y-auto rounded-xl border border-slate-200 bg-white p-4 shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-3 flex items-center justify-between">
              <h3 id="day-overflow-title" className="text-lg font-semibold text-slate-900">
                {formatDateUk(overflowDayYmd)}
              </h3>
              <button
                type="button"
                className="rounded px-2 py-1 text-sm text-slate-600 hover:bg-slate-100"
                onClick={() => setOverflowDayYmd(null)}
              >
                Close
              </button>
            </div>
            <ul className="space-y-2">
              {overflowDayEvents.map((event) => (
                <li key={`${event.eventType}-${event.sourceId}`}>
                  <button
                    type="button"
                    className="flex w-full items-center justify-between gap-2 rounded-lg border border-slate-200 px-3 py-2 text-left text-sm hover:bg-slate-50"
                    onClick={() => {
                      setOverflowDayYmd(null);
                      openEventPopover(event);
                    }}
                  >
                    <span className="font-medium text-slate-900">{event.title}</span>
                    <div className="shrink-0 flex items-center gap-2">
                      <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] text-slate-700">
                        {eventTypeLabel(event.eventType)}
                      </span>
                      {event.isCompleted && (
                        <span className="text-xs text-slate-500">Done</span>
                      )}
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}

      {/* Reminder detail popover */}
      {popoverEvent && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          role="presentation"
          onClick={() => void closeReminderPopover()}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="popover-reminder-title"
            className="max-h-[85vh] w-full max-w-lg overflow-y-auto rounded-xl border border-slate-200 bg-white p-5 shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-4 flex items-start justify-between gap-3">
              <div>
                <h3 id="popover-reminder-title" className="text-lg font-semibold text-slate-900">
                  {popoverEvent.title}
                </h3>
                <p className="mt-1 text-xs text-slate-500">{eventTypeLabel(popoverEvent.eventType)}</p>
              </div>
              <button
                type="button"
                className="shrink-0 rounded px-2 py-1 text-sm text-slate-600 hover:bg-slate-100"
                onClick={() => void closeReminderPopover()}
              >
                Close
              </button>
            </div>

            <div className="mb-4 flex flex-wrap gap-2">
              {popoverEvent.eventType === 'reminder' && (
                <button
                  type="button"
                  disabled={icsBusy || !popoverReminder?.reminderDate}
                  className="rounded-md border border-blue-300 bg-blue-50 px-3 py-1.5 text-sm font-medium text-blue-800 hover:bg-blue-100 disabled:cursor-not-allowed disabled:opacity-50"
                  onClick={() => handleDownloadIcs(popoverEvent.sourceId)}
                >
                  {icsBusy ? 'Downloading…' : 'Download .ics (Outlook, Google, etc.)'}
                </button>
              )}
              <button
                type="button"
                className="rounded-md border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50"
                onClick={() => {
                  void (async () => {
                    await closeReminderPopover();
                    navigate(eventFullPagePath(popoverEvent));
                  })();
                }}
              >
                Open full page
              </button>
            </div>
            {popoverEvent.eventType === 'reminder' && !popoverReminder?.reminderDate && !popoverLoading && (
              <p className="mb-3 text-sm text-amber-800">
                Set a reminder date on this reminder to enable calendar export.
              </p>
            )}

            {popoverLoading && <p className="text-sm text-slate-500">Loading details…</p>}
            {popoverError && <p className="mb-3 text-sm text-red-600">{popoverError}</p>}

            {popoverReminder && (
              <dl className="space-y-2 text-sm text-slate-700">
                <div className="flex gap-2">
                  <dt className="w-28 shrink-0 font-medium text-slate-500">Reminder date</dt>
                  <dd>{formatDateUk(popoverReminder.reminderDate)}</dd>
                </div>
                <div className="flex gap-2">
                  <dt className="w-28 shrink-0 font-medium text-slate-500">Status</dt>
                  <dd>{popoverReminder.isCompleted ? 'Completed' : 'Open'}</dd>
                </div>
                {popoverReminder.reminderPriorityName && (
                  <div className="flex gap-2">
                    <dt className="w-28 shrink-0 font-medium text-slate-500">Priority</dt>
                    <dd>{popoverReminder.reminderPriorityName}</dd>
                  </div>
                )}
                <div className="flex gap-2">
                  <dt className="w-28 shrink-0 font-medium text-slate-500">Property</dt>
                  <dd>{popoverReminder.propertyName || '—'}</dd>
                </div>
                <div className="flex gap-2">
                  <dt className="w-28 shrink-0 font-medium text-slate-500">Tenancy</dt>
                  <dd>{popoverReminder.tenancySummary || '—'}</dd>
                </div>
                <div className="flex gap-2">
                  <dt className="w-28 shrink-0 font-medium text-slate-500">Tenant</dt>
                  <dd>{popoverReminder.tenantName || '—'}</dd>
                </div>
                {popoverReminder.notes && (
                  <div>
                    <dt className="mb-1 font-medium text-slate-500">Detail</dt>
                    <dd className="whitespace-pre-wrap rounded-md bg-slate-50 p-3 text-slate-800">{popoverReminder.notes}</dd>
                  </div>
                )}
              </dl>
            )}

            {!popoverLoading && !popoverReminder && (
              <dl className="space-y-2 text-sm text-slate-700">
                <div className="flex gap-2">
                  <dt className="w-28 shrink-0 font-medium text-slate-500">Date</dt>
                  <dd>{formatDateUk(popoverEvent.start)}</dd>
                </div>
                <div className="flex gap-2">
                  <dt className="w-28 shrink-0 font-medium text-slate-500">Type</dt>
                  <dd>{eventTypeLabel(popoverEvent.eventType)}</dd>
                </div>
                {popoverEvent.propertyName && (
                  <div className="flex gap-2">
                    <dt className="w-28 shrink-0 font-medium text-slate-500">Property</dt>
                    <dd>{popoverEvent.propertyName}</dd>
                  </div>
                )}
                {popoverEvent.tenancySummary && (
                  <div className="flex gap-2">
                    <dt className="w-28 shrink-0 font-medium text-slate-500">Tenancy</dt>
                    <dd>{popoverEvent.tenancySummary}</dd>
                  </div>
                )}
                {popoverEvent.tenantName && (
                  <div className="flex gap-2">
                    <dt className="w-28 shrink-0 font-medium text-slate-500">Tenant</dt>
                    <dd>{popoverEvent.tenantName}</dd>
                  </div>
                )}
              </dl>
            )}

            {!popoverLoading && !popoverReminder && popoverEvent.description && (
              <p className="mt-3 whitespace-pre-wrap text-sm text-slate-600">{popoverEvent.description}</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default RemindersCalendar;
