import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { contactLogService, ContactLogResponseDto } from '../../../services/contactLogService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import HubScopedHeader from '../HubScopedHeader';
import { countContactLogsForProperty } from '../propertyHubMetrics';
import { formatDateUk } from '../../../dateFormat';
import EntityActionButtons from '../../../components/EntityActionButtons';

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

  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>(
    propertyIdFromUrl ? parseInt(propertyIdFromUrl, 10) : ''
  );
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

  const applyFilters = useCallback(() => {
    let filtered = [...contactLogs];

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
  }, [contactLogs, filterPropertyId, filterContactLogTypeId, filterDateFrom, filterDateTo]);

  useEffect(() => {
    applyFilters();
  }, [applyFilters]);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this contact log?')) return;

    try {
      await contactLogService.deleteContactLog(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete contact log');
    }
  };

  const scopedProperty = properties.find((p) => p.propertyId === scopedPropertyId);
  const scopedTotal = useMemo(
    () => (scoped ? countContactLogsForProperty(scopedPropertyId, contactLogs) : 0),
    [scoped, scopedPropertyId, contactLogs]
  );

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
          subtitle={`${scopedTotal} total for this property`}
          actions={
            <button
              type="button"
              onClick={() => navigate('/Property Hub/Contact Logs/New')}
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
                onClick={() => navigate(`/Property Hub/Contact Logs/${log.contactLogId}`)}
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
                    {formatDateUk(log.contactDate)}
                    {log.tenantName ? ` · ${log.tenantName}` : ''}
                  </p>
                </div>
                <div className="flex shrink-0">
                  <EntityActionButtons
                    onView={() => navigate(`/Property Hub/Contact Logs/${log.contactLogId}`)}
                    onEdit={() => navigate(`/Property Hub/Contact Logs/${log.contactLogId}?edit=true`)}
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
      <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Contact Logs</h2>
        <button
          type="button"
          onClick={() => navigate('/Property Hub/Contact Logs/New')}
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
            <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-4">
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">Property</label>
                <select
                  value={filterPropertyId === '' ? '' : filterPropertyId.toString()}
                  onChange={(e) => setFilterPropertyId(e.target.value ? parseInt(e.target.value, 10) : '')}
                  className="w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  <option value="">All Properties</option>
                  {properties.map((p) => (
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
            {(filterPropertyId || filterContactLogTypeId || filterDateFrom || filterDateTo) && (
              <button
                type="button"
                onClick={() => {
                  setFilterPropertyId('');
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
                <td colSpan={7} className="px-6 py-4 text-center text-sm text-gray-500">
                  No contact logs found
                </td>
              </tr>
            ) : (
              filteredLogs.map((log) => (
                <tr
                  key={log.contactLogId}
                  className="cursor-pointer hover:bg-gray-50"
                  onClick={() => navigate(`/Property Hub/Contact Logs/${log.contactLogId}`)}
                >
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-900">
                    {formatDateUk(log.contactDate)}
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
                      onView={() => navigate(`/Property Hub/Contact Logs/${log.contactLogId}`)}
                      onEdit={() => navigate(`/Property Hub/Contact Logs/${log.contactLogId}?edit=true`)}
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
