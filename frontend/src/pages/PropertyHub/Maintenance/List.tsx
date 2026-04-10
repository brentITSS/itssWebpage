import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { maintenanceService, MaintenanceResponseDto } from '../../../services/maintenanceService';
import { propertyService, PropertyResponseDto } from '../../../services/propertyService';

const MaintenanceList: React.FC = () => {
  const navigate = useNavigate();
  const [rows, setRows] = useState<MaintenanceResponseDto[]>([]);
  const [filtered, setFiltered] = useState<MaintenanceResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [filterPropertyId, setFilterPropertyId] = useState<number | ''>('');
  const [filterTypeId, setFilterTypeId] = useState<number | ''>('');
  const [types, setTypes] = useState<{ maintenanceTypeId: number; maintenanceTypeName: string }[]>([]);

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

  const applyFilters = useCallback(() => {
    let list = [...rows];
    if (filterPropertyId) list = list.filter((x) => x.propertyId === filterPropertyId);
    if (filterTypeId) list = list.filter((x) => x.maintenanceTypeId === filterTypeId);
    setFiltered(list);
  }, [rows, filterPropertyId, filterTypeId]);

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

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Maintenance &amp; repairs</h2>
        <button
          type="button"
          onClick={() => navigate('/Property Hub/Maintenance/New')}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
        >
          New record
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">{error}</div>
      )}

      <div className="bg-white rounded-lg shadow p-4 mb-6">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Property</label>
            <select
              value={filterPropertyId === '' ? '' : String(filterPropertyId)}
              onChange={(e) => setFilterPropertyId(e.target.value ? parseInt(e.target.value, 10) : '')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
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
            <label className="block text-sm font-medium text-gray-700 mb-1">Type</label>
            <select
              value={filterTypeId === '' ? '' : String(filterTypeId)}
              onChange={(e) => setFilterTypeId(e.target.value ? parseInt(e.target.value, 10) : '')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md"
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
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Work date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Property</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Group</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Summary</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-4 text-center text-sm text-gray-500">
                  No maintenance records
                </td>
              </tr>
            ) : (
              filtered.map((r) => (
                <tr key={r.maintenanceId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {r.workDate ? new Date(r.workDate).toLocaleDateString() : '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{r.maintenanceTypeName}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{r.propertyName}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{r.propertyGroupName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    <div className="max-w-xs truncate">{r.summary || '—'}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                    <button
                      type="button"
                      onClick={() => navigate(`/Property Hub/Maintenance/${r.maintenanceId}`)}
                      className="text-blue-600 hover:text-blue-900"
                    >
                      View
                    </button>
                    <button
                      type="button"
                      onClick={() => navigate(`/Property Hub/Maintenance/${r.maintenanceId}?edit=true`)}
                      className="text-green-600 hover:text-green-900"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(r.maintenanceId)}
                      className="text-red-600 hover:text-red-900"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default MaintenanceList;
