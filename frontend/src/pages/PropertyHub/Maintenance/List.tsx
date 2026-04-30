import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { maintenanceService, MaintenanceResponseDto } from '../../../services/maintenanceService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';
import HubScopedHeader from '../HubScopedHeader';
import { countMaintenanceForProperty } from '../propertyHubMetrics';
import { formatDateUk } from '../../../dateFormat';
import EntityActionButtons from '../../../components/EntityActionButtons';

const MaintenanceList: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const propertyIdParam = searchParams.get('propertyId');
  const scopedPropertyId = propertyIdParam ? parseInt(propertyIdParam, 10) : NaN;
  const scoped = Number.isFinite(scopedPropertyId);

  const [rows, setRows] = useState<MaintenanceResponseDto[]>([]);
  const [filtered, setFiltered] = useState<MaintenanceResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>(
    scoped ? scopedPropertyId : ''
  );
  const [filterPropertyGroupId, setFilterPropertyGroupId] = useState<number | ''>('');
  const [filterTypeId, setFilterTypeId] = useState<number | ''>('');
  const [types, setTypes] = useState<{ maintenanceTypeId: number; maintenanceTypeName: string }[]>([]);
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
      const [m, p, t] = await Promise.all([
        maintenanceService.getMaintenanceRecords(),
        propertyService.getProperties(),
        maintenanceService.getMaintenanceTypes(),
      ]);
      setRows(m);
      setFiltered(m);
      setProperties(p);
      setTypes(t);
    } catch (err: any) {
      setError(err.message || 'Failed to load maintenance records');
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

  const applyFilters = useCallback(() => {
    let list = [...rows];
    if (filterPropertyGroupId) {
      list = list.filter((x) => {
        const groupFromProperty = x.propertyId ? propertyToGroup.get(x.propertyId) : undefined;
        const effectiveGroupId = x.propertyGroupId ?? groupFromProperty;
        return effectiveGroupId === filterPropertyGroupId;
      });
    }
    if (filterPropertyId) list = list.filter((x) => x.propertyId === filterPropertyId);
    if (filterTypeId) list = list.filter((x) => x.maintenanceTypeId === filterTypeId);
    setFiltered(list);
  }, [rows, filterPropertyId, filterPropertyGroupId, filterTypeId, propertyToGroup]);

  useEffect(() => {
    applyFilters();
  }, [applyFilters]);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Delete this maintenance record?')) return;
    try {
      await maintenanceService.deleteMaintenanceRecord(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete');
    }
  };

  const scopedProperty = properties.find((p) => p.propertyId === scopedPropertyId);
  const scopedTotal = useMemo(
    () => (scoped ? countMaintenanceForProperty(scopedPropertyId, rows) : 0),
    [scoped, scopedPropertyId, rows]
  );
  const returnPropertyId = useMemo(() => {
    if (scoped && Number.isFinite(scopedPropertyId)) return scopedPropertyId;
    if (filterPropertyId !== '' && typeof filterPropertyId === 'number') return filterPropertyId;
    return null;
  }, [scoped, scopedPropertyId, filterPropertyId]);

  const maintenanceDetailPath = (maintenanceId: number) =>
    returnPropertyId != null
      ? `/Property Hub/Maintenance/${maintenanceId}?propertyId=${returnPropertyId}`
      : `/Property Hub/Maintenance/${maintenanceId}`;

  const maintenanceEditPath = (maintenanceId: number) => {
    const q = new URLSearchParams({ edit: 'true' });
    if (returnPropertyId != null) q.set('propertyId', String(returnPropertyId));
    return `/Property Hub/Maintenance/${maintenanceId}?${q.toString()}`;
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
          title="Maintenance"
          subtitle={`${scopedTotal} total ${scopedTotal === 1 ? 'record' : 'records'} for this property`}
          actions={
            <button
              type="button"
              onClick={() =>
                navigate(`/Property Hub/Maintenance/New?propertyId=${scopedProperty.propertyId}`)
              }
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-800"
            >
              + New record
            </button>
          }
        />

        {error && (
          <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{error}</div>
        )}

        <div className="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-4">
          <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <p className="text-xs font-semibold uppercase text-slate-500">Total</p>
            <p className="mt-1 text-2xl font-semibold text-slate-900">{filtered.length}</p>
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
                value={filterTypeId === '' ? '' : String(filterTypeId)}
                onChange={(e) => setFilterTypeId(e.target.value ? parseInt(e.target.value, 10) : '')}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
              >
                <option value="">All types</option>
                {types.map((t) => (
                  <option key={t.maintenanceTypeId} value={t.maintenanceTypeId}>
                    {t.maintenanceTypeName}
                  </option>
                ))}
              </select>
            </div>
          )}
        </div>

        <div className="space-y-3">
          {filtered.length === 0 ? (
            <p className="text-sm text-slate-500">No maintenance records.</p>
          ) : (
            filtered.map((r) => (
              <div
                key={r.maintenanceId}
                className="flex cursor-pointer flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between"
                onClick={() => navigate(maintenanceDetailPath(r.maintenanceId))}
              >
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="font-semibold text-slate-900">{r.summary || 'Maintenance'}</h3>
                    <span className="rounded-full bg-violet-100 px-2 py-0.5 text-xs text-violet-800">
                      {r.maintenanceTypeName}
                    </span>
                    {r.maintenanceStatusName && (
                      <span className="rounded-full bg-slate-200 px-2 py-0.5 text-xs text-slate-800">
                        {r.maintenanceStatusName}
                      </span>
                    )}
                  </div>
                  {r.detailNotes && (
                    <p className="mt-1 line-clamp-2 text-sm text-slate-600">{r.detailNotes}</p>
                  )}
                  <p className="mt-2 text-xs text-slate-500">
                    {scopedProperty.address || scopedProperty.propertyName}
                    {r.workDate
                      ? ` · Work date ${formatDateUk(r.workDate)}`
                      : r.createdDate
                        ? ` · Logged ${formatDateUk(r.createdDate)}`
                        : ''}
                  </p>
                </div>
                <div className="flex shrink-0">
                  <EntityActionButtons
                    onView={() => navigate(maintenanceDetailPath(r.maintenanceId))}
                    onEdit={() => navigate(maintenanceEditPath(r.maintenanceId))}
                    onDelete={() => handleDelete(r.maintenanceId)}
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
      <div className="mb-6 flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Maintenance &amp; repairs</h2>
        <button
          type="button"
          onClick={() => {
            const q = filterPropertyId ? `?propertyId=${filterPropertyId}` : '';
            navigate(`/Property Hub/Maintenance/New${q}`);
          }}
          className="rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
        >
          New record
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
          <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-4">
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
                <option value="">All</option>
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
                value={filterPropertyId === '' ? '' : String(filterPropertyId)}
                onChange={(e) => {
                  const nextPropertyId = e.target.value ? parseInt(e.target.value, 10) : '';
                  setFilterPropertyId(nextPropertyId);
                  if (nextPropertyId) {
                    const selectedProperty = properties.find((p) => p.propertyId === nextPropertyId);
                    if (selectedProperty) {
                      setFilterPropertyGroupId(selectedProperty.propertyGroupId);
                    }
                  }
                }}
                className="w-full rounded-md border border-gray-300 px-3 py-2"
              >
                <option value="">All</option>
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
              <label className="mb-1 block text-sm font-medium text-gray-700">Type</label>
              <select
                value={filterTypeId === '' ? '' : String(filterTypeId)}
                onChange={(e) => setFilterTypeId(e.target.value ? parseInt(e.target.value, 10) : '')}
                className="w-full rounded-md border border-gray-300 px-3 py-2"
              >
                <option value="">All</option>
                {types.map((t) => (
                  <option key={t.maintenanceTypeId} value={t.maintenanceTypeId}>
                    {t.maintenanceTypeName}
                  </option>
                ))}
              </select>
            </div>
          </div>
        )}
      </div>

      <div className="rounded-lg bg-white shadow">
        <div className="overflow-x-auto">
          <table className="min-w-[1040px] w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Work date</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Type</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Group</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Summary</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 bg-white">
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-6 py-4 text-center text-sm text-gray-500">
                  No maintenance records
                </td>
              </tr>
            ) : (
              filtered.map((r) => (
                <tr
                  key={r.maintenanceId}
                  className="cursor-pointer hover:bg-gray-50"
                  onClick={() => navigate(maintenanceDetailPath(r.maintenanceId))}
                >
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-900">
                    {r.workDate ? formatDateUk(r.workDate) : '—'}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">{r.maintenanceTypeName}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">{r.maintenanceStatusName || '—'}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-900">
                    {r.propertyName || 'Group-wide'}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-500">{r.propertyGroupName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <div className="max-w-xs truncate">{r.summary || '—'}</div>
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-medium">
                    <EntityActionButtons
                      compact
                      onView={() => navigate(maintenanceDetailPath(r.maintenanceId))}
                      onEdit={() => navigate(maintenanceEditPath(r.maintenanceId))}
                      onDelete={() => handleDelete(r.maintenanceId)}
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

export default MaintenanceList;
