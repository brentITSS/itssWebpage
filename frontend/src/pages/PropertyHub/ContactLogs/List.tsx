import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { contactLogService, ContactLogResponseDto } from '../../../services/contactLogService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import HubScopedHeader from '../HubScopedHeader';
import { countContactLogsForProperty } from '../propertyHubMetrics';
import { formatDateUk } from '../../../dateFormat';
import EntityActionButtons from '../../../components/EntityActionButtons';
import DeleteImpactModal from '../../../components/DeleteImpactModal';

const ContactLogsList: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const propertyIdFromUrl = searchParams.get('propertyId');
  const scopedPropertyId = propertyIdFromUrl ? parseInt(propertyIdFromUrl, 10) : NaN;
  const scoped = Number.isFinite(scopedPropertyId);

  const [contactLogs, setContactLogs] = useState<ContactLogResponseDto[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<ContactLogResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [contactLogTypes, setContactLogTypes] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pendingDelete, setPendingDelete] = useState<{
    id: number;
    attachments: number;
    tags: number;
    calendar: number;
  } | null>(null);
  const [deleteProcessing, setDeleteProcessing] = useState(false);

  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>(
    propertyIdFromUrl ? parseInt(propertyIdFromUrl, 10) : ''
  );
  const [filterPropertyGroupId, setFilterPropertyGroupId] = useState<number | ''>('');
  const [filterContactLogTypeId, setFilterContactLogTypeId] = useState<number | ''>('');
  const [filterDateFrom, setFilterDateFrom] = useState<string>('');
  const [filterDateTo, setFilterDateTo] = useState<string>('');
  const [showFilters, setShowFilters] = useState(false);

  useEffect(() => {
    const pid = searchParams.get('propertyId');
    if (pid) {
      const n = parseInt(pid, 10);
      if (Number.isFinite(n)) setFilterPropertyId(n);
    }
  }, [searchParams]);

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
      const [logsData, propertiesData, typesData] = await Promise.all([
        contactLogService.getContactLogs(),
        propertyService.getProperties(),
        contactLogService.getContactLogTypes(),
      ]);
      setContactLogs(logsData);
      setFilteredLogs(logsData);
      setProperties(propertiesData);
      setContactLogTypes(typesData);
    } catch (err: any) {
      setError(err.message || 'Failed to load contact logs');
    } finally {
      setLoading(false);
    }
  };

  const propertyGroups = useMemo(
    () =>
      Array.from(
        new Map(properties.map((p) => [p.propertyGroupId, { propertyGroupId: p.propertyGroupId, propertyGroupName: p.propertyGroupName }])).values()
      ).sort((a, b) => a.propertyGroupName.localeCompare(b.propertyGroupName)),
    [properties]
  );

  const propertyToGroup = useMemo(
    () => new Map(properties.map((p) => [p.propertyId, p.propertyGroupId])),
    [properties]
  );

  const propertyIdToGroupName = useMemo(
    () => new Map(properties.map((p) => [p.propertyId, p.propertyGroupName])),
    [properties]
  );

  const groupNameByPropertyGroupId = useMemo(
    () => new Map(propertyGroups.map((g) => [g.propertyGroupId, g.propertyGroupName])),
    [propertyGroups]
  );

  const contactPropertyGroupDisplay = useCallback(
    (log: ContactLogResponseDto) => {
      if (log.propertyGroupName?.trim()) return log.propertyGroupName.trim();
      if (log.propertyGroupId)
        return groupNameByPropertyGroupId.get(log.propertyGroupId)?.trim() || '—';
      if (log.propertyId) return propertyIdToGroupName.get(log.propertyId) ?? '—';
      return '—';
    },
    [propertyIdToGroupName, groupNameByPropertyGroupId]
  );

  const applyFilters = useCallback(() => {
    let filtered = [...contactLogs];

    if (filterPropertyGroupId) {
      filtered = filtered.filter((log) => {
        const groupFromProperty = log.propertyId ? propertyToGroup.get(log.propertyId) : undefined;
        const effectiveGroupId = log.propertyGroupId ?? groupFromProperty;
        return effectiveGroupId === filterPropertyGroupId;
      });
    }

    if (filterPropertyId) {
      filtered = filtered.filter((log) => log.propertyId === filterPropertyId);
    }

    if (filterContactLogTypeId) {
      filtered = filtered.filter((log) => log.contactLogTypeId === filterContactLogTypeId);
    }

    if (filterDateFrom) {
      filtered = filtered.filter((log) => new Date(log.contactDate) >= new Date(filterDateFrom));
    }

    if (filterDateTo) {
      filtered = filtered.filter((log) => new Date(log.contactDate) <= new Date(filterDateTo));
    }

    setFilteredLogs(filtered);
  }, [contactLogs, filterPropertyId, filterPropertyGroupId, filterContactLogTypeId, filterDateFrom, filterDateTo, propertyToGroup]);

  useEffect(() => {
    applyFilters();
  }, [applyFilters]);

  const handleDelete = async (id: number) => {
    try {
      const impact = await contactLogService.getDeleteImpact(id);
      setPendingDelete({
        id,
        attachments: impact.attachmentCount,
        tags: impact.tagCount,
        calendar: impact.calendarAppointmentCount,
      });
    } catch (err: any) {
      setError(err.message || 'Failed to load delete impact.');
    }
  };

  const confirmDelete = async () => {
    if (!pendingDelete) return;
    try {
      setDeleteProcessing(true);
      await contactLogService.deleteContactLog(pendingDelete.id);
      setPendingDelete(null);
      await loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete contact log');
    } finally {
      setDeleteProcessing(false);
    }
  };

  const scopedProperty = properties.find((p) => p.propertyId === scopedPropertyId);
  const scopedTotal = useMemo(
    () => (scoped ? countContactLogsForProperty(scopedPropertyId, contactLogs) : 0),
    [scoped, scopedPropertyId, contactLogs]
  );
  const returnPropertyId = useMemo(() => {
    if (scoped && Number.isFinite(scopedPropertyId)) return scopedPropertyId;
    if (filterPropertyId !== '' && typeof filterPropertyId === 'number') return filterPropertyId;
    return null;
  }, [scoped, scopedPropertyId, filterPropertyId]);

  const contactLogDetailPath = (contactLogId: number) =>
    returnPropertyId != null
      ? `/Property Hub/Contact Logs/${contactLogId}?propertyId=${returnPropertyId}`
      : `/Property Hub/Contact Logs/${contactLogId}`;

  const contactLogEditPath = (contactLogId: number) => {
    const q = new URLSearchParams({ edit: 'true' });
    if (returnPropertyId != null) q.set('propertyId', String(returnPropertyId));
    return `/Property Hub/Contact Logs/${contactLogId}?${q.toString()}`;
  };

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

  if (scoped && scopedProperty) {
    return (
      <div>
        <HubScopedHeader
          propertyId={scopedProperty.propertyId}
          propertyName={scopedProperty.propertyName}
          title="Contact logs"
          subtitle={`${scopedProperty.propertyGroupName} · ${scopedTotal} total for this property`}
          actions={
            <button
              type="button"
              onClick={() =>
                navigate(
                  returnPropertyId != null
                    ? `/Property Hub/Contact Logs/New?propertyId=${returnPropertyId}`
                    : '/Property Hub/Contact Logs/New'
                )
              }
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800"
            >
              + New contact log
            </button>
          }
        />

        {error && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{error}</div>
        )}

        <div className="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-4">
          <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase text-slate-500">Showing</p>
            <p className="mt-1 text-2xl font-semibold text-slate-900">{filteredLogs.length}</p>
          </div>
        </div>

        <div className="mb-4 rounded-xl border border-slate-200 bg-white p-3 shadow-sm">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium text-slate-700">Filters</h3>
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
            <div className="mt-3 flex flex-wrap gap-3">
              <select
                value={filterContactLogTypeId === '' ? '' : String(filterContactLogTypeId)}
                onChange={(e) => setFilterContactLogTypeId(e.target.value ? parseInt(e.target.value, 10) : '')}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
              >
                <option value="">All types</option>
                {contactLogTypes.map((t) => (
                  <option key={t.contactLogTypeId} value={t.contactLogTypeId}>
                    {t.contactLogTypeName}
                  </option>
                ))}
              </select>
              <input
                type="date"
                value={filterDateFrom}
                onChange={(e) => setFilterDateFrom(e.target.value)}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
                placeholder="From"
              />
              <input
                type="date"
                value={filterDateTo}
                onChange={(e) => setFilterDateTo(e.target.value)}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
                placeholder="To"
              />
            </div>
          )}
        </div>

        <div className="space-y-3">
          {filteredLogs.length === 0 ? (
            <p className="text-sm text-slate-500">No contact logs match your filters.</p>
          ) : (
            filteredLogs.map((log) => (
              <div
                key={log.contactLogId}
                className="flex cursor-pointer flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between"
                onClick={() => navigate(contactLogDetailPath(log.contactLogId))}
              >
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="font-semibold text-slate-900">{log.subject}</h3>
                    <span className="rounded-full bg-sky-100 px-2 py-0.5 text-xs text-sky-900">
                      {log.contactLogTypeName}
                    </span>
                  </div>
                  <p className="mt-1 line-clamp-2 text-sm text-slate-600">{log.notes || '—'}</p>
                  <p className="mt-2 text-xs text-slate-500">
                    <span className="font-medium text-slate-600">{contactPropertyGroupDisplay(log)}</span>
                    {scopedProperty.propertyName ? (
                      <>
                        {' · '}
                        <span>{scopedProperty.propertyName}</span>
                      </>
                    ) : null}
                  </p>
                  <p className="mt-1 text-xs text-slate-500">
                    {formatDateUk(log.contactDate)}
                    {log.tenantName ? ` · ${log.tenantName}` : ''}
                  </p>
                </div>
                <div className="flex shrink-0">
                  <EntityActionButtons
                    onView={() => navigate(contactLogDetailPath(log.contactLogId))}
                    onEdit={() => navigate(contactLogEditPath(log.contactLogId))}
                    onDelete={() => handleDelete(log.contactLogId)}
                  />
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    );
  }

  return (
    <div>
      <DeleteImpactModal
        isOpen={pendingDelete !== null}
        title="Delete Contact Log?"
        subjectLabel="this contact log"
        impacts={[
          { label: 'Calendar appointments', count: pendingDelete?.calendar ?? 0 },
          { label: 'Tags', count: pendingDelete?.tags ?? 0 },
          { label: 'Attachments', count: pendingDelete?.attachments ?? 0 },
        ]}
        confirmText="Delete contact log"
        isProcessing={deleteProcessing}
        onCancel={() => setPendingDelete(null)}
        onConfirm={confirmDelete}
      />
      <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Contact Logs</h2>
        <button
          type="button"
          onClick={() =>
            navigate(
              returnPropertyId != null
                ? `/Property Hub/Contact Logs/New?propertyId=${returnPropertyId}`
                : '/Property Hub/Contact Logs/New'
            )
          }
          className="w-full rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 sm:w-auto"
        >
          New Contact Log
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
          <>
            <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-5">
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Property Group</label>
                <select
                  value={filterPropertyGroupId === '' ? '' : String(filterPropertyGroupId)}
                  onChange={(e) => {
                    const nextGroup = e.target.value ? parseInt(e.target.value, 10) : '';
                    setFilterPropertyGroupId(nextGroup);
                    if (nextGroup && filterPropertyId) {
                      const selectedProperty = properties.find((p) => p.propertyId === filterPropertyId);
                      if (selectedProperty && selectedProperty.propertyGroupId !== nextGroup) {
                        setFilterPropertyId('');
                      }
                    }
                  }}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  <option value="">All Groups</option>
                  {propertyGroups.map((g) => (
                    <option key={g.propertyGroupId} value={g.propertyGroupId}>
                      {g.propertyGroupName}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Property</label>
                <select
                  value={filterPropertyId === '' ? '' : filterPropertyId.toString()}
                  onChange={(e) => {
                    const nextPropertyId = e.target.value ? parseInt(e.target.value, 10) : '';
                    setFilterPropertyId(nextPropertyId);
                    if (nextPropertyId) {
                      const nextProperty = properties.find((p) => p.propertyId === nextPropertyId);
                      if (nextProperty) {
                        setFilterPropertyGroupId(nextProperty.propertyGroupId);
                      }
                    }
                  }}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  <option value="">All Properties</option>
                  {properties
                    .filter((p) => !filterPropertyGroupId || p.propertyGroupId === filterPropertyGroupId)
                    .map((p) => (
                    <option key={p.propertyId} value={p.propertyId}>
                      {p.propertyName}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Contact Log Type</label>
                <select
                  value={filterContactLogTypeId === '' ? '' : String(filterContactLogTypeId)}
                  onChange={(e) => setFilterContactLogTypeId(e.target.value ? parseInt(e.target.value, 10) : '')}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  <option value="">All Types</option>
                  {contactLogTypes.map((t) => (
                    <option key={t.contactLogTypeId} value={t.contactLogTypeId}>
                      {t.contactLogTypeName}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Date From</label>
                <input
                  type="date"
                  value={filterDateFrom}
                  onChange={(e) => setFilterDateFrom(e.target.value)}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Date To</label>
                <input
                  type="date"
                  value={filterDateTo}
                  onChange={(e) => setFilterDateTo(e.target.value)}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </div>
            </div>
            {(filterPropertyId || filterPropertyGroupId || filterContactLogTypeId || filterDateFrom || filterDateTo) && (
              <button
                type="button"
                onClick={() => {
                  setFilterPropertyId('');
                  setFilterPropertyGroupId('');
                  setFilterContactLogTypeId('');
                  setFilterDateFrom('');
                  setFilterDateTo('');
                }}
                className="mt-4 text-sm text-blue-600 hover:text-blue-800"
              >
                Clear Filters
              </button>
            )}
          </>
        )}
      </div>

      <div className="rounded-lg bg-white shadow">
        <div className="overflow-x-auto">
          <table className="min-w-[980px] w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Property Group</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Tenant</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Type</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Subject</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Notes</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 bg-white">
            {filteredLogs.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-6 py-4 text-center text-sm text-gray-500">
                  No contact logs found
                </td>
              </tr>
            ) : (
              filteredLogs.map((log) => (
                <tr
                  key={log.contactLogId}
                  className="cursor-pointer hover:bg-gray-50"
                  onClick={() => navigate(contactLogDetailPath(log.contactLogId))}
                >
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-900">
                    {formatDateUk(log.contactDate)}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-600">
                    {contactPropertyGroupDisplay(log)}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-900">{log.propertyName}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">{log.tenantName || '-'}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">{log.contactLogTypeName}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-gray-900">{log.subject}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <div className="max-w-xs truncate">{log.notes || '-'}</div>
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-medium">
                    <EntityActionButtons
                      compact
                      onView={() => navigate(contactLogDetailPath(log.contactLogId))}
                      onEdit={() => navigate(contactLogEditPath(log.contactLogId))}
                      onDelete={() => handleDelete(log.contactLogId)}
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

export default ContactLogsList;
