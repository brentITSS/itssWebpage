import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { reminderService, ReminderResponseDto } from '../../../services/reminderService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import HubScopedHeader from '../HubScopedHeader';
import { countOpenRemindersForProperty } from '../propertyHubMetrics';
import { formatDateUk } from '../../../dateFormat';
import EntityActionButtons from '../../../components/EntityActionButtons';

const RemindersList: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const propertyIdParam = searchParams.get('propertyId');
  const scopedPropertyId = propertyIdParam ? parseInt(propertyIdParam, 10) : NaN;
  const scoped = Number.isFinite(scopedPropertyId);

  const [reminders, setReminders] = useState<ReminderResponseDto[]>([]);
  const [filtered, setFiltered] = useState<ReminderResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [togglingId, setTogglingId] = useState<number | null>(null);

  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>(scoped ? scopedPropertyId : '');
  const [filterCompleted, setFilterCompleted] = useState<'all' | 'open' | 'done'>('all');
  const [showFilters, setShowFilters] = useState(false);

  useEffect(() => {
    if (scoped) setFilterPropertyId(scopedPropertyId);
  }, [scoped, scopedPropertyId]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [r, p] = await Promise.all([reminderService.getReminders(), propertyService.getProperties()]);
      setReminders(r);
      setFiltered(r);
      setProperties(p);
    } catch (err: any) {
      setError(err.message || 'Failed to load reminders');
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = useCallback(() => {
    let list = [...reminders];
    if (filterPropertyId) list = list.filter((x) => x.propertyId === filterPropertyId);
    if (filterCompleted === 'open') list = list.filter((x) => !x.isCompleted);
    if (filterCompleted === 'done') list = list.filter((x) => x.isCompleted);
    setFiltered(list);
  }, [reminders, filterPropertyId, filterCompleted]);

  useEffect(() => {
    applyFilters();
  }, [applyFilters]);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Delete this reminder?')) return;
    try {
      await reminderService.deleteReminder(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete');
    }
  };

  const toggleComplete = async (r: ReminderResponseDto) => {
    try {
      setTogglingId(r.reminderId);
      setError(null);
      await reminderService.updateReminder(r.reminderId, {
        tenantId: r.tenantId ?? null,
        tenancyId: r.tenancyId ?? null,
        propertyGroupId: r.propertyGroupId ?? null,
        propertyId: r.propertyId ?? null,
        reminderPriorityId: r.reminderPriorityId ?? null,
        title: r.title,
        notes: r.notes ?? null,
        isCompleted: !r.isCompleted,
      });
      await loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to update');
    } finally {
      setTogglingId(null);
    }
  };

  const scopedProperty = properties.find((p) => p.propertyId === scopedPropertyId);
  const scopedForList = useMemo(
    () => filtered.filter((r) => r.propertyId === scopedPropertyId),
    [filtered, scopedPropertyId]
  );
  const activeList = useMemo(() => scopedForList.filter((r) => !r.isCompleted), [scopedForList]);
  const doneList = useMemo(() => scopedForList.filter((r) => r.isCompleted), [scopedForList]);
  const openCount = scoped ? countOpenRemindersForProperty(scopedPropertyId, reminders) : 0;

  if (loading) {
    return <div className="py-8 text-center">Loading...</div>;
  }

  if (scoped && !scopedProperty) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
        Unknown property.
        <button type="button" className="ml-2 underline" onClick={() => navigate('/Property Hub/Home')}>
          Home
        </button>
      </div>
    );
  }

  const ReminderCard: React.FC<{ r: ReminderResponseDto; showCheckbox?: boolean }> = ({
    r,
    showCheckbox = false,
  }) => (
    <div
      className="flex cursor-pointer gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
      onClick={() => navigate(`/Property Hub/Reminders/${r.reminderId}`)}
    >
      {showCheckbox && (
        <div className="pt-0.5">
          <input
            type="checkbox"
            checked={r.isCompleted}
            disabled={togglingId === r.reminderId}
            onChange={() => toggleComplete(r)}
            onClick={(e) => e.stopPropagation()}
            className="h-4 w-4 rounded border-slate-300"
            aria-label={r.isCompleted ? 'Mark open' : 'Mark completed'}
          />
        </div>
      )}
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <h3
            className={`font-semibold text-slate-900 ${r.isCompleted ? 'text-slate-500 line-through' : ''}`}
          >
            {r.title}
          </h3>
          {r.reminderPriorityName && (
            <span
              className="rounded-full px-2 py-0.5 text-xs font-medium text-white"
              style={{ backgroundColor: r.reminderPriorityColor || '#64748b' }}
            >
              {r.reminderPriorityName}
            </span>
          )}
          {r.isCompleted ? (
            <span className="rounded-full bg-slate-200 px-2 py-0.5 text-xs text-slate-700">Done</span>
          ) : (
            <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-900">Open</span>
          )}
        </div>
        {r.notes && (
          <p className={`mt-1 text-sm text-slate-600 ${r.isCompleted ? 'line-through opacity-70' : ''}`}>
            {r.notes}
          </p>
        )}
        <p className="mt-2 text-xs text-slate-500">
          {r.createdDate ? `Created ${formatDateUk(r.createdDate)}` : ''}
          {r.tenantName ? ` · ${r.tenantName}` : ''}
        </p>
        <div className="mt-3">
          <EntityActionButtons
            onView={() => navigate(`/Property Hub/Reminders/${r.reminderId}`)}
            onEdit={() => navigate(`/Property Hub/Reminders/${r.reminderId}?edit=true`)}
            onDelete={() => handleDelete(r.reminderId)}
          />
        </div>
      </div>
    </div>
  );

  if (scoped && scopedProperty) {
    return (
      <div>
        <HubScopedHeader
          propertyId={scopedProperty.propertyId}
          propertyName={scopedProperty.propertyName}
          title="Reminders"
          subtitle={`${openCount} open · ${scopedForList.length} total for this property`}
          actions={
            <button
              type="button"
              onClick={() =>
                navigate(`/Property Hub/Reminders/New?propertyId=${scopedProperty.propertyId}`)
              }
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800"
            >
              + Add reminder
            </button>
          }
        />

        {error && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{error}</div>
        )}

        <section className="mb-8">
          <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">Active</h3>
          <div className="space-y-3">
            {activeList.length === 0 ? (
              <p className="text-sm text-slate-500">No open reminders.</p>
            ) : (
              activeList.map((r) => <ReminderCard key={r.reminderId} r={r} showCheckbox />)
            )}
          </div>
        </section>

        <section>
          <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">Completed</h3>
          <div className="space-y-3">
            {doneList.length === 0 ? (
              <p className="text-sm text-slate-500">No completed reminders.</p>
            ) : (
              doneList.map((r) => <ReminderCard key={r.reminderId} r={r} showCheckbox />)
            )}
          </div>
        </section>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Reminders</h2>
        <button
          type="button"
          onClick={() => {
            const q = filterPropertyId ? `?propertyId=${filterPropertyId}` : '';
            navigate(`/Property Hub/Reminders/New${q}`);
          }}
          className="rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
        >
          New reminder
        </button>
      </div>

      {error && (
        <div className="mb-4 rounded border border-red-200 bg-red-50 px-4 py-3 text-red-700">{error}</div>
      )}

      <div className="mb-6 rounded-lg bg-white p-4 shadow">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-medium text-gray-700">Filters</h3>
          <button
            type="button"
            onClick={() => setShowFilters((prev) => !prev)}
            className="text-sm font-medium text-blue-600 hover:text-blue-800"
            aria-expanded={showFilters}
          >
            {showFilters ? 'Hide filters' : 'Show filters'}
          </button>
        </div>
        {showFilters && (
          <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Property</label>
              <select
                value={filterPropertyId === '' ? '' : String(filterPropertyId)}
                onChange={(e) => setFilterPropertyId(e.target.value ? parseInt(e.target.value, 10) : '')}
                className="w-full rounded-md border border-gray-300 px-3 py-2"
              >
                <option value="">All</option>
                {properties.map((p) => (
                  <option key={p.propertyId} value={p.propertyId}>
                    {p.propertyName}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">Status</label>
              <select
                value={filterCompleted}
                onChange={(e) => setFilterCompleted(e.target.value as 'all' | 'open' | 'done')}
                className="w-full rounded-md border border-gray-300 px-3 py-2"
              >
                <option value="all">All</option>
                <option value="open">Open</option>
                <option value="done">Completed</option>
              </select>
            </div>
          </div>
        )}
      </div>

      <div className="rounded-lg bg-white shadow">
        <div className="overflow-x-auto">
          <table className="min-w-[1160px] w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Created</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Reminder</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Priority</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Tenant</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Tenancy</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Done</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 bg-white">
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-6 py-4 text-center text-sm text-gray-500">
                  No reminders found
                </td>
              </tr>
            ) : (
              filtered.map((r) => (
                <tr
                  key={r.reminderId}
                  className="cursor-pointer hover:bg-gray-50"
                  onClick={() => navigate(`/Property Hub/Reminders/${r.reminderId}`)}
                >
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-900">
                    {r.createdDate ? formatDateUk(r.createdDate) : '—'}
                  </td>
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{r.title}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">
                    {r.reminderPriorityName ? (
                      <span
                        className="rounded-full px-2 py-0.5 text-xs font-medium text-white"
                        style={{ backgroundColor: r.reminderPriorityColor || '#64748b' }}
                      >
                        {r.reminderPriorityName}
                      </span>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">{r.propertyName || '—'}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">{r.tenantName || '—'}</td>
                  <td className="max-w-xs truncate px-6 py-4 text-sm text-gray-500">{r.tenancySummary || '—'}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm">{r.isCompleted ? 'Yes' : 'No'}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-medium">
                    <EntityActionButtons
                      compact
                      onView={() => navigate(`/Property Hub/Reminders/${r.reminderId}`)}
                      onEdit={() => navigate(`/Property Hub/Reminders/${r.reminderId}?edit=true`)}
                      onDelete={() => handleDelete(r.reminderId)}
                    />
                  </td>
                </tr>
              ))
            )}
          </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default RemindersList;
